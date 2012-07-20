using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Zlib.DotZLib;

namespace MDo.Common.IO.Test
{
    public class IO_DeflateStream_EncodeDecodeInvariant : ConsoleAppModule
    {
        private const string DecompressedOutputExtension = ".out";

        public static void Run()
        {
            const string DeflateOutputExtension = ".zlib";

            foreach (string testData in TestCommon.GetTestDataPaths())
            {
                string compressedOutputFile = testData + DeflateOutputExtension;

                using (Stream inStream = File.OpenRead(testData),
                              outStream = File.OpenWrite(compressedOutputFile))
                {
                    using (Stream encodeStream = new DeflateEncodeStream(outStream))
                    {
                        inStream.Transfer(encodeStream);
                    }
                }

                using (Stream inStream = File.OpenRead(compressedOutputFile),
                              outStream = File.OpenWrite(compressedOutputFile + DecompressedOutputExtension))
                {
                    using (Stream decodeStream = new DeflateDecodeStream(inStream))
                    {
                        decodeStream.Transfer(outStream);
                    }
                }

                Assert.AreEqual(CrcCalc.CalculateFromFile(testData), CrcCalc.CalculateFromFile(compressedOutputFile + DecompressedOutputExtension));
            }
        }
        
        #region ConsoleAppModule

        public override int Run(string[] args)
        {
            Run();
            return (int)ReturnCode.Normal;
        }

        #endregion ConsoleAppModule
    }
}
