using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace System.Data.IO
{
    public class SqlDataManager : IDataManager
    {
        private const string RowIdColName = "_ID_";

        private readonly string DbConnString;

        public SqlDataManager(string connString)
        {
            SqlConnectionStringBuilder sqlConnString = new SqlConnectionStringBuilder(connString);
            string server = sqlConnString.DataSource, database = sqlConnString.InitialCatalog,
                   userId = sqlConnString.IntegratedSecurity ? null : sqlConnString.UserID,
                   password = sqlConnString.IntegratedSecurity ? null : sqlConnString.Password;

            SqlUtility.CreateDatabase(server, database, userId, password, false);
            
            this.DbConnString = SqlUtility.GetSqlConnectionString(connString);
        }


        #region IDataProvider

        public string[] ListFolders()
        {
            ICollection<string> schemas = new List<string>();
            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT DISTINCT S.name AS SchemaName FROM sys.tables T JOIN sys.schemas S ON T.schema_id = S.schema_id";
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schemas.Add((string)reader["SchemaName"]);
                    }
                }

                return null;
            });
            return schemas.ToArray();
        }

        public string[] ListFiles(string folderName)
        {
            ICollection<string> tables = new List<string>();
            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "SELECT DISTINCT T.name AS TableName FROM sys.tables T JOIN sys.schemas S ON T.schema_id = S.schema_id WHERE S.name = '{0}'",
                    SqlUtility.SqlEscapeStringValue(folderName));
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add((string)reader["TableName"]);
                    }
                }

                return null;
            });
            return tables.ToArray();
        }

        public bool FileExists(string folderName, string fileName)
        {
            return (bool)SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "SELECT 1 FROM sys.tables T JOIN sys.schemas S ON T.schema_id = S.schema_id WHERE S.name = '{0}' AND T.name = '{1}'",
                    SqlUtility.SqlEscapeStringValue(folderName),
                    SqlUtility.SqlEscapeStringValue(fileName));
                cmd.Parameters.Clear();

                bool exists;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    exists = reader.Read();
                }

                return exists;
            });
        }

        public Metadata GetMetadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (!this.FileExists(folderName, fileName))
                throw new InvalidOperationException(string.Format(
                    "Folder '{0}', File '{1}' does not exist.",
                    folderName,
                    fileName));

            SqlMetadata metadata = new SqlMetadata(folderName, fileName);

            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                string qualifiedTableName = GetQualifiedObjectName(folderName, fileName);

                // Get item count in table
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT COUNT(*) FROM {0};", qualifiedTableName);
                cmd.Parameters.Clear();

                metadata.ItemCount = (int)cmd.ExecuteScalar();


                // Get table schema
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT TOP(0) * FROM {0};", qualifiedTableName);
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    var schemaTable = reader.GetSchemaTable();
                    DataRow idColMetadata = null;

                    foreach (DataRow row in schemaTable.Rows)
                    {
                        if (((string)row["ColumnName"]).Equals(RowIdColName, StringComparison.OrdinalIgnoreCase) &&
                            (Type)row["DataType"] == typeof(long) &&
                            (bool)row["AllowDBNull"] == false)
                        {
                            idColMetadata = row;
                            break;
                        }
                    }

                    if (null != idColMetadata)
                    {
                        using (SqlCommand cmd2 = cmd.Connection.CreateCommand())
                        {
                            cmd2.Transaction = cmd.Transaction;

                            cmd2.CommandType = CommandType.Text;
                            cmd2.CommandText = string.Format("SELECT COUNT_BIG({1}), MIN({1}), MAX({1}) FROM {0};", qualifiedTableName, RowIdColName);
                            cmd2.Parameters.Clear();

                            using (SqlDataReader reader2 = cmd2.ExecuteReader())
                            {
                                if (reader2.Read())
                                {
                                    long count = (long)reader2[0];
                                    if (count > 0L)
                                    {
                                        long min = (long)reader2[1],
                                             max = (long)reader2[2];
                                        metadata.SupportsIndexing = (count == max-min+1);
                                        metadata.StartIndex = min;
                                    }
                                    else
                                    {
                                        metadata.SupportsIndexing = true;
                                        metadata.StartIndex = null;
                                    }
                                }
                            }
                        }
                    }

                    int numDims = metadata.SupportsIndexing ? schemaTable.Rows.Count-1 : schemaTable.Rows.Count;
                    metadata.FieldNames = new string[numDims];
                    metadata.FieldTypes = new Type[numDims];

                    int j = 0;
                    foreach (DataRow row in schemaTable.Rows)
                    {
                        if (row == idColMetadata)
                            continue;

                        Type dimType = (Type)row["DataType"];
                        if (dimType.IsValueType && (bool)row["AllowDBNull"])
                            dimType = typeof(Nullable<>).MakeGenericType(dimType);

                        metadata.FieldNames[j] = (string)row["ColumnName"];
                        metadata.FieldTypes[j] = dimType;

                        j++;
                    }
                }

                return null;
            });

            return metadata;
        }

        public object[] GetItem(string folderName, string fileName, long indx)
        {
            return this.GetItems(folderName, fileName, indx, 1L).FirstOrDefault();
        }

        public ICollection<object[]> GetItems(string folderName, string fileName, long startIndx, long numItems)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (startIndx < 0L)
                throw new ArgumentOutOfRangeException("startIndx");

            if (numItems < 0L)
                throw new ArgumentOutOfRangeException("numItems");

            SqlMetadata metadata = (SqlMetadata)this.GetMetadata(folderName, fileName);

            ICollection<object[]> items = new List<object[]>();
            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                string qualifiedTableName = GetQualifiedObjectName(folderName, fileName);

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = metadata.SupportsIndexing
                    ? string.Format("SELECT TOP({1}) * FROM {0} WHERE {2} >= {3};", qualifiedTableName, numItems, RowIdColName, (metadata.StartIndex ?? 0L) + startIndx)
                    : string.Format("SELECT TOP({1}) * FROM {0};", qualifiedTableName, startIndx+numItems);
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!metadata.SupportsIndexing)
                    {
                        for (long i = 0L; i < startIndx; i++)
                            reader.Read();
                    }
                    while (reader.Read())
                    {
                        items.Add(ReadItem(reader, metadata));
                    }
                }

                return null;
            });
            return items;
        }

        public ICollection<object[]> GetItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            SqlMetadata metadata = (SqlMetadata)this.GetMetadata(folderName, fileName);

            ICollection<object[]> items = new List<object[]>();
            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT * FROM {0};", GetQualifiedObjectName(folderName, fileName));
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int numDims = reader.FieldCount;
                    while (reader.Read())
                    {
                        items.Add(ReadItem(reader, metadata));
                    }
                }

                return null;
            });
            return items;
        }

        #endregion IDataProvider


        #region IDataManager

        public void AddFile(Metadata metadata)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.FolderName))
                    throw new ArgumentNullException("metadata.FolderName");

                if (string.IsNullOrWhiteSpace(metadata.FileName))
                    throw new ArgumentNullException("metadata.FileName");

                if (null == metadata.FieldNames)
                    throw new ArgumentNullException("metadata.FieldNames");

                if (null == metadata.FieldTypes)
                    throw new ArgumentNullException("metadata.FieldTypes");

                int dimNamesLength = metadata.FieldNames.Length,
                    dimTypesLength = metadata.FieldTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Fields");
            }

            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                CreateTable(metadata, cmd);

                return null;
            });
        }

        public void EditFile(Metadata metadata)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.FolderName))
                    throw new ArgumentNullException("metadata.FolderName");

                if (string.IsNullOrWhiteSpace(metadata.FileName))
                    throw new ArgumentNullException("metadata.FileName");

                if (null == metadata.FieldNames)
                    throw new ArgumentNullException("metadata.FieldNames");

                if (null == metadata.FieldTypes)
                    throw new ArgumentNullException("metadata.FieldTypes");

                int dimNamesLength = metadata.FieldNames.Length,
                    dimTypesLength = metadata.FieldTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Fields");
            }

            if (!this.FileExists(metadata.FolderName, metadata.FileName))
                throw new InvalidOperationException(string.Format(
                    "Folder '{0}', File '{1}' does not exist.",
                    metadata.FolderName,
                    metadata.FileName));

            Metadata oldMetadata = this.GetMetadata(metadata.FolderName, metadata.FileName);

            if (!metadata.SchemaEquals(oldMetadata))
            {
                SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
                {
                    DropTable(metadata.FolderName, metadata.FileName, cmd);
                    CreateTable(metadata, cmd);

                    return null;
                });
            }
        }

        public void AddItem(string folderName, string fileName, object[] item)
        {
            this.AddItems(folderName, fileName, new object[][] { item });
        }

        public void AddItems(string folderName, string fileName, IEnumerable<object[]> items)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (null == items)
                throw new ArgumentNullException("items");

            SqlMetadata metadata = (SqlMetadata)this.GetMetadata(folderName, fileName);

            int numDims = metadata.FieldNames.Length;

            DataTable table = new DataTable();
            for (int j = 0; j < numDims; j++)
            {
                table.Columns.Add(metadata.FieldNames[j], metadata.FieldTypes[j]);
            }

            foreach (object[] item in items)
            {
                table.Rows.Add(item);
            }

            SqlUtility.ExecuteSqlOperation(this.DbConnString, (SqlConnection conn) =>
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                {
                    bulkCopy.BulkCopyTimeout = SqlUtility.DefaultCommandTimeoutInSeconds;
                    for (int j = 0; j < numDims; j++)
                    {
                        bulkCopy.ColumnMappings.Add(metadata.FieldNames[j], metadata.FieldNames[j]);
                    }
                    bulkCopy.DestinationTableName = GetQualifiedObjectName(folderName, fileName);
                    bulkCopy.WriteToServer(table);
                }
                return null;
            });
        }

        public void ClearItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("TRUNCATE TABLE {0};", GetQualifiedObjectName(folderName, fileName));
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();


                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("DBCC CHECKIDENT('{0}', RESEED, 0);", GetQualifiedObjectName(folderName, fileName));
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();

                return null;
            });
        }

        public void RemoveFile(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            SqlUtility.ExecuteSqlCommand(this.DbConnString, (SqlCommand cmd) =>
            {
                DropTable(folderName, fileName, cmd);

                return null;
            });
        }

        #endregion IDataManager


        #region InternalOps

        private static string GetQualifiedObjectName(string schemaName, string objectName)
        {
            return string.Format("[{0}].[{1}]", schemaName, objectName);
        }

        private static void CreateTable(Metadata metadata, SqlCommand cmd)
        {
            {
                StringBuilder cmdTxt = new StringBuilder();
                cmdTxt.AppendFormat("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{0}')", SqlUtility.SqlEscapeStringValue(metadata.FolderName));
                cmdTxt.AppendLine();
                cmdTxt.AppendLine("BEGIN");
                cmdTxt.AppendLine("  DECLARE @cmd NVARCHAR(MAX);");
                cmdTxt.AppendFormat("  SET @cmd = 'CREATE SCHEMA [{0}]';", metadata.FolderName);
                cmdTxt.AppendLine();
                cmdTxt.AppendLine("  EXEC(@cmd);");
                cmdTxt.AppendLine("END");

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdTxt.ToString();
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();
            }

            string qualifiedTableName = GetQualifiedObjectName(metadata.FolderName, metadata.FileName);

            int numDims = metadata.FieldNames.Length;
            {
                StringBuilder cmdText = new StringBuilder();
                cmdText.AppendFormat("CREATE TABLE {0} ({1} BIGINT IDENTITY(0,1) PRIMARY KEY", qualifiedTableName, RowIdColName);
                for (int j = 0; j < numDims; j++)
                {
                    cmdText.AppendFormat(", [{0}] {1}", metadata.FieldNames[j], SqlUtility.ToSqlType(metadata.FieldTypes[j]));
                    if (metadata.FieldTypes[j].IsValueType && !typeof(Nullable<>).IsAssignableFrom(metadata.FieldTypes[j]))
                        cmdText.Append(" NOT NULL");
                }
                cmdText.Append(");");

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText.ToString();
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();
            }

            foreach (string dimName in Enumerable.Range(0, numDims)
                    .Where(j => typeof(byte[]) != metadata.FieldTypes[j] && typeof(string) != metadata.FieldTypes[j])
                    .Select(j => metadata.FieldNames[j]))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "CREATE NONCLUSTERED INDEX [IX_{0}_{1}_{2}_{3}] ON {4} ([{5}] ASC);",
                    metadata.FolderName.Substring(0, Math.Min(metadata.FolderName.Length, 20)),
                    metadata.FileName.Substring(0, Math.Min(metadata.FileName.Length, 40)),
                    dimName.Substring(0, Math.Min(dimName.Length, 30)),
                    Guid.NewGuid().ToString("N"),
                    qualifiedTableName,
                    dimName);
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();
            }

            // Consider adding full-text index for varchar and varbinary columns.
        }

        private static void DropTable(string folderName, string fileName, SqlCommand cmd)
        {
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = string.Format("DROP TABLE {0};", GetQualifiedObjectName(folderName, fileName));
            cmd.Parameters.Clear();

            cmd.ExecuteNonQuery();
        }

        private static object[] ReadItem(SqlDataReader reader, SqlMetadata metadata)
        {
            int numDims = metadata.FieldTypes.Length;
            object[] item = new object[numDims];
            for (int j = 0; j < numDims; j++)
            {
                object obj = metadata.SupportsIndexing ? reader[j+1] : reader[j];
                if (obj is DBNull)
                    obj = null;
                item[j] = obj;
            }
            return item;
        }

        #endregion InternalOps
    }
}
