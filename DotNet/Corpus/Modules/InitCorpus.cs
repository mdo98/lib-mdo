using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

using MDo.Common.App.CLI;
using MDo.Data.Corpus.DataImport;

namespace MDo.Data.Corpus.Modules
{
    internal class InitCorpus : ConsoleAppModule
    {
        public static void Run(string server, string database, string userId, string password, bool encrypt = true)
        {
            SqlUtility.EncryptConnection = encrypt;

            Console.Write("Creating database {0}/{1} for RefCorpus...", server, database);
            SqlUtility.CreateDatabase(server, database, userId, password, true);
            Console.WriteLine(" Done.");
            
            string sqlScript;
            Type sqlClassDataMgrType = typeof(SqlClassDataManager);
            using (Stream sqlScriptStream = sqlClassDataMgrType.Assembly.GetManifestResourceStream(string.Format(
                   "{0}.{1}", sqlClassDataMgrType.Namespace, SqlClassDataManager.CorpusDBInitScript)))
            {
                using (TextReader reader = new StreamReader(sqlScriptStream))
                {
                    sqlScript = reader.ReadToEnd();
                }
            }
            Console.Write("Provisioning RefCorpus database at {0}/{1}...", server, database);
            SqlUtility.ExecuteSqlScript(server, database, userId, password, sqlScript);
            Console.WriteLine(" Done.");
        }

        public override void Run(string[] args)
        {
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
                password = ConsoleAppUtil.GetInteractiveInput("Password", true);
                if (null == password)
                    return;
                password = password.Trim();
            }

            Run(server, database, userId, password, encrypt);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [SRV=(server)] [DB=(database)] [UID=(userId)] [PWD=(password)] [OPT: ENCRYPT=(true/false)]", this.Name);
            Console.WriteLine("\tUID=(WAuth) to use Integrated Security.");
        }
    }
}
