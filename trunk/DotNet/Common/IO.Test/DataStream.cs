using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MDo.Common.IO.Test
{
    public static class TestDataStream
    {
        private static readonly CompressionAlgorithm[] Algorithms = new CompressionAlgorithm[]
        {
            CompressionAlgorithm.Deflate,
            CompressionAlgorithm.DeflateN,
            CompressionAlgorithm.Zlib,
            CompressionAlgorithm.BZip2,
            CompressionAlgorithm.Lzma,
            CompressionAlgorithm.LzmaN,
        };

        public static void CompressionAlgorithmPerformanceComparison()
        {
            foreach (string testData in TestCommon.GetTestDataPaths())
            {
                IDictionary<CompressionAlgorithm, double> compressionRatio = new Dictionary<CompressionAlgorithm, double>();
                IDictionary<CompressionAlgorithm, TimeSpan> compressionTime = new Dictionary<CompressionAlgorithm, TimeSpan>();
                IDictionary<CompressionAlgorithm, TimeSpan> decompressionTime = new Dictionary<CompressionAlgorithm, TimeSpan>();

                using (MemoryStream clearStream = new MemoryStream())
                {
                    using (Stream inStream = FS.OpenRead(testData))
                    {
                        inStream.Transfer(clearStream);
                    }

                    Stopwatch timer;
                    foreach (CompressionAlgorithm algo in Algorithms)
                    {
                        clearStream.Position = 0;

                        using (Stream compressedStream = new MemoryStream())
                        {
                            timer = Stopwatch.StartNew();
                            using (Stream encodeStream = new DataEncodeStream(compressedStream, algo))
                            {
                                clearStream.Transfer(encodeStream);
                            }
                            timer.Stop();

                            compressionTime.Add(algo, timer.Elapsed);
                            compressionRatio.Add(algo, (double)compressedStream.Length / (double)clearStream.Length);

                            compressedStream.Position = 0;

                            using (MemoryStream decompressedStream = new MemoryStream())
                            {
                                timer = Stopwatch.StartNew();
                                using (Stream decodeStream = new DataDecodeStream(compressedStream, algo))
                                {
                                    decodeStream.Transfer(decompressedStream);
                                }
                                timer.Stop();

                                decompressionTime.Add(algo, timer.Elapsed);

                                Assert.IsTrue(decompressedStream.ToArray().SequenceEqual(clearStream.ToArray()));
                            }
                        }
                    }
                }

                Console.WriteLine(testData);
                Console.WriteLine("Algo	Ratio	EncT	DecT");
                Console.WriteLine("----	----	----	----");
                foreach (CompressionAlgorithm algo in Algorithms)
                {
                    Console.WriteLine("{0}	{1:G3}	{2:G3}	{3:G3}",
                        algo.ToString(),
                        compressionRatio[algo],
                        compressionTime[algo].TotalSeconds,
                        decompressionTime[algo].TotalSeconds);
                }
                Console.WriteLine("----	----	----	----");
                Console.WriteLine("Winner	{0}	{1}	{2}",
                    string.Join(",", compressionRatio.Where(item => item.Value == compressionRatio.Min(sItem => sItem.Value)).Select(item => item.Key.ToString())),
                    string.Join(",", compressionTime.Where(item => item.Value == compressionTime.Min(sItem => sItem.Value)).Select(item => item.Key.ToString())),
                    string.Join(",", decompressionTime.Where(item => item.Value == decompressionTime.Min(sItem => sItem.Value)).Select(item => item.Key.ToString())));
                Console.WriteLine("====================");
            }
        }
    }
}
