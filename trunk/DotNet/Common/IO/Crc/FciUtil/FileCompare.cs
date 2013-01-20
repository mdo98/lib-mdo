using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.FciUtil
{
    public class FileCompare : ConsoleAppModule
    {
        private const int BufferSize = 16777216;
        private const string EOF = "EOF";

        #region ConsoleAppModule

        public override int Run(string[] args)
        {
            if (args.Length < 2)
                throw new ArgumentMissingException("files");

            long numDifferentBytes = 0L;

            // Naive comparison
            byte[] l = new byte[BufferSize], r = new byte[BufferSize];
            string leftFilePath = args[0], rightFilePath = args[1];
            using (Stream leftFile = File.OpenRead(leftFilePath),
                          rightFile = File.OpenRead(rightFilePath))
            {
                long leftFileSize = leftFile.Length,
                     rightFileSize = rightFile.Length;
                long minFileSize = Math.Min(leftFileSize, rightFileSize),
                     numProcessedBytes = 0L;
                bool headerWritten = false;

                while (numProcessedBytes < minFileSize)
                {
                    int nextBatchSize = Math.Min((int)(minFileSize - numProcessedBytes), BufferSize);
                    int numBytesRead;

                    if ((numBytesRead = leftFile.Read(l, 0, nextBatchSize)) < nextBatchSize)
                        throw new IOException(string.Format("Unexpected {0} in left file at position {1}.", EOF, numProcessedBytes + numBytesRead));

                    if ((numBytesRead = rightFile.Read(r, 0, nextBatchSize)) < nextBatchSize)
                        throw new IOException(string.Format("Unexpected {0} in right file at position {1}.", EOF, numProcessedBytes + numBytesRead));

                    for (int i = 0; i < nextBatchSize; i++)
                    {
                        if (l[i] != r[i])
                        {
                            if (!headerWritten)
                            {
                                Trace.TraceInformation("{0,10}\t{1,12}\t{2,12}", "POS", "LEFT", "RIGHT");
                                headerWritten = true;
                            }
                            Trace.TraceInformation("{0,10:D}\t{1,12:X2}\t{2,12:X2}", numProcessedBytes + i, l[i], r[i]);
                            numDifferentBytes++;
                        }
                    }
                    numProcessedBytes += (long)nextBatchSize;
                }

                if (leftFileSize > minFileSize)
                    Trace.TraceInformation("{0,10:D}\t({1,10:D})\t({2,10})", minFileSize, leftFileSize - minFileSize, EOF);
                else if (rightFileSize > minFileSize)
                    Trace.TraceInformation("{0,10:D}\t({1,10})\t({2,10:D})", minFileSize, EOF, leftFileSize - minFileSize);

                Trace.TraceInformation("# different bytes: {0} of {1} ({2:F2} %)", numDifferentBytes, minFileSize, 100.0 * numDifferentBytes / minFileSize);
            }

            return (int)Math.Min(numDifferentBytes, (long)int.MaxValue);
        }

        public override string Usage
        {
            get
            {
                StringBuilder usage = new StringBuilder();
                string moduleName = this.Name;
                usage.AppendLine  ("=========================");
                usage.AppendFormat("{0}: [LeftFile] [RightFile]", moduleName);  usage.AppendLine();
                usage.AppendLine  ("=========================");
                return usage.ToString();
            }
        }

        #endregion ConsoleAppModule
    }
}
