using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.FciUtil
{
    public class CrcCalculator : ConsoleAppModule, IApplyFileOperation
    {
        #region Constants

        public const int NumRequiredArgs = 1;

        public static readonly string[] CmdLineArg_Recursive = { "R", "-R", "/R" };

        #endregion Constants


        #region ConsoleAppModule

        public override int Run(string[] args)
        {
            bool recursive = false;

            int numOptionalArgs;
            for (numOptionalArgs = 0; numOptionalArgs < args.Length; numOptionalArgs++)
            {
                if (CmdLineArg_Recursive.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    recursive = true;
                }
                else
                {
                    break;
                }
            }
            args = args.Skip(numOptionalArgs).ToArray();

            if (args.Length < NumRequiredArgs)
            {
                throw new ArgumentMissingException("paths");
            }

            int numErrors = 0;
            foreach (string path in args)
            {
                try
                {
                    FS.ApplyFileOperation(this, path, recursive, null, (string message) => Trace.TraceInformation(message));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error while processing {0}: {1}", path, ex.ToString());
                    numErrors++;
                }
            }
            return numErrors;
        }

        public override string Usage
        {
            get
            {
                StringBuilder usage = new StringBuilder();
                string moduleName = this.Name;
                usage.AppendLine  ("=========================");
                usage.AppendFormat("{0}: [OptionalArgs] [RequiredArgs]", moduleName);   usage.AppendLine();
                usage.AppendLine  ("-------------------------");
                usage.AppendLine  ("  [OptionalArgs] :=");
                usage.AppendFormat("      {0}: Recursive.", CmdLineArg_Recursive[0]);   usage.AppendLine();
                usage.AppendLine  ("-------------------------");
                usage.AppendLine  ("  [RequiredArgs]");
                usage.AppendLine  ("      [paths]: List of files, folders or patterns to calculate CRCs.");
                usage.AppendLine  ("=========================");
                return usage.ToString();
            }
        }

        #endregion ConsoleAppModule


        #region IApplyFileOperation

        public object InitFileOperation(object initialState, Action<string> log)
        {
            return null;
        }

        public object ExecuteFileOperation(string filePath, object state, Action<string> log)
        {
            log(string.Format(
                "{0} <= {1}",
                CrcCalc.CalculateFromFile(filePath).ToString("X8"),
                filePath));

            return null;
        }

        public void FinalizeFileOperation(object initialState, object finalExecutionState, Action<string> log)
        {
        }

        #endregion IApplyFileOperation
    }
}
