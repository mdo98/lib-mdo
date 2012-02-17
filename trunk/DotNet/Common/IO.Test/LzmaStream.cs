using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using SevenZip.Compression.LZMA;

namespace MDo.Common.IO.Test
{
    public static class TestLzmaStream
    {
        private const string DecompressedOutputExtension = ".out";

        public static void EncodeDecodeInvariant()
        {
            const string LzmaOutputExtension = ".lzma";

            foreach (Tuple<LzmaEncoder, LzmaDecoder> coderPair in new Tuple<LzmaEncoder, LzmaDecoder>[]
                {
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new ManagedLzmaEncoder(), new ManagedLzmaDecoder()),
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new NativeLzmaEncoder(), new NativeLzmaDecoder()),
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new NativeLzmaEncoderWithBcjFilter(), new NativeLzmaDecoderWithBcjFilter()),
                })
            {
                foreach (string testData in TestCommon.GetTestDataPaths())
                {
                    string compressedOutputFile = testData + LzmaOutputExtension + "." + coderPair.Item1.GetType().Name;

                    using (Stream inStream = FS.OpenRead(testData),
                                  outStream = FS.OpenWrite(compressedOutputFile))
                    {
                        LzmaStream.Compress(inStream, outStream, coderPair.Item1);
                    }

                    using (Stream inStream = FS.OpenRead(compressedOutputFile),
                                  outStream = FS.OpenWrite(compressedOutputFile + DecompressedOutputExtension))
                    {
                        LzmaStream.Decompress(inStream, outStream, coderPair.Item2);
                    }

                    Assert.AreEqual(CrcCalc.CalculateFromFile(testData), CrcCalc.CalculateFromFile(compressedOutputFile + DecompressedOutputExtension));
                }
            }
        }

        public static void BufferedEncodeDecodeInvariant()
        {
            const string BufferedLzmaOutputExtension = ".lzma.buf";

            foreach (Tuple<LzmaEncoder, LzmaDecoder> coderPair in new Tuple<LzmaEncoder, LzmaDecoder>[]
                {
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new ManagedLzmaEncoder(), new ManagedLzmaDecoder()),
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new NativeLzmaEncoder(), new NativeLzmaDecoder()),
                    Tuple.Create<LzmaEncoder, LzmaDecoder>(new NativeLzmaEncoderWithBcjFilter(), new NativeLzmaDecoderWithBcjFilter()),
                })
            {
                foreach (string testData in TestCommon.GetTestDataPaths())
                {
                    string compressedOutputFile = testData + BufferedLzmaOutputExtension + "." + coderPair.Item1.GetType().Name;

                    using (Stream inStream = FS.OpenRead(testData),
                                  outStream = FS.OpenWrite(compressedOutputFile))
                    {
                        using (LzmaStream encodeStream = new LzmaEncodeStream(outStream, coderPair.Item1))
                        {
                            inStream.Transfer(encodeStream);
                        }
                    }

                    using (Stream inStream = FS.OpenRead(compressedOutputFile),
                                  outStream = FS.OpenWrite(compressedOutputFile + DecompressedOutputExtension))
                    {
                        using (LzmaStream decodeStream = new LzmaDecodeStream(inStream, coderPair.Item2))
                        {
                            decodeStream.Transfer(outStream);
                        }
                    }

                    Assert.AreEqual(CrcCalc.CalculateFromFile(testData), CrcCalc.CalculateFromFile(compressedOutputFile + DecompressedOutputExtension));
                }
            }
        }
    }
}
