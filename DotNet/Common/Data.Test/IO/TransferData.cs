using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.App.CLI;

namespace MDo.Common.Data.IO.Test
{
    public class TransferData : ConsoleAppModule
    {
        public static void Run(
            bool toDb, string baseDir, string server, string database, string userId, string password,
            bool append = false, bool encrypt = false)
        {
            bool encryptState = SqlUtility.EncryptConnection;
            SqlUtility.EncryptConnection = encrypt;
            IDataManager
                txtDataMgr = new TextDataManager(baseDir),
                sqlDataMgr = new SqlDataManager(SqlUtility.GetSqlConnectionString(server, database, userId, password));
            IDataProvider src;
            IDataManager dest;
            if (toDb)
            {
                src = txtDataMgr;
                dest = sqlDataMgr;
            }
            else
            {
                src = sqlDataMgr;
                dest = txtDataMgr;
            }
            DataIO.ImportData(src, dest, append);
            SqlUtility.EncryptConnection = encryptState;
        }

        public override void Run(string[] args)
        {
            bool toDb = false, txt2db = false, db2txt = false;
            string baseDir = string.Empty;
            string server = string.Empty, database = string.Empty,
                   userId = string.Empty, password = string.Empty;
            bool append = false, encrypt = false;

            if (null != args && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string[] kv = arg.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    switch (kv[0].Trim().ToUpperInvariant())
                    {
                        case "TXT2DB":
                            txt2db = true;
                            break;

                        case "DB2TXT":
                            db2txt = true;
                            break;

                        case "DIR":
                            baseDir = kv[1].Trim();
                            break;

                        case "SRV":
                            server = kv[1].Trim();
                            break;

                        case "DB":
                            database = kv[1].Trim();
                            break;

                        case "UID":
                            userId = kv[1].Trim();
                            break;

                        case "PWD":
                            password = kv[1].Trim();
                            break;

                        case "APPEND":
                            append = bool.Parse(kv[1].Trim());
                            break;

                        case "ENCRYPT":
                            encrypt = bool.Parse(kv[1].Trim());
                            break;

                        default:
                            throw new ArgumentException(string.Format(
                                "Switch {0} not recognized.",
                                kv[0]));
                    }
                }
            }

            if (txt2db ^ db2txt)
            {
                toDb = txt2db;
            }
            else
            {
                throw new ArgumentException("One and only one of TXT2DB or DB2TXT must be specified.");
            }

            while (string.Empty == baseDir)
            {
                baseDir = ConsoleAppUtil.GetInteractiveInput("Data Folder", true);
                if (null == baseDir)
                    return;
                baseDir = baseDir.Trim();
            }

            while (string.Empty == server)
            {
                server = ConsoleAppUtil.GetInteractiveInput("Server", true);
                if (null == server)
                    return;
                server = server.Trim();
            }

            while (string.Empty == database)
            {
                database = ConsoleAppUtil.GetInteractiveInput("Database", true);
                if (null == database)
                    return;
                database = database.Trim();
            }

            while (string.Empty == userId)
            {
                Console.Write("Integrated Security will be used. Proceed (Y/N/X)? ");
                var response = Console.ReadKey();
                Console.WriteLine();

                bool integratedSecurity = false;
                switch (response.Key)
                {
                    case ConsoleKey.Enter:
                    case ConsoleKey.Y:
                        integratedSecurity = true;
                        break;

                    case ConsoleKey.N:
                        break;

                    case ConsoleKey.X:
                        return;

                    default:
                        continue;
                }
                
                if (integratedSecurity)
                    break;

                userId = ConsoleAppUtil.GetInteractiveInput("User ID", true);
                if (null == userId)
                    return;
                userId = userId.Trim();
            }

            if (userId.Equals("(WAuth)", StringComparison.OrdinalIgnoreCase))
                userId = string.Empty;

            while (string.Empty == password && !string.IsNullOrEmpty(userId))
            {
                password = ConsoleAppUtil.GetInteractiveInput("Password", false);
                if (null == password)
                    return;
                password = password.Trim();
            }

            Run(toDb, baseDir, server, database, userId, password, append, encrypt);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [TXT2DB / DB2TXT] [DIR=(baseDir)] [SRV=(server)] [DB=(database)] [UID=(userId)] [PWD=(password)]", this.Name);
            Console.WriteLine("[OPT: APPEND=(true/FALSE)] [OPT: ENCRYPT=(true/FALSE)]");
            Console.WriteLine("\tUID=(WAuth) to use Integrated Security.");
        }
    }
}
