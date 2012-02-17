using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Zlib.DotZLib;

namespace MDo.Common.IO.Test
{
    public static class TestDeflateStream
    {
        private const string DecompressedOutputExtension = ".out";

        public static void EncodeDecodeInvariant()
        {
            const string DeflateOutputExtension = ".zlib";

            foreach (string testData in TestCommon.GetTestDataPaths())
            {
                string compressedOutputFile = testData + DeflateOutputExtension;

                using (Stream inStream = FS.OpenRead(testData),
                              outStream = FS.OpenWrite(compressedOutputFile))
                {
                    using (Stream encodeStream = new DeflateEncodeStream(outStream))
                    {
                        inStream.Transfer(encodeStream);
                    }
                }

                using (Stream inStream = FS.OpenRead(compressedOutputFile),
                              outStream = FS.OpenWrite(compressedOutputFile + DecompressedOutputExtension))
                {
                    using (Stream decodeStream = new DeflateDecodeStream(inStream))
                    {
                        decodeStream.Transfer(outStream);
                    }
                }

                Assert.AreEqual(CrcCalc.CalculateFromFile(testData), CrcCalc.CalculateFromFile(compressedOutputFile + DecompressedOutputExtension));
            }
        }
    }
}
