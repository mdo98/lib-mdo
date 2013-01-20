using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.FciUtil
{
    public class CrcChecker : ConsoleAppModule, IApplyFileOperation
    {
        #region Constants

        public const int NumRequiredArgs = 1;

        public const string CrcCheckStatus_Passed   = "PASSED";
        public const string CrcCheckStatus_Failed   = "FAILED";
        public const string CrcCheckStatus_Checked  = "CHECKD";

        public static readonly string[] CmdLineArg_Recursive = { "R", "-R", "/R" };
        public static readonly string[] CmdLineArg_KeepNames = { "K", "-K", "/K" };

        #endregion Constants


        #region ConsoleAppModule

        public override int Run(string[] args)
        {
            bool recursive = false;
            bool rename = true;

            int numOptionalArgs;
            for (numOptionalArgs = 0; numOptionalArgs < args.Length; numOptionalArgs++)
            {
                if (CmdLineArg_Recursive.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    recursive = true;
                }
                else if (CmdLineArg_KeepNames.Any(expectedArg => expectedArg.Equals(args[numOptionalArgs], StringComparison.OrdinalIgnoreCase)))
                {
                    rename = false;
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
                    IDictionary<string, string> failedRenames = new Dictionary<string, string>();
                    FS.ApplyFileOperation(this, path, recursive, new object[] { rename }, (string message) => Trace.TraceInformation(message));
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
                usage.AppendFormat("      {0}: Keep existing file names.", CmdLineArg_KeepNames[0]);    usage.AppendLine();
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
            object[] args = initialState as object[];
            bool rename = (bool)args[0];

            IDictionary<string, string> failedRenames = new SortedList<string, string>();
            IDictionary<string, int> crcCheckStatuses = new Dictionary<string, int>();
            crcCheckStatuses.Add(CrcCheckStatus_Passed, 0);
            crcCheckStatuses.Add(CrcCheckStatus_Failed, 0);
            crcCheckStatuses.Add(CrcCheckStatus_Checked, 0);
            return new object[] { rename, failedRenames, crcCheckStatuses };
        }

        public object ExecuteFileOperation(string filePath, object state, Action<string> log)
        {
            object[] args = state as object[];
            bool rename = (bool)args[0];
            IDictionary<string, string> failedRenames = (IDictionary<string, string>)args[1];
            IDictionary<string, int> crcCheckStatuses = (IDictionary<string, int>)args[2];

            uint? expectedCrcValue;
            uint crcValue;
            string crcCheckedFilePath;
            bool? renamed;
            bool crcPassed = CrcCheck.CheckCrc(filePath, rename, out expectedCrcValue, out crcValue, out crcCheckedFilePath, out renamed);
            string crcCheckStatus = expectedCrcValue.HasValue ? (crcPassed ? CrcCheckStatus_Passed : CrcCheckStatus_Failed) : CrcCheckStatus_Checked;
            crcCheckStatuses[crcCheckStatus]++;

            log(string.Format(
                "{0}{1}: {2} <= {3}{0}{4} => {5}",
                Environment.NewLine,
                crcCheckStatus,
                crcValue.ToString("X8"),
                filePath,
                "                ",
                (renamed.HasValue && renamed.Value == true) ? crcCheckedFilePath : "[not renamed]"));

            if (renamed.HasValue && renamed.Value == false)
            {
                failedRenames.Add(filePath, crcCheckedFilePath);
            }

            return new object[] { rename, failedRenames, crcCheckStatuses };
        }

        public void FinalizeFileOperation(object initialState, object finalExecutionState, Action<string> log)
        {
            object[] args = finalExecutionState as object[];
            IDictionary<string, string> failedRenames = (IDictionary<string, string>)args[1];
            IDictionary<string, int> crcCheckStatuses = (IDictionary<string, int>)args[2];

            if (failedRenames.Count > 0)
            {
                log("The following files should have been renamed, but the operation failed, possibly due to write protection or name conflicts.");
                foreach (KeyValuePair<string, string> failedRename in failedRenames)
                {
                    log(string.Format("{0} => {1}", failedRename.Key, failedRename.Value));
                }
                log(string.Empty);
            }

            log("SUMMARY");
            int numFiles = 0;
            foreach (KeyValuePair<string, int> status in crcCheckStatuses)
            {
                log(string.Format("{0} = {1}", status.Key, status.Value));
                numFiles += status.Value;
            }
            log(string.Format("{0} files processed.", numFiles));
        }

        #endregion IApplyFileOperation
    }
}
