using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

using MDo.Common.App;

namespace MDo.Data.Corpus
{
    public static class SqlUtility
    {
        public const string SqlServer_MasterDb = "master";

        public static bool EncryptConnection = true;

        public static bool DatabaseExists(string server, string database, string userId, string password)
        {
            return (bool)ExecuteDbOperation(
                GetSqlConnectionString(server, SqlServer_MasterDb, userId, password),
                (SqlCommand cmd) =>
                {
                    cmd.CommandText = string.Format(
                        "SELECT 1 FROM [sys].[databases] WHERE name = '{0}';",
                        SqlEscapeStringValue(database));

                    bool dbExists = false;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            dbExists = true;
                        }
                    }
                    return dbExists;
                },
                (Exception ex) => false);
        }

        public static void CreateDatabase(string server, string database, string userId, string password, bool dropExisting = false, int? maxSizeInGB = null)
        {
            bool createDatabase = true;

            if (dropExisting)
                DropDatabase(server, database, userId, password);
            else if (DatabaseExists(server, database, userId, password))
                createDatabase = false;

            if (createDatabase)
            {
                ExecuteDbOperation(
                    GetSqlConnectionString(server, SqlServer_MasterDb, userId, password),
                    (SqlCommand cmd) =>
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = string.Format(
                            "CREATE DATABASE [{0}]{1};",
                            database,
                            maxSizeInGB.HasValue ? string.Format(" (MAXSIZE = {0} GB)", maxSizeInGB.Value.ToString()) : string.Empty);

                        return cmd.ExecuteNonQuery();
                    },
                    (Exception ex) => false,
                    false);
            }
        }

        public static void DropDatabase(string server, string database, string userId, string password)
        {
            if (DatabaseExists(server, database, userId, password))
            {
                ExecuteDbOperation(
                    GetSqlConnectionString(server, SqlServer_MasterDb, userId, password),
                    (SqlCommand cmd) =>
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = string.Format(
                            "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{0}];",
                            database);

                        return cmd.ExecuteNonQuery();
                    },
                    (Exception ex) => false,
                    false);
            }
        }

        public static string GetSqlConnectionString(string dbConnString)
        {
            SqlConnectionStringBuilder connString = new SqlConnectionStringBuilder(dbConnString);
            connString.MultipleActiveResultSets = true;
            connString.Encrypt = EncryptConnection;
            return connString.ToString();
        }

        public static string GetSqlConnectionString(string server, string database, string userId, string password)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException("server");

            if (string.IsNullOrEmpty(database))
                throw new ArgumentNullException("database");

            bool integratedSecurity = string.IsNullOrEmpty(userId);
            if (!integratedSecurity && null == password)
                throw new ArgumentNullException("password");

            SqlConnectionStringBuilder connString = new SqlConnectionStringBuilder();
            connString.DataSource = server;
            connString.InitialCatalog = database;
            if (integratedSecurity)
            {
                connString.IntegratedSecurity = true;
            }
            else
            {
                connString.UserID = userId;
                connString.Password = password;
            }
            connString.MultipleActiveResultSets = true;
            connString.Encrypt = EncryptConnection;
            return connString.ToString();
        }

        public static void ExecuteSqlScript(string server, string database, string userId, string password, string sqlScript)
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString(server, database, userId, password)))
            {
                conn.Open();
                Server sqlServer = new Server(new ServerConnection(conn));
                sqlServer.ConnectionContext.ExecuteNonQuery(sqlScript);
            }
        }

        public static object ExecuteDbOperation(string dbConnString, Func<SqlCommand, object> dbOperation, Func<Exception, bool> shouldThrow,
            IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            return ExecuteDbOperation(dbConnString, dbOperation, shouldThrow, true, isolationLevel);
        }

        public static object ExecuteDbOperation(string dbConnString, Func<SqlCommand, object> dbOperation, Func<Exception, bool> shouldThrow,
            bool transactable, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            dbConnString = GetSqlConnectionString(dbConnString);
            SqlConnection conn = new SqlConnection(dbConnString);
            conn.Open();
            object retVal = Utility.ExecuteWithRetry(() =>
                {
                    SqlTransaction transaction = null;
                    if (transactable)
                        transaction = conn.BeginTransaction(isolationLevel);
                    try
                    {
                        object dbOperationResult;
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandTimeout = 600;
                            dbOperationResult = dbOperation(cmd);
                        }
                        if (transactable)
                            transaction.Commit();
                        return dbOperationResult;
                    }
                    catch
                    {
                        if (transactable)
                            transaction.Rollback();
                        throw;
                    }
                    finally
                    {
                        if (transactable)
                            transaction.Dispose();
                    }
                },
                shouldThrow,
                () =>
                {
                    Thread.Sleep(500);
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Close();
                        conn = new SqlConnection(dbConnString);
                        conn.Open();
                    }
                });
            conn.Close();
            return retVal;
        }

        public static string SqlEscapeStringValue(string stringValue)
        {
            return stringValue.Replace("'", "''");
        }

        public static string ToSqlType(Type type)
        {
            string sqlType;

            if (typeof(string) == type)
                sqlType = "NVARCHAR(MAX)";

            else if (typeof(double) == type)
                sqlType = "FLOAT";

            else if (typeof(int) == type)
                sqlType = "INT";

            else if (typeof(long) == type)
                sqlType = "BIGINT";

            else if (typeof(short) == type)
                sqlType = "SMALLINT";

            else if (typeof(byte) == type)
                sqlType = "TINYINT";

            else if (typeof(bool) == type)
                sqlType = "BIT";

            else if (typeof(DateTime) == type)
                sqlType = "DATETIME";

            else if (typeof(byte[]) == type)
                sqlType = "VARBINARY(MAX)";

            else
                throw new NotSupportedException(type.FullName);

            return sqlType;
        }

        public static SqlDbType ToSqlDbType(Type type)
        {
            SqlDbType sqlType;

            if (typeof(string) == type)
                sqlType = SqlDbType.NVarChar;

            else if (typeof(double) == type)
                sqlType = SqlDbType.Float;

            else if (typeof(int) == type)
                sqlType = SqlDbType.Int;

            else if (typeof(long) == type)
                sqlType = SqlDbType.BigInt;

            else if (typeof(short) == type)
                sqlType = SqlDbType.SmallInt;

            else if (typeof(byte) == type)
                sqlType = SqlDbType.TinyInt;

            else if (typeof(bool) == type)
                sqlType = SqlDbType.Bit;

            else if (typeof(DateTime) == type)
                sqlType = SqlDbType.DateTime;

            else if (typeof(byte[]) == type)
                sqlType = SqlDbType.VarBinary;

            else
                throw new NotSupportedException(type.FullName);

            return sqlType;
        }

        public static object ToObject(string str, Type type)
        {
            object obj;
            if (typeof(string) == type)
            {
                obj = str;
            }
            else if (typeof(double)     == type
                 ||  typeof(int)        == type
                 ||  typeof(long)       == type
                 ||  typeof(short)      == type
                 ||  typeof(byte)       == type
                 ||  typeof(bool)       == type
                 ||  typeof(DateTime)   == type)
            {
                obj = type
                    .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null)
                    .Invoke(null, new object[] { str });
            }
            else if (typeof(byte[]) == type)
            {
                string[] bStr = str.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] b = new byte[bStr.Length];
                for (int i = 0; i < bStr.Length; i++)
                {
                    b[i] = byte.Parse(bStr[i], System.Globalization.NumberStyles.HexNumber);
                }
                obj = b;
            }
            else
            {
                throw new NotSupportedException(type.FullName);
            }
            return obj;
        }
    }
}
