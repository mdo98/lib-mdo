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
            StringBuilder usage = new StringBuilder();
            string entryBinary = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            usage.AppendLine  ("=========================");
            usage.AppendFormat("{0}: [OptionalArgs] [ModuleName]#[ModuleIndx] [ModuleArgs]", entryBinary);  usage.AppendLine();
            usage.AppendLine  ("-------------------------");
            usage.AppendLine  ("  [OptionalArgs] :=");
            usage.AppendFormat("      {0}: Prints help.", CmdLineArg_Help[0]);
            usage.AppendLine  ("  [ModuleName] := A registered module.");
            usage.AppendLine  ("  [ModuleIndx] := Optionally, specify a 0-based index, if multiple modules have the same name.");
            foreach (string moduleName in this.ModuleNames)
            {
                usage.AppendFormat("      {0}", moduleName);
                usage.AppendLine();
            }
            usage.AppendFormat("  [ModuleArgs] := Module arguments. For more info: {0} {1} [ModuleName]", entryBinary, CmdLineArg_Help[0]); usage.AppendLine();
            usage.AppendLine  ("=========================");
            Console.WriteLine(usage.ToString());
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
            Trace.TraceInformation("Module {0}", module.Name);
            DateTime start = DateTime.Now;
            if (time)
            {
                Trace.TraceInformation("Start  : {0}", start);
            }
            try
            {
                returnCode = module.Run(args);
                Trace.TraceInformation("Return : {0}", returnCode);
            }
            catch (ArgumentMissingException ex)
            {
                Trace.TraceError("Error  : Module {0}: {1}", module.Name, ex.Message);
                module.PrintUsage();
                returnCode = (int)ReturnCode.ArgumentMissing;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error  : Module {0}: {1}", module.Name, ex.ToString());
                returnCode = (int)ReturnCode.Error;
            }
            if (time)
            {
                DateTime end = DateTime.Now;
                Trace.TraceInformation("End    : {0}", end);
                Trace.TraceInformation("Elapsed: {0}", end - start);
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
            Type baseType = typeof(IConsoleAppModule);
            Type thisType = this.GetType();
            foreach (Type type in assembly.GetTypes().Where(item => !item.IsAbstract && baseType.IsAssignableFrom(item) && !thisType.IsAssignableFrom(item)).OrderBy(item => item.Name))
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
