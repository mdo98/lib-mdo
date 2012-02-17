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
        [TestMethod]
        public void DeflateStream_EncodeDecodeInvariant()
        {
            TestDeflateStream.EncodeDecodeInvariant();
        }

        [TestMethod]
        public void LzmaStream_EncodeDecodeInvariant()
        {
            TestLzmaStream.EncodeDecodeInvariant();
        }

        [TestMethod]
        public void LzmaStream_BufferedEncodeDecodeInvariant()
        {
            TestLzmaStream.BufferedEncodeDecodeInvariant();
        }

        [TestMethod]
        public void DataStream_CompressionAlgorithmPerformanceComparison()
        {
            TestDataStream.CompressionAlgorithmPerformanceComparison();
        }
    }
}
