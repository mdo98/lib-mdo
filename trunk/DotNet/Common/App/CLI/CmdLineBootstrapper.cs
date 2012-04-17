using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MDo.Common.App.CLI
{
    public class CmdLineBootstrapper : ConsoleAppMaster
    {
        public CmdLineBootstrapper() : base()
        {
            this.LoadFrom(Assembly.GetCallingAssembly());
        }

        protected const string Command_Enumerate = "ENUM";
        protected const string Command_Help = "HELP";
        protected const string Command_ToggleTiming = "TIME";
        protected const string Command_Exit = "X";
        public static readonly IDictionary<string, string> Commands = new SortedList<string, string>()
        {
            { StandardizedModuleName(Command_Enumerate),    "Enumerate all supported modules and commands" },
            { StandardizedModuleName(Command_Help),         "Print the argument list of a module (qualify with Module #ID)" },
            { StandardizedModuleName(Command_ToggleTiming), "Toggle module timing" },
            { StandardizedModuleName(Command_Exit),         "Exit" },
        };
        protected bool TimeModule = true;

        public override void Run(string[] args)
        {
            base.Run(args);

            // Finished running the prescribed module; now listing supported modules & commands
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
                switch (StandardizedModuleName(command))
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

        public void LoadFrom(Assembly assembly)
        {
            Type moduleBaseType = typeof(IConsoleAppModule);
            foreach (Type type in assembly.GetTypes().Where(item => moduleBaseType.IsAssignableFrom(item)).OrderBy(item => item.Name))
            {
                this.RegisterModule(Activator.CreateInstance(type, true) as IConsoleAppModule, true);
            }
        }
    }
}
