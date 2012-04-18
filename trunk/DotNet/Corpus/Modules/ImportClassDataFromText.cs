using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.App.CLI;

using MDo.Data.Corpus.DataImport;

namespace MDo.Data.Corpus.Modules
{
    public class ImportClassDataFromText : ConsoleAppModule
    {
        public static void Run(string baseDir, string server, string database, string userId, string password, bool encrypt = true)
        {
            SqlUtility.EncryptConnection = encrypt;
            IClassDataManager destStore = new SqlClassDataManager(SqlUtility.GetSqlConnectionString(server, database, userId, password));
            ClassDataTextImporter metaReader = new ClassDataTextImporter(baseDir);
            foreach (string className in metaReader.ListClasses())
            {
                foreach (string variantName in metaReader.ListVariants(className))
                {
                    Console.Write("Importing {0}.{1}...", className, variantName);
                    IClassDataImporter srcStore = new ClassDataTextCachedImporter(baseDir, className, variantName);
                    srcStore.Import(className, variantName, destStore);
                    Console.WriteLine(" Done.");
                }
            }
        }

        public override void Run(string[] args)
        {
            string baseDir = string.Empty;
            string server = string.Empty, database = string.Empty,
                   userId = string.Empty, password = string.Empty;
            bool encrypt = true;

            if (null != args && args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string[] kv = arg.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2)
                        throw new ArgumentException(string.Format(
                            "Invalid argument: {0}",
                            arg));

                    switch (kv[0].Trim().ToUpperInvariant())
                    {
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
                Console.Write("Integrated Security will be used. Proceed (Y/N/Q)? ");
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

                    case ConsoleKey.Q:
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

            if (userId.ToUpperInvariant() == "(WAUTH)")
                userId = string.Empty;

            while (string.Empty == password && !string.IsNullOrEmpty(userId))
            {
                password = ConsoleAppUtil.GetInteractiveInput("Password", false);
                if (null == password)
                    return;
                password = password.Trim();
            }

            Run(baseDir, server, database, userId, password, encrypt);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [DIR=(baseDir)] [SRV=(server)] [DB=(database)] [UID=(userId)] [PWD=(password)] [OPT: ENCRYPT=(true/false)]", this.Name);
            Console.WriteLine("\tUID=(WAuth) to use Integrated Security.");
        }
    }
}
