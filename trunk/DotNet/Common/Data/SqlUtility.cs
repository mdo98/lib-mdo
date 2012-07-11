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

namespace MDo.Common.Data
{
    public static class SqlUtility
    {
        public const string SqlServer_MasterDb = "master";

        public static bool EncryptConnection = false;
        private static int CommandTimeoutInSeconds = 600;

        public static bool DatabaseExists(string server, string database, string userId, string password)
        {
            return (bool)ExecuteSqlCommand(
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
                });
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
                ExecuteSqlCommand(
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
                    false);
            }
        }

        public static void DropDatabase(string server, string database, string userId, string password)
        {
            if (DatabaseExists(server, database, userId, password))
            {
                ExecuteSqlCommand(
                    GetSqlConnectionString(server, SqlServer_MasterDb, userId, password),
                    (SqlCommand cmd) =>
                    {
                        cmd.CommandType = CommandType.Text;

                        try
                        {
                            cmd.CommandText = string.Format(
                                "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;",
                                database);

                            cmd.ExecuteNonQuery();
                        }
                        catch { }

                        cmd.CommandText = string.Format(
                            "DROP DATABASE [{0}];",
                            database);

                        return cmd.ExecuteNonQuery();
                    },
                    false);
            }
        }

        public static void CreateSqlLogin(string server, string userId, string password, string newLogin, string newPassword)
        {
            ExecuteSqlCommand(
                GetSqlConnectionString(server, SqlServer_MasterDb, userId, password),
                (SqlCommand cmd) =>
                {
                    bool loginExists = false;

                    cmd.CommandText = string.Format(
                        "SELECT 1 FROM [sys].[sql_logins] WHERE name = '{0}';",
                        SqlEscapeStringValue(newLogin));

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            loginExists = true;
                        }
                    }

                    if (loginExists)
                    {
                        cmd.CommandText = string.Format(
                            "ALTER LOGIN [{0}] WITH PASSWORD = '{1}';",
                            newLogin,
                            SqlEscapeStringValue(newPassword));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = string.Format(
                            "ALTER LOGIN [{0}] ENABLE;",
                            newLogin);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        cmd.CommandText = string.Format(
                            "CREATE LOGIN [{0}] WITH PASSWORD = '{1}';",
                            newLogin,
                            SqlEscapeStringValue(newPassword));
                        cmd.ExecuteNonQuery();
                    }
                    return null;
                },
                false);
        }

        public static void CreateSqlUser(string server, string database, string userId, string password, string userName, string fromLogin)
        {
            ExecuteSqlCommand(
                GetSqlConnectionString(server, database, userId, password),
                (SqlCommand cmd) =>
                {
                    cmd.CommandText = string.Format(
                        "IF EXISTS (SELECT 1 FROM [sys].[database_principals] WHERE [name] = '{0}')" + Environment.NewLine +
                        "BEGIN" + Environment.NewLine +
                        "  DROP USER [{1}];" + Environment.NewLine +
                        "END",
                        SqlEscapeStringValue(userName),
                        userName);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = string.Format(
                        "CREATE USER [{0}] FROM LOGIN [{1}];",
                        userName,
                        fromLogin);
                    cmd.ExecuteNonQuery();
                    return null;
                },
                false);
        }

        public static void GrantDbPermissionToUser(string server, string database, string userId, string password, string userName, params string[] permissions)
        {
            if (null != permissions)
            {
                ExecuteSqlCommand(
                    GetSqlConnectionString(server, database, userId, password),
                    (SqlCommand cmd) =>
                    {
                        foreach (string permission in permissions)
                        {
                            cmd.CommandText = string.Format(
                                "GRANT {0} TO [{1}];",
                                permission,
                                userName);
                            cmd.ExecuteNonQuery();
                        }
                        return null;
                    });
            }
        }

        public static void AddDbRoleMembershipToUser(string server, string database, string userId, string password, string userName, params string[] roleNames)
        {
            if (null != roleNames)
            {
                ExecuteSqlCommand(
                    GetSqlConnectionString(server, database, userId, password),
                    (SqlCommand cmd) =>
                    {
                        foreach (string roleName in roleNames)
                        {
                            cmd.CommandText = string.Format(
                                "EXEC sp_addrolemember '{0}', '{1}';",
                                roleName,
                                SqlEscapeStringValue(userName));
                            cmd.ExecuteNonQuery();
                        }
                        return null;
                    });
            }
        }

        public static void ExecuteSqlScript(string connString, string sqlScript)
        {
            using (SqlConnection conn = new SqlConnection(GetSqlConnectionString(connString)))
            {
                conn.Open();
                Server sqlServer = new Server(new ServerConnection(conn));
                sqlServer.ConnectionContext.ExecuteNonQuery(sqlScript);
            }
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

        private const int
            QuickNap    = 500,      //  0.5 second
            ShortSleep  = 5000,     //  5   seconds
            LongSleep   = 15000;    // 15   seconds

        // Based on http://windowsazurecat.com/2010/10/best-practices-for-handling-transient-conditions-in-sql-azure-client-applications/
        private static readonly IDictionary<int, int> RetryableSqlErrorCodes = new Dictionary<int, int>()
        {
            /* Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding. */
            { -2, ShortSleep },

            /* The instance of SQL Server you attempted to connect to does not support encryption. */
            { 20, QuickNap },

            /* A connection was successfully established with the server, but then an error occurred during the login process.
             * (provider: TCP Provider, error: 0 – The specified network name is no longer available.) */
            { 64, QuickNap },

            /* The client was unable to establish a connection because of an error during connection initialization process before login.
             * Possible causes include the following: the client tried to connect to an unsupported version of SQL Server; the server was
             * too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum allowed connections)
             * on the server. (provider: TCP Provider, error: 0 – An existing connection was forcibly closed by the remote host.) */
            { 233, ShortSleep },

            /* Process ID %d attempted to unlock a resource it does not own: %.*ls. Retry the transaction, because this error may be caused
             * by a timing condition. If the problem persists, contact the database administrator. */
            { 1203, QuickNap },

            /* The instance of the SQL Server Database Engine cannot obtain a LOCK resource at this time. Rerun your statement when there are
             * fewer active users. Ask the database administrator to check the lock and memory configuration for this instance, or to check
             * for long-running transactions. */
            { 1204, QuickNap },

            /* Transaction (Process ID %d) was deadlocked on %.*ls resources with another process and has been chosen as the deadlock victim.
             * Rerun the transaction. */
            { 1205, QuickNap },

            /* The Microsoft Distributed Transaction Coordinator (MS DTC) has cancelled the distributed transaction. */
            { 1206, QuickNap },

            /* A transport-level error has occurred when receiving results from the server. An established connection was aborted by the
             * software in your host machine. */
            { 10053, QuickNap },

            /* A transport-level error has occurred when sending the request to the server. (provider: TCP Provider, error: 0 - An existing
             * connection was forcibly closed by the remote host.) */
            { 10054, QuickNap },

            /* A network-related or instance-specific error occurred while establishing a connection to SQL Server. The server was not found
             * or was not accessible. Verify that the instance name is correct and that SQL Server is configured to allow remote connections.
             * (provider: TCP Provider, error: 0 - A connection attempt failed because the connected party did not properly respond after a
             * period of time, or established connection failed because connected host has failed to respond.) */
            { 10060, QuickNap },

            /* The service has encountered an error processing your request. Please try again. */
            { 40143, QuickNap },

            /* The service has encountered an error processing your request. Please try again. Error code %d. */
            { 40197, QuickNap },

            /* The service is currently busy. Retry the request after 10 seconds. Incident ID: %ls. Code: %d. */
            { 40501, LongSleep },

            /* Database '%.*ls' on server '%.*ls' is not currently available. Please retry the connection later. If the problem persists,
             * contact customer support, and provide them the session tracing ID of '%.*ls'. */
            { 40613, ShortSleep },
        };

        private static bool ShouldRetry(Exception ex)
        {
            if (ex is SqlException)
            {
                return (ex as SqlException).Errors.Cast<SqlError>().Any(item => RetryableSqlErrorCodes.Keys.Contains(item.Number));
            }
            else
            {
                return false;
            }
        }

        private static void Recover(Exception ex)
        {
            SqlException sqlEx = ex as SqlException;
            if (null != sqlEx)
            {
                int sleepTime = 0;
                foreach (SqlError error in sqlEx.Errors)
                {
                    if (RetryableSqlErrorCodes.ContainsKey(error.Number))
                    {
                        int s = RetryableSqlErrorCodes[error.Number];
                        if (sleepTime < s)
                            sleepTime = s;
                    }
                }
                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        private static readonly RetryStrategy DefaultSqlRetryStrategy = new RetryStrategy()
        {
            MaxTries = RetryStrategy.DefaultMaxTries,
            IsRetryable = ShouldRetry,
            RecoveryAction = Recover,
        };

        public static object ExecuteSqlCommand(string connString, Func<SqlCommand, object> executeSqlCommand,
            bool transactable = true, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            return ExecuteSqlCommand(connString, executeSqlCommand, DefaultSqlRetryStrategy, transactable, isolationLevel);
        }

        public static object ExecuteSqlCommand(string connString, Func<SqlCommand, object> executeSqlCommand, RetryStrategy retryStrategy,
            bool transactable = true, IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            connString = GetSqlConnectionString(connString);
            return retryStrategy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlTransaction transaction = null;
                    if (transactable)
                        transaction = conn.BeginTransaction(isolationLevel);
                    try
                    {
                        object result;
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandTimeout = CommandTimeoutInSeconds;
                            result = executeSqlCommand(cmd);
                        }
                        if (transactable)
                            transaction.Commit();
                        return result;
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
                        conn.Close();
                    }
                }
            });
        }

        public static object ExecuteSqlOperation(string connString, Func<SqlConnection, object> executeSqlOperation)
        {
            return ExecuteSqlOperation(connString, executeSqlOperation, DefaultSqlRetryStrategy);
        }

        public static object ExecuteSqlOperation(string connString, Func<SqlConnection, object> executeSqlOperation, RetryStrategy retryStrategy)
        {
            connString = GetSqlConnectionString(connString);
            return retryStrategy.Execute(() =>
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    try
                    {
                        return executeSqlOperation(conn);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            });
        }

        public static void SetCommandTimeout(int timeoutSeconds)
        {
            if (timeoutSeconds < 0)
                throw new ArgumentOutOfRangeException("timeoutSeconds");

            CommandTimeoutInSeconds = timeoutSeconds;
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

            else if (typeof(double) == type || typeof(double?) == type)
                sqlType = "FLOAT";

            else if (typeof(int) == type || typeof(int?) == type)
                sqlType = "INT";

            else if (typeof(long) == type || typeof(long?) == type)
                sqlType = "BIGINT";

            else if (typeof(short) == type || typeof(short?) == type)
                sqlType = "SMALLINT";

            else if (typeof(byte) == type || typeof(byte?) == type)
                sqlType = "TINYINT";

            else if (typeof(bool) == type || typeof(bool?) == type)
                sqlType = "BIT";

            else if (typeof(Guid) == type || typeof(Guid?) == type)
                sqlType = "UNIQUEIDENTIFIER";

            else if (typeof(DateTime) == type || typeof(DateTime?) == type)
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

            else if (typeof(double) == type || typeof(double?) == type)
                sqlType = SqlDbType.Float;

            else if (typeof(int) == type || typeof(int?) == type)
                sqlType = SqlDbType.Int;

            else if (typeof(long) == type || typeof(long?) == type)
                sqlType = SqlDbType.BigInt;

            else if (typeof(short) == type || typeof(short?) == type)
                sqlType = SqlDbType.SmallInt;

            else if (typeof(byte) == type || typeof(byte?) == type)
                sqlType = SqlDbType.TinyInt;

            else if (typeof(bool) == type || typeof(bool?) == type)
                sqlType = SqlDbType.Bit;

            else if (typeof(Guid) == type || typeof(Guid?) == type)
                sqlType = SqlDbType.UniqueIdentifier;

            else if (typeof(DateTime) == type || typeof(DateTime?) == type)
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
            else if (typeof(double?)    == type
                 ||  typeof(int?)       == type
                 ||  typeof(long?)      == type
                 ||  typeof(short?)     == type
                 ||  typeof(byte?)      == type
                 ||  typeof(bool?)      == type
                 ||  typeof(Guid?)      == type
                 ||  typeof(DateTime?)  == type)
            {
                obj = string.IsNullOrWhiteSpace(str) ? null : type.GetGenericArguments()[0]
                    .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null)
                    .Invoke(null, new object[] { str.Trim() });
            }
            else if (typeof(double)     == type
                 ||  typeof(int)        == type
                 ||  typeof(long)       == type
                 ||  typeof(short)      == type
                 ||  typeof(byte)       == type
                 ||  typeof(bool)       == type
                 ||  typeof(Guid)       == type
                 ||  typeof(DateTime)   == type)
            {
                obj = type
                    .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null)
                    .Invoke(null, new object[] { str.Trim() });
            }
            else if (typeof(byte[]) == type)
            {
                byte[] b = null;
                if (!string.IsNullOrWhiteSpace(str))
                {
                    string[] bStr = str.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    b = new byte[bStr.Length];
                    for (int i = 0; i < bStr.Length; i++)
                    {
                        b[i] = byte.Parse(bStr[i], System.Globalization.NumberStyles.HexNumber);
                    }
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
