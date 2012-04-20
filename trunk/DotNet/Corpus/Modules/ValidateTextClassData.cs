using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.App.CLI;

namespace MDo.Data.Corpus.Modules
{
    internal class ValidateTextClassData : ConsoleAppModule
    {
        public static void Run(string baseDir, string className = null, string variantName = null)
        {
            if (string.IsNullOrEmpty(baseDir))
                throw new ArgumentNullException("baseDir");

            IClassDataProvider metaReader = new TextClassDataReader(baseDir);
            if (string.IsNullOrEmpty(className))
            {
                foreach (string cls in metaReader.ListClasses())
                {
                    Run(baseDir, cls, variantName);
                }
            }
            else if (string.IsNullOrEmpty(variantName))
            {
                foreach (string variant in metaReader.ListVariants(className))
                {
                    Run(baseDir, className, variant);
                }
            }
            else
            {
                Console.WriteLine("============================================================");
                Console.WriteLine("Validating text classification data {0}/{1}.{2}...", baseDir, className, variantName);
                try
                {
                    IClassDataProvider provider = new TextClassDataReader(baseDir);
                    bool isValid = true;
                    ClassMetadata metadata = null;
                    try
                    {
                        metadata = provider.GetMetadata(className, variantName);
                    }
                    catch (Exception ex)
                    {
                        isValid = false;
                        Console.WriteLine("\t{0}/{1}.{2}: Invalid header: {3}", baseDir, className, variantName, ex.Message);
                    }
                    if (isValid)
                    {
                        for (int i = 0; i < metadata.NumItems; i++)
                        {
                            try
                            {
                                object[] item = provider.GetItem(className, variantName, i);
                            }
                            catch
                            {
                                isValid = false;
                                Console.WriteLine("\t{0}/{1}.{2}: Invalid Item #{3}", baseDir, className, variantName, i);
                            }
                        }
                    }
                    Console.WriteLine("Text classification data {0}/{1}.{2} is {3}.", baseDir, className, variantName, isValid ? "valid" : "invalid");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: {0}", ex.ToString());
                }
            }
        }

        public override void Run(string[] args)
        {
            string baseDir = string.Empty, className = string.Empty, variantName = string.Empty;

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

                        case "CLS":
                            className = kv[1].Trim();
                            break;

                        case "VAR":
                            variantName = kv[1].Trim();
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

            Run(baseDir, className, variantName);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [DIR=(baseDir)] [OPT: CLS=(className)] [OPT: VAR=(variantName)]", this.Name);
        }
    }
}
