using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus
{
    public class SqlClassDataManager : IClassDataManager
    {
        public const string CorpusDBInitScript = "CorpusDB.sql";

        private readonly string DbConnString;

        public SqlClassDataManager(string connString)
        {
            this.DbConnString = SqlUtility.GetSqlConnectionString(connString);
        }


        #region IClassDataProvider

        public string[] ListClasses()
        {
            return (string[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[ListClasses]";

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

        public string[] ListVariants(string className)
        {
            return (string[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[ListClassVariants]";

                cmd.Parameters.Add("@ClassName", SqlDbType.VarChar).Value = className;

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

        public ClassMetadata GetMetadata(string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            ClassMetadata metadata = new ClassMetadata(className, variantName);

            // List all variants
            ICollection<string> variants = new List<string>();
            string variantRef = null;
            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[ListClassVariants]";

                cmd.Parameters.Add("@ClassName", SqlDbType.VarChar).Value = className;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string variant = (string)reader["Name"];
                        variants.Add(variant);

                        if (variant.Equals(variantName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (reader["Ref"] != DBNull.Value)
                                variantRef = (string)reader["Ref"];

                            if (reader["Desc"] != DBNull.Value)
                                metadata.Desc = (string)reader["Desc"];
                        }
                    }
                }

                return null;
            }, ShouldThrowOnDbOperationException);
            metadata.VariantNames = variants.ToArray();

            if (string.IsNullOrWhiteSpace(variantRef))
                throw new ArgumentException("variantName");

            // Get metadata for variant
            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[CountClassItems]";

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = className;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = variantName;

                metadata.Count = (int)cmd.ExecuteScalar();


                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[PeekClassItems]";
                cmd.Parameters.Clear();

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = className;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = variantName;
                cmd.Parameters.Add("@NumItems",     SqlDbType.Int).Value        = 0;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int numDims = reader.FieldCount;
                    metadata.DimensionNames = new string[numDims-1];
                    metadata.DimensionTypes = new Type[numDims-1];

                    for (int j = 1; j < numDims; j++)
                    {
                        metadata.DimensionNames[j-1] = reader.GetName(j);
                        metadata.DimensionTypes[j-1] = reader.GetFieldType(j);
                    }
                }

                return null;
            }, ShouldThrowOnDbOperationException);

            return metadata;
        }

        public object[] GetItem(string className, string variantName, int indx)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            return (object[])SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[GetClassItem]";

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = className;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = variantName;
                cmd.Parameters.Add("@ItemId",       SqlDbType.Int).Value        = indx;

                object[] item;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int numDims = reader.FieldCount;
                        item = new object[numDims-1];
                        for (int j = 1; j < numDims; j++)
                        {
                            item[j-1] = reader[j];
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("indx");
                    }
                }
                return item;
            }, ShouldThrowOnDbOperationException);
        }

        #endregion IClassDataProvider


        #region IClassDataManager

        public void AddVariant(ClassMetadata metadata)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.Class))
                    throw new ArgumentNullException("metadata.Class");

                if (string.IsNullOrWhiteSpace(metadata.Variant))
                    throw new ArgumentNullException("metadata.Variant");

                if (null == metadata.DimensionNames)
                    throw new ArgumentNullException("metadata.DimensionNames");

                if (null == metadata.DimensionTypes)
                    throw new ArgumentNullException("metadata.DimensionTypes");

                int dimNamesLength = metadata.DimensionNames.Length,
                    dimTypesLength = metadata.DimensionTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.DimensionNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.DimensionTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Dimensions");
            }

            string status = (string)SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[AddClassVariant]";

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = metadata.Class;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = metadata.Variant;
                cmd.Parameters.Add("@Desc",         SqlDbType.VarChar).Value    = metadata.Desc == null ? (object)DBNull.Value : metadata.Desc;

                object retVal = cmd.ExecuteScalar();
                if (retVal == DBNull.Value)
                    return null;

                string variantRef = (string)retVal;
                int numDims = metadata.DimensionNames.Length;

                {
                    StringBuilder cmdText = new StringBuilder();
                    cmdText.AppendFormat("CREATE TABLE [RefCorpus].[{0}] ([Id] BIGINT IDENTITY(0,1) PRIMARY KEY", variantRef);
                    for (int j = 0; j < numDims; j++)
                    {
                        cmdText.AppendFormat(", [{0}] {1}", metadata.DimensionNames[j], SqlUtility.ToSqlType(metadata.DimensionTypes[j]));
                    }
                    cmdText.Append(");");

                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = cmdText.ToString();
                    cmd.Parameters.Clear();

                    cmd.ExecuteNonQuery();
                }

                foreach (string dimName in Enumerable.Range(0, numDims)
                        .Where(j => typeof(byte[]) != metadata.DimensionTypes[j] && typeof(string) != metadata.DimensionTypes[j])
                        .Select(j => metadata.DimensionNames[j]))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = string.Format(
                        "CREATE NONCLUSTERED INDEX [IDX_{0}_{1}_{2}] ON [RefCorpus].[{3}]([{4}] ASC);",
                        variantRef.Substring(0, Math.Min(variantRef.Length, 60)),
                        dimName.Substring(0, Math.Min(dimName.Length, 30)),
                        Guid.NewGuid().ToString("N"),
                        variantRef,
                        dimName);
                    cmd.Parameters.Clear();

                    cmd.ExecuteNonQuery();
                }

                // Consider adding full-text index for varchar and varbinary columns.

                return variantRef;
            }, ShouldThrowOnDbOperationException);

            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("metadata");
        }

        public bool VariantExists(string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            string variantRef = this.GetVariantRef(className, variantName);
            return (!string.IsNullOrWhiteSpace(variantRef));
        }

        public void AddItem(ClassMetadata metadata, object[] item)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.Class))
                    throw new ArgumentNullException("metadata.Class");

                if (string.IsNullOrWhiteSpace(metadata.Variant))
                    throw new ArgumentNullException("metadata.Variant");

                if (null == metadata.DimensionNames)
                    throw new ArgumentNullException("metadata.DimensionNames");

                if (null == metadata.DimensionTypes)
                    throw new ArgumentNullException("metadata.DimensionTypes");

                int dimNamesLength = metadata.DimensionNames.Length,
                    dimTypesLength = metadata.DimensionTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.DimensionNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.DimensionTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Dimensions");

                if (null == item)
                    throw new ArgumentNullException("item");

                if (item.Length != dimNamesLength)
                    throw new ArgumentOutOfRangeException("item.Length");
            }

            string variantRef = this.GetVariantRef(metadata.Class, metadata.Variant);

            if (string.IsNullOrWhiteSpace(variantRef))
                throw new ArgumentException("metadata");

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                int numDims = metadata.DimensionNames.Length;

                StringBuilder cmdText = new StringBuilder();
                cmdText.AppendFormat("INSERT INTO [RefCorpus].[{0}] (", variantRef);
                for (int j = 0; j < numDims; j++)
                {
                    cmdText.AppendFormat("[{0}]", metadata.DimensionNames[j]);
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

                for (int j = 0; j < numDims; j++)
                {
                    cmd.Parameters.Add("@P_" + j, SqlUtility.ToSqlDbType(metadata.DimensionTypes[j]))
                        .Value = (null == item[j] ? DBNull.Value : item[j]);
                }

                return cmd.ExecuteNonQuery();
            }, ShouldThrowOnDbOperationException);
        }

        public void ClearItems(string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            string variantRef = this.GetVariantRef(className, variantName);

            if (string.IsNullOrWhiteSpace(variantRef))
                throw new ArgumentException("metadata");

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "TRUNCATE TABLE [RefCorpus].[{0}];",
                    variantRef);

                cmd.ExecuteNonQuery();


                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "DBCC CHECKIDENT('[RefCorpus].[{0}]', RESEED, 0);",
                    variantRef);
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();

                return null;
            }, ShouldThrowOnDbOperationException);
        }

        public void RemoveVariant(string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            string variantRef = this.GetVariantRef(className, variantName);

            if (string.IsNullOrWhiteSpace(variantRef))
                throw new ArgumentException("metadata");

            SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[RemoveClassVariant]";

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = className;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = variantName;

                cmd.ExecuteNonQuery();


                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format(
                    "DROP TABLE [RefCorpus].[{0}];",
                    variantRef);
                cmd.Parameters.Clear();

                cmd.ExecuteNonQuery();

                return null;
            }, ShouldThrowOnDbOperationException);
        }

        #endregion IClassDataManager


        #region InternalOps

        private string GetVariantRef(string className, string variantName)
        {
            return (string)SqlUtility.ExecuteDbOperation(this.DbConnString, (SqlCommand cmd) =>
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[RefCorpus].[VariantRef]";

                cmd.Parameters.Add("@ClassName",    SqlDbType.VarChar).Value    = className;
                cmd.Parameters.Add("@VariantName",  SqlDbType.VarChar).Value    = variantName;

                cmd.Parameters.Add("@Ref", SqlDbType.VarChar).Direction = ParameterDirection.ReturnValue;

                cmd.ExecuteNonQuery();
                string variantRef = (cmd.Parameters["@Ref"].Value == DBNull.Value ? null : (string)cmd.Parameters["@Ref"].Value);

                return variantRef;
            }, ShouldThrowOnDbOperationException);
        }

        private static bool ShouldThrowOnDbOperationException(Exception ex)
        {
            return !(ex is SqlException);
        }

        #endregion InternalOps
    }
}
