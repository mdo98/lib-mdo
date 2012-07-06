using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public class SqlDataManager : IDataManager
    {
        private readonly string DbConnString;

        public SqlDataManager(string connString)
        {
            this.DbConnString = SqlUtility.GetSqlConnectionString(connString);
        }


        #region IDataProvider

        public string[] ListFolders()
        {
            return (string[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "ListFolders");
                cmd.Parameters.Clear();

                ICollection<string> classes = new List<string>();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string className = (string)reader["Name"];
                        classes.Add(className);
                    }
                }
                return classes.ToArray();
            }, ShouldThrowOnDbOperationException);
        }

        public string[] ListFiles(string folderName)
        {
            return (string[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "ListFiles");
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@FolderName", SqlDbType.VarChar).Value = folderName;

                ICollection<string> variants = new List<string>();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string variant = (string)reader["Name"];
                        variants.Add(variant);
                    }
                }
                return variants.ToArray();
            }, ShouldThrowOnDbOperationException);
        }

        public bool FileExists(string folderName, string fileName)
        {
            return (bool)SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "FileExists");
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@FolderName",   SqlDbType.VarChar).Value    = folderName;
                cmd.Parameters.Add("@FileName",     SqlDbType.VarChar).Value    = fileName;
                cmd.Parameters.Add("@Exists",       SqlDbType.Bit).Direction    = ParameterDirection.ReturnValue;

                cmd.ExecuteNonQuery();

                return cmd.Parameters["@Exists"].Value;
            }, ShouldThrowOnDbOperationException);
        }

        public Metadata GetMetadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            Metadata metadata = new Metadata(folderName, fileName);

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "GetFile");
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@FolderName",   SqlDbType.VarChar).Value    = folderName;
                cmd.Parameters.Add("@FileName",     SqlDbType.VarChar).Value    = fileName;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader["DfltColIndx"] != DBNull.Value)
                            metadata.DefaultField = (short)reader["DfltColIndx"];

                        if (reader["Desc"] != DBNull.Value)
                            metadata.Desc = (string)reader["Desc"];
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format(
                            "Folder '{0}', File '{1}' does not exist.",
                            folderName,
                            fileName));
                    }
                }

                // Get item count & field information
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT COUNT(*) FROM {0};", GetQualifiedObjectName(folderName, fileName));
                cmd.Parameters.Clear();

                metadata.ItemCount = (int)cmd.ExecuteScalar();


                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT TOP(0) * FROM {0};", GetQualifiedObjectName(folderName, fileName));
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int numDims = reader.FieldCount;
                    metadata.FieldNames = new string[numDims-1];
                    metadata.FieldTypes = new Type[numDims-1];

                    for (int j = 1; j < numDims; j++)
                    {
                        metadata.FieldNames[j-1] = reader.GetName(j);

                        Type dimType = reader.GetFieldType(j);
                        if (dimType.IsValueType)
                            dimType = typeof(Nullable<>).MakeGenericType(dimType);

                        metadata.FieldTypes[j-1] = dimType;
                    }
                }

                return null;
            }, ShouldThrowOnDbOperationException);

            return metadata;
        }

        public object[] GetItem(string folderName, string fileName, long indx)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (indx < 0L)
                throw new ArgumentOutOfRangeException("indx");

            return (object[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT * FROM {0} WHERE [Id] = {1};", GetQualifiedObjectName(folderName, fileName), indx);
                cmd.Parameters.Clear();

                object[] item;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        item = ReadItem(reader, reader.FieldCount);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("indx");
                    }
                }
                return item;
            }, ShouldThrowOnDbOperationException);
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

            ICollection<object[]> items = new List<object[]>();
            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("SELECT TOP({0}) * FROM {1} WHERE [Id] >= {2};",
                    numItems,
                    GetQualifiedObjectName(folderName, fileName),
                    startIndx);
                cmd.Parameters.Clear();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int numDims = reader.FieldCount;
                    while (reader.Read())
                    {
                        items.Add(ReadItem(reader, numDims));
                    }
                }

                return null;
            }, ShouldThrowOnDbOperationException);
            return items;
        }

        public bool SupportsPages { get { return false; } }

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

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "AddFile");
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@FolderName",   SqlDbType.VarChar).Value    = metadata.FolderName;
                cmd.Parameters.Add("@FileName",     SqlDbType.VarChar).Value    = metadata.FileName;
                cmd.Parameters.Add("@DfltColIndx",  SqlDbType.SmallInt).Value   = metadata.DefaultField;
                cmd.Parameters.Add("@Desc",         SqlDbType.VarChar).Value    = metadata.Desc == null ? (object)DBNull.Value : metadata.Desc;

                cmd.ExecuteNonQuery();

                CreateTable(metadata, cmd);

                return null;
            }, ShouldThrowOnDbOperationException);
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

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                if (metadata.DefaultField != oldMetadata.DefaultField ||
                    metadata.Desc != oldMetadata.Desc)
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "EditFile");
                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@FolderName",   SqlDbType.VarChar).Value    = metadata.FolderName;
                    cmd.Parameters.Add("@FileName",     SqlDbType.VarChar).Value    = metadata.FileName;
                    cmd.Parameters.Add("@DfltColIndx",  SqlDbType.SmallInt).Value   = metadata.DefaultField;
                    cmd.Parameters.Add("@Desc",         SqlDbType.VarChar).Value    = metadata.Desc == null ? (object)DBNull.Value : metadata.Desc;

                    cmd.ExecuteNonQuery();
                }

                if (!metadata.SchemaEquals(oldMetadata))
                {
                    DropTable(metadata.FolderName, metadata.FileName, cmd);
                    CreateTable(metadata, cmd);
                }

                return null;
            }, ShouldThrowOnDbOperationException);
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

            Metadata metadata = this.GetMetadata(folderName, fileName);

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                int numDims = metadata.FieldNames.Length;

                StringBuilder cmdText = new StringBuilder();
                cmdText.AppendFormat("INSERT INTO {0} (", GetQualifiedObjectName(metadata.FolderName, metadata.FileName));
                for (int j = 0; j < numDims; j++)
                {
                    cmdText.AppendFormat("[{0}]", metadata.FieldNames[j]);
                    if (j < numDims-1)
                        cmdText.Append(",");
                    else
                        cmdText.Append(")");
                }
                cmdText.Append(" VALUES (");
                for (int j = 0; j < numDims; j++)
                {
                    cmdText.AppendFormat("@P_{0}", j);
                    if (j < numDims-1)
                        cmdText.Append(",");
                    else
                        cmdText.Append(")");
                }
                cmdText.Append(";");

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = cmdText.ToString();

                ICollection<Exception> exceptions = new List<Exception>();

                foreach (object[] item in items)
                {
                    try
                    {
                        cmd.Parameters.Clear();

                        for (int j = 0; j < numDims; j++)
                        {
                            cmd.Parameters.Add("@P_" + j, SqlUtility.ToSqlDbType(metadata.FieldTypes[j]))
                                .Value = (null == item[j] ? DBNull.Value : item[j]);
                        }

                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);
                else
                    return null;
            }, ShouldThrowOnDbOperationException);
        }

        public void ClearItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
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
            }, ShouldThrowOnDbOperationException);
        }

        public void RemoveFile(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = GetQualifiedObjectName(DataIO.MetaSchemaName, "RemoveFile");
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@FolderName",   SqlDbType.VarChar).Value    = folderName;
                cmd.Parameters.Add("@FileName",     SqlDbType.VarChar).Value    = fileName;

                cmd.ExecuteNonQuery();


                DropTable(folderName, fileName, cmd);

                return null;
            }, ShouldThrowOnDbOperationException);
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
                cmdTxt.AppendFormat("IF NOT EXISTS (SELECT 1 FROM [sys].[schemas] WHERE name = '{0}')", SqlUtility.SqlEscapeStringValue(metadata.FolderName));
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
                cmdText.AppendFormat("CREATE TABLE {0} ([Id] BIGINT IDENTITY(0,1) PRIMARY KEY", qualifiedTableName);
                for (int j = 0; j < numDims; j++)
                {
                    cmdText.AppendFormat(", [{0}] {1}", metadata.FieldNames[j], SqlUtility.ToSqlType(metadata.FieldTypes[j]));
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
            cmd.CommandText = string.Format("DROP TABLE {0}", GetQualifiedObjectName(folderName, fileName));
            cmd.Parameters.Clear();

            cmd.ExecuteNonQuery();
        }

        private static object[] ReadItem(SqlDataReader reader, int numDims)
        {
            object[] item = new object[numDims-1];
            for (int j = 1; j < numDims; j++)
            {
                object obj = reader[j];
                if (obj is DBNull)
                    obj = null;
                item[j-1] = obj;
            }
            return item;
        }

        private static bool ShouldThrowOnDbOperationException(Exception ex)
        {
            return !(ex is SqlException);
        }

        #endregion InternalOps
    }
}
