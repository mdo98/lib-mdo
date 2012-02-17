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
        void Run(string[] args, TextReader stdIn, TextWriter stdOut, TextWriter stdErr);
        void PrintUsage(TextWriter stream);
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


        #region Static Variables

        private TextReader InStream  = Console.In;
        private TextWriter OutStream = Console.Out;
        private TextWriter ErrStream = Console.Error;
        private readonly IDictionary<string, IConsoleAppModule> Modules = new SortedList<string, IConsoleAppModule>();

        #endregion Static Variables


        #region IConsoleAppModule

        public string Name
        {
            get { return this.GetType().Name; }
        }

        public void Run(string[] args, TextReader stdIn, TextWriter stdOut, TextWriter stdErr)
        {
            bool help = false;

            int numOptionalArgs;
            for (numOptionalArgs = 0; numOptionalArgs < args.Length; numOptionalArgs++)
            {
                if (CmdLineArg_Help.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    help = true;
                }
                else if (CmdLineArg_InFile.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    string inFile = args[++numOptionalArgs];
                    try
                    {
                        this.InStream = new StreamReader(File.OpenRead(inFile));
                    }
                    catch (Exception ex)
                    {
                        this.ErrStream.WriteLine("Unable to open '{0}' for reading; will use stdin. The error was: {1}", inFile, ex.ToString());
                    }
                }
                else if (CmdLineArg_OutFile.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    string outFile = args[++numOptionalArgs];
                    try
                    {
                        this.OutStream = new StreamWriter(new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.Read));
                    }
                    catch (Exception ex)
                    {
                        this.ErrStream.WriteLine("Unable to open '{0}' for writing; will use stdout. The error was: {1}", outFile, ex.ToString());
                    }
                }
                else if (CmdLineArg_ErrFile.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    string errFile = args[++numOptionalArgs];
                    try
                    {
                        this.ErrStream = new StreamWriter(new FileStream(errFile, FileMode.Create, FileAccess.Write, FileShare.Read));
                    }
                    catch (Exception ex)
                    {
                        this.ErrStream.WriteLine("Unable to open '{0}' for writing; will use stderr. The error was: {1}", errFile, ex.ToString());
                    }
                }
                else
                {
                    break;
                }
            }
            args = args.Skip(numOptionalArgs).ToArray();

            string moduleName = null;
            IConsoleAppModule module = null;
            if (args.Length < NumRequiredArgs)
            {
                if (!help)
                {
                    this.ErrStream.WriteLine("A module name must be specified to invoke.");
                    this.PrintUsage(this.ErrStream);
                    return;
                }
            }
            else
            {
                moduleName = args[0].Trim().ToUpperInvariant();
                if (!this.Modules.ContainsKey(moduleName))
                {
                    this.ErrStream.WriteLine("Module {0} not found.", moduleName);
                    this.PrintUsage(this.ErrStream);
                    return;
                }
                module = this.Modules[moduleName];
            }
            args = args.Skip(NumRequiredArgs).ToArray();

            if (help)
            {
                if (module == null)
                {
                    this.PrintUsage(this.OutStream);
                }
                else
                {
                    module.PrintUsage(this.OutStream);
                }
                return;
            }

            try
            {
                module.Run(args, this.InStream, this.OutStream, this.ErrStream);
            }
            catch (ArgumentMissingException ex)
            {
                this.ErrStream.WriteLine("Module {0}: Missing argument '{1}'.", moduleName, ex.ParameterName);
                module.PrintUsage(this.ErrStream);
            }
            catch (Exception ex)
            {
                this.ErrStream.WriteLine("An error occurred: {0}", ex.ToString());
            }
            finally
            {
                this.OutStream.Flush();
                this.ErrStream.Flush();
            }
        }

        public void PrintUsage(TextWriter stream)
        {
            string entryBinary = Path.GetFileName(Assembly.GetEntryAssembly().Location);
            StringBuilder text = new StringBuilder();
            text.AppendLine("=========================");
            text.AppendLine(entryBinary + " [OptionalArgs] [ModuleName] [ModuleArgs]");
            text.AppendLine("-------------------------");
            text.AppendLine("  [OptionalArgs] :=");
            text.AppendLine("      " + CmdLineArg_Help[0] + ": Prints help.");
            text.AppendLine("      " + CmdLineArg_InFile[0]  + " [InFile]: Uses [InFile] as stdin.");
            text.AppendLine("      " + CmdLineArg_OutFile[0] + " [OutFile]: Uses [OutFile] as stdout.");
            text.AppendLine("      " + CmdLineArg_ErrFile[0] + " [ErrFile]: Uses [ErrFile] as stderr.");
            text.AppendLine("  [ModuleName] := A registered module.");
            foreach (string moduleName in this.Modules.Keys)
            {
                text.AppendLine("      " + moduleName);
            }
            text.AppendLine("  [ModuleArgs] := Module arguments. For more info: " + entryBinary + " " + CmdLineArg_Help[0] + " [ModuleName]");
            text.AppendLine("=========================");
            stream.WriteLine(text.ToString());
        }

        #endregion IConsoleAppModule


        public void RegisterModule(IConsoleAppModule module)
        {
            if (null == module)
                throw new ArgumentNullException("module");

            string moduleName = module.Name.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new ArgumentNullException("module.Name");
            
            if (this.Modules.ContainsKey(moduleName))
                throw new InvalidOperationException(string.Format("A module is already registered as {0}.", moduleName));
            else
                this.Modules.Add(moduleName, module);
        }
    }
}
