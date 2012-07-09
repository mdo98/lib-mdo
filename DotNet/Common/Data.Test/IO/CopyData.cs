using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.App.CLI;

namespace MDo.Common.Data.IO.Test
{
    public class CopyData : ConsoleAppModule
    {
        public static void Run(
            bool toDb, string baseDir, string server, string database, string userId, string password,
            ICollection<string> files,
            bool echoSource = false, bool appendDest = false, bool encrypt = false)
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
                if (!SqlUtility.DatabaseExists(server, database, userId, password))
                    SqlUtility.CreateDatabase(server, database, userId, password);

                src = txtDataMgr;
                dest = sqlDataMgr;
            }
            else
            {
                src = sqlDataMgr;
                dest = txtDataMgr;
            }
            CopyOptions options = new CopyOptions()
            {
                EchoSource = echoSource,
                AppendDest = appendDest,
            };

            if (null != files && files.Count > 0)
            {
                foreach (string file in files)
                {
                    string[] f = file.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    switch (f.Length)
                    {
                        case 1:
                            DataIO.CopyData(src, dest, options, f[0]);
                            break;

                        case 2:
                            DataIO.CopyData(src, dest, options, f[0], f[1] == "*" ? null : f[1]);
                            break;

                        default:
                            Console.Error.WriteLine("file: {0} is invalid.", file);
                            break;
                    }
                }
            }
            else
            {
                DataIO.CopyData(src, dest, options);
            }
            SqlUtility.EncryptConnection = encryptState;
        }

        public override void Run(string[] args)
        {
            bool toDb = false, txt2db = false, db2txt = false;
            string baseDir = string.Empty;
            string server = string.Empty, database = string.Empty,
                   userId = string.Empty, password = string.Empty;
            bool echoSource = false, appendDest = false, encrypt = false;
            ICollection<string> files = new List<string>();

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

                        case "F":
                            files.Add(kv[1].Trim());
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

                        case "ECHO":
                            echoSource = bool.Parse(kv[1].Trim());
                            break;

                        case "APPEND":
                            appendDest = bool.Parse(kv[1].Trim());
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

            Run(toDb, baseDir, server, database, userId, password, files, echoSource, appendDest, encrypt);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [TXT2DB / DB2TXT] [DIR=baseDir] [SRV=server] [DB=database] [UID=userId] [PWD=password]", this.Name);
            Console.WriteLine("[OPT: F=folder1 F=folder2/* F=folder3/file3-1 F=folder3/file3-2]");
            Console.WriteLine("[OPT: ECHO=true/FALSE] [OPT: APPEND=true/FALSE] [OPT: ENCRYPT=true/FALSE]");
            Console.WriteLine("\tUID=(WAuth) to use Integrated Security.");
        }
    }
}
