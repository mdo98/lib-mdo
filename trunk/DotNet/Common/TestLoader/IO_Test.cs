using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MDo.Common.IO.Test;

namespace MDo.Common.TestLoader
{
    [TestClass]
    public class IO_Test
    {
        [TestCategory("IO_Test"), TestMethod]
        public void DeflateStream_EncodeDecodeInvariant()
        {
            IO_DeflateStream_EncodeDecodeInvariant.Run();
        }
        /*
        [TestCategory("IO_Test"), TestMethod]
        public void LzmaStream_EncodeDecodeInvariant()
        {
            TestLzmaStream.EncodeDecodeInvariant();
        }

        [TestCategory("IO_Test"), TestMethod]
        public void LzmaStream_BufferedEncodeDecodeInvariant()
        {
            TestLzmaStream.BufferedEncodeDecodeInvariant();
        }
        */
        [TestCategory("IO_Test"), TestMethod]
        public void DataStream_CompressionAlgorithmPerformanceComparison()
        {
            IO_DataStream_CompressionAlgorithmPerformance.Run();
        }
    }
}
