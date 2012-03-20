using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MDo.Common.App
{
    public interface IConsoleAppModule
    {
        string Name { get; }
        void Run(string[] args);
        void PrintUsage();
    }

    public abstract class ConsoleAppModule : IConsoleAppModule
    {
        public virtual string Name
        {
            get { return this.GetType().Name; }
        }

        public abstract void Run(string[] args);

        public virtual void PrintUsage()
        {
            Console.WriteLine("{0}: No cmdline parameters required or accepted.", this.Name);
        }
    }

    public class ConsoleAppMaster : IConsoleAppModule
    {
        #region Constants

        private const int NumRequiredArgs = 1;

        public static readonly string[] CmdLineArg_Help     = { "HELP", "/?", "-?"    };
        public static readonly string[] CmdLineArg_InFile   = { "IN",  "/IN",  "-IN"  };
        public static readonly string[] CmdLineArg_OutFile  = { "OUT", "/OUT", "-OUT" };
        public static readonly string[] CmdLineArg_ErrFile  = { "ERR", "/ERR", "-ERR" };

        #endregion Constants


        #region Fields & Properties
        
        protected readonly IList<IConsoleAppModule> Modules = new List<IConsoleAppModule>();
        protected readonly ISet<string> ModuleNames = new HashSet<string>();

        #endregion Fields & Properties


        #region IConsoleAppModule

        public string Name
        {
            get { return this.GetType().Name; }
        }

        public virtual void Run(string[] args)
        {
            ICollection<string> mArgs = new List<string>();
            bool help = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (CmdLineArg_Help.Any(expectedArg => expectedArg.Equals(args[i], StringComparison.OrdinalIgnoreCase)))
                {
                    help = true;
                }
                else if (CmdLineArg_InFile.Any(expectedArg => expectedArg.Equals(args[i], StringComparison.OrdinalIgnoreCase)))
                {
                    string inFile = args[++i];
                    try
                    {
                        Console.SetIn(new StreamReader(File.OpenRead(inFile)));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("IN: Unable to open '{0}' for reading; using stdin. The error was: {1}", inFile, ex.ToString());
                    }
                }
                else if (CmdLineArg_OutFile.Any(expectedArg => expectedArg.Equals(args[i], StringComparison.OrdinalIgnoreCase)))
                {
                    string outFile = args[++i];
                    try
                    {
                        Console.SetOut(new StreamWriter(new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.Read)));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("OUT: Unable to open '{0}' for writing; using stdout. The error was: {1}", outFile, ex.ToString());
                    }
                }
                else if (CmdLineArg_ErrFile.Any(expectedArg => expectedArg.Equals(args[i], StringComparison.OrdinalIgnoreCase)))
                {
                    string errFile = args[++i];
                    try
                    {
                        Console.SetError(new StreamWriter(new FileStream(errFile, FileMode.Create, FileAccess.Write, FileShare.Read)));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("ERR: Unable to open '{0}' for writing; using stderr. The error was: {1}", errFile, ex.ToString());
                    }
                }
                else
                {
                    mArgs.Add(args[i]);
                }
            }
            args = mArgs.ToArray();

            string moduleName = null;
            IConsoleAppModule module = null;
            if (args.Length < NumRequiredArgs)
            {
                if (!help)
                    return;
            }
            else
            {
                string[] moduleNameAndId = args[0].Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                moduleName = StandardizedModuleName(moduleNameAndId[0].Trim());
                if (!this.ModuleNames.Contains(moduleName))
                {
                    Console.Error.WriteLine("Module {0} not found.", moduleName);
                    return;
                }
                IList<IConsoleAppModule> modules = this.Modules.Where(item => StandardizedModuleName(item.Name) == moduleName).ToList();
                if (modules.Count > 1)
                {
                    int moduleId;
                    if (moduleNameAndId.Length < 2 || !int.TryParse(moduleNameAndId[1], out moduleId))
                    {
                        Console.WriteLine("There are {0} modules named {0}. Select the module you want to run:", modules.Count, moduleName);
                        while (true)
                        {
                            Console.WriteLine("#ID\tModule");
                            for (int i = 0; i < modules.Count; i++)
                            {
                                Console.WriteLine("{0}\t{1}", i, modules[i].GetType().FullName);
                            }
                            Console.Write("Module #ID: ");
                            string moduleSelection = Console.ReadLine().Trim();
                            try
                            {
                                module = GetModuleFromIndexString(modules, moduleSelection);
                                break;
                            }
                            catch (ArgumentException ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    else
                    {
                        module = modules[moduleId];
                    }
                }
                else
                {
                    module = modules.First();
                }
            }
            args = args.Skip(NumRequiredArgs).ToArray();

            if (help)
            {
                if (module == null)
                {
                    this.PrintUsage();
                }
                else
                {
                    module.PrintUsage();
                }
                return;
            }

            this.RunModule(module, args);
        }

        public void PrintUsage()
        {
            string entryBinary = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine("=========================");
            Console.WriteLine(entryBinary + " [OptionalArgs] [ModuleName]#[ModuleIndx] [ModuleArgs]");
            Console.WriteLine("-------------------------");
            Console.WriteLine("  [OptionalArgs] :=");
            Console.WriteLine("      " + CmdLineArg_Help[0] + ": Prints help.");
            Console.WriteLine("      " + CmdLineArg_InFile[0]  + " [InFile]: Uses [InFile] as stdin.");
            Console.WriteLine("      " + CmdLineArg_OutFile[0] + " [OutFile]: Uses [OutFile] as stdout.");
            Console.WriteLine("      " + CmdLineArg_ErrFile[0] + " [ErrFile]: Uses [ErrFile] as stderr.");
            Console.WriteLine("  [ModuleName] := A registered module.");
            Console.WriteLine("  [ModuleIndx] := Optionally, specify a 0-based index, if multiple modules have the same name.");
            foreach (string moduleName in this.ModuleNames)
            {
                Console.WriteLine("      " + moduleName);
            }
            Console.WriteLine("  [ModuleArgs] := Module arguments. For more info: " + entryBinary + " " + CmdLineArg_Help[0] + " [ModuleName]");
            Console.WriteLine("=========================");
        }

        #endregion IConsoleAppModule


        public void RegisterModule(IConsoleAppModule module, bool allowDuplicateNames = false)
        {
            if (null == module)
                throw new ArgumentNullException("module");

            string moduleName = module.Name.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentNullException("module.Name");

            if (this.ModuleNames.Contains(moduleName))
            {
                if (!allowDuplicateNames)
                    throw new InvalidOperationException(string.Format("A module is already registered as {0}.", moduleName));
            }
            else
            {
                this.ModuleNames.Add(moduleName);
            }
            this.Modules.Add(module);
        }

        protected void RunModule(IConsoleAppModule module, string[] args, bool time = true)
        {
            DateTime start = DateTime.Now;
            if (time)
            {
                Console.WriteLine("Module Start  : {0}", start);
            }
            try
            {
                module.Run(args);
            }
            catch (ArgumentMissingException ex)
            {
                Console.Error.WriteLine("Module {0}: Missing argument '{1}'.", module.Name, ex.ParameterName);
                module.PrintUsage();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                Console.Out.Flush();
                Console.Error.Flush();
            }
            if (time)
            {
                DateTime end = DateTime.Now;
                Console.WriteLine("Module End    : {0}", end);
                Console.WriteLine("Module Elapsed: {0}", end - start);
            }
        }

        protected static string StandardizedModuleName(IConsoleAppModule module)
        {
            return StandardizedModuleName(module.Name);
        }

        protected static string StandardizedModuleName(string moduleName)
        {
            return moduleName.ToUpperInvariant();
        }

        protected static IConsoleAppModule GetModuleFromIndexString(IList<IConsoleAppModule> modules, string moduleIndexString)
        {
            if (null == modules || modules.Count == 0)
                throw new ArgumentException("No modules are found.", "modules");
            int moduleId;
            if (!int.TryParse(moduleIndexString, out moduleId) || moduleId < 0 || moduleId >= modules.Count)
                throw new ArgumentException(
                    string.Format("{0} is not a valid Module #ID; expecting a number [0..{1}].", moduleIndexString, modules.Count - 1),
                    "moduleIndexString");
            return modules[moduleId];
        }
    }
}
