#define TRACE

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System
{
    public class ConsoleAppBootstrapper : IConsoleAppModule
    {
        #region Constants

        private const int NumRequiredArgs = 1;

        public static readonly string[] CmdLineArg_Help = { "HELP", "/?", "-?" };
        public static readonly string[] CmdLineArg_InFile = { "IN", "/IN", "-IN" };
        public static readonly string[] CmdLineArg_OutFile = { "OUT", "/OUT", "-OUT" };
        public static readonly string[] CmdLineArg_ErrFile = { "ERR", "/ERR", "-ERR" };

        public const string Command_Enumerate = "ENUM";
        public const string Command_Help = "HELP";
        public const string Command_ToggleTiming = "TIME";
        public const string Command_Exit = "X";

        protected static readonly IDictionary<string, string> Commands = new SortedList<string, string>()
        {
            { StandardizeModuleName(Command_Enumerate),     "Enumerate all supported modules and commands" },
            { StandardizeModuleName(Command_Help),          "Print the argument list of a module (qualify with Module #ID)" },
            { StandardizeModuleName(Command_ToggleTiming),  "Toggle module timing" },
            { StandardizeModuleName(Command_Exit),          "Exit" },
        };

        #endregion Constants


        #region Fields

        protected readonly IList<IConsoleAppModule> Modules = new List<IConsoleAppModule>();
        protected readonly ISet<string> ModuleNames = new HashSet<string>();

        protected bool TimeModule = true;

        #endregion Fields


        #region Constructors

        public ConsoleAppBootstrapper(bool autoLoad = true)
        {
            if (autoLoad)
                this.LoadFrom(Assembly.GetCallingAssembly());
        }

        #endregion Constructors


        #region IConsoleAppModule

        public string Name
        {
            get { return this.GetType().Name; }
        }

        public int Run(string[] args)
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
                        Trace.TraceError("IN: Unable to open '{0}' for reading; using stdin. The error was: {1}", inFile, ex.ToString());
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
                        Trace.TraceError("OUT: Unable to open '{0}' for writing; using stdout. The error was: {1}", outFile, ex.ToString());
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
                        Trace.TraceError("ERR: Unable to open '{0}' for writing; using stderr. The error was: {1}", errFile, ex.ToString());
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
                {
                    this.EnterShellMode();
                    return (int)ReturnCode.Normal;
                }
            }
            else
            {
                string[] moduleNameAndId = args[0].Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
                moduleName = StandardizeModuleName(moduleNameAndId[0].Trim());
                if (!this.ModuleNames.Contains(moduleName))
                {
                    Trace.TraceError("Module {0} not found.", moduleName);
                    return (int)ReturnCode.ModuleNotFound;
                }
                IList<IConsoleAppModule> modules = this.Modules.Where(item => StandardizeModuleName(item.Name) == moduleName).ToList();
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
                return (int)ReturnCode.Normal;
            }

            return this.RunModule(module, args);
        }

        public void PrintUsage()
        {
            string entryBinary = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine("=========================");
            Console.WriteLine(entryBinary + " [OptionalArgs] [ModuleName]#[ModuleIndx] [ModuleArgs]");
            Console.WriteLine("-------------------------");
            Console.WriteLine("  [OptionalArgs] :=");
            Console.WriteLine("      " + CmdLineArg_Help[0] + ": Prints help.");
            Console.WriteLine("      " + CmdLineArg_InFile[0] + " [InFile]: Uses [InFile] as stdin.");
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

        protected int RunModule(IConsoleAppModule module, string[] args, bool time = true)
        {
            int returnCode;
            DateTime start = DateTime.Now;
            if (time)
            {
                Console.WriteLine("Module Start  : {0}", start);
            }
            try
            {
                returnCode = module.Run(args);
                Console.WriteLine("Module Return : {0}", returnCode);
            }
            catch (ArgumentMissingException ex)
            {
                Trace.TraceError("Module {0}: Missing argument '{1}'.", module.Name, ex.ParameterName);
                module.PrintUsage();
                returnCode = (int)ReturnCode.ArgumentMissing;
            }
            catch (Exception ex)
            {
                Trace.TraceError("An error occurred: {0}", ex.ToString());
                returnCode = (int)ReturnCode.Error;
            }
            if (time)
            {
                DateTime end = DateTime.Now;
                Console.WriteLine("Module End    : {0}", end);
                Console.WriteLine("Module Elapsed: {0}", end - start);
            }
            return returnCode;
        }

        protected void EnterShellMode()
        {
            this.ListModulesAndCommands();

            while (true)
            {
                Console.Write("> ");
                string cmd = Console.ReadLine().Trim();
                string[] cmdArgs = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cmdArgs.Length == 0)
                    continue;
                IConsoleAppModule module;
                string command = cmdArgs[0];
                cmdArgs = cmdArgs.Skip(1).ToArray();
                switch (StandardizeModuleName(command))
                {
                    case Command_Enumerate:
                        this.ListModulesAndCommands();
                        break;

                    case Command_Help:
                        if (cmdArgs.Length == 0)
                            break;
                        module = this.GetModule(cmdArgs[0]);
                        if (null != module)
                        {
                            module.PrintUsage();
                        }
                        break;

                    case Command_ToggleTiming:
                        this.TimeModule = !this.TimeModule;
                        Console.WriteLine("Module timing is {0}.", this.TimeModule ? "enabled" : "disabled");
                        break;

                    case Command_Exit:
                        return;

                    default:
                        module = this.GetModule(command);
                        if (null != module)
                        {
                            this.RunModule(module, cmdArgs, this.TimeModule);
                        }
                        break;
                }
            }
        }

        protected void LoadFrom(Assembly assembly)
        {
            Type moduleBaseType = typeof(IConsoleAppModule);
            foreach (Type type in assembly.GetTypes().Where(item => moduleBaseType.IsAssignableFrom(item)).OrderBy(item => item.Name))
            {
                this.RegisterModule(Activator.CreateInstance(type, true) as IConsoleAppModule, true);
            }
        }

        private IConsoleAppModule GetModule(string moduleCmd)
        {
            IConsoleAppModule module = null;
            try
            {
                module = GetModuleFromIndexString(this.Modules, moduleCmd);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("{0} for a list of supported modules and commands.", Command_Enumerate);
            }
            return module;
        }

        public void ListModulesAndCommands()
        {
            Console.WriteLine("#ID\tNAME");
            for (int i = 0; i < this.Modules.Count; i++)
            {
                Console.WriteLine("{0}\t{1} ({2})", i, this.Modules[i].Name, this.Modules[i].GetType().FullName);
            }
            foreach (var cmd in Commands)
            {
                Console.WriteLine("{0}\t{1}", cmd.Key, cmd.Value);
            }
        }

        protected static string StandardizeModuleName(IConsoleAppModule module)
        {
            return StandardizeModuleName(module.Name);
        }

        protected static string StandardizeModuleName(string moduleName)
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

    public enum ReturnCode : int
    {
        ModuleNotFound = int.MinValue,
        ArgumentMissing = int.MinValue + 1,

        Normal = 0,

        Error = int.MaxValue,
    }
}
