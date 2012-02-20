using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MDo.Common.IO;

namespace MDo.Common.Numerics.Random.Test
{
    public static class RngTestMain
    {
        private static readonly Type[] RNGTypes =
        {
            typeof(SystemRandom),
            typeof(LaggedFibonacciRng),
            typeof(NRCombinedRng1),
            typeof(NRCombinedRng2),
            typeof(MT19937Rng),
        };

        public static void GenerateSamplesForDieHard()
        {
            const int NumSamplesToPrint = 10000000;
            const string RngSamplesOutputExtension = ".out";

            int numUnknownRNGs = 0;
            foreach (Type type in RNGTypes)
            {
                IRandom rng = (IRandom)Activator.CreateInstance(type);
                RandomNumberGenerator rngWithSampling = rng as RandomNumberGenerator;

                string rngName = string.IsNullOrWhiteSpace(rng.Name) ? ("UnknownRNG_" + numUnknownRNGs++) : rng.Name;

                string pathSafeRngTestName = rngName;
                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                {
                    pathSafeRngTestName = pathSafeRngTestName.Replace(invalidChar, '-');
                }
                string txtOutPath = pathSafeRngTestName + RngSamplesOutputExtension;

                Console.WriteLine("{0}: Writing {1} samples to {2} for DIEHARD...", rngName, NumSamplesToPrint, txtOutPath);
                using (Stream txtStream = FS.OpenWrite(txtOutPath))
                {
                    using (TextWriter txtWriter = new StreamWriter(txtStream))
                    {
                        int samplesLength = 0;
                        for (int i = 0; i < NumSamplesToPrint; i++)
                        {
                            byte[] b;
                            if (null != rngWithSampling)
                            {
                                var s = rngWithSampling.Sample();
                                b = BitConverter.GetBytes(s);
                                txtWriter.Write(s.ToString(string.Format("X{0}", 2 * b.Length)));
                            }
                            else
                            {
                                int s = rng.Int();
                                b = BitConverter.GetBytes(s);
                                txtWriter.Write(s.ToString(string.Format("X{0}", 2 * b.Length)));
                            }
                            samplesLength += 2 * b.Length;
                            if (samplesLength % 80 == 0)
                                txtWriter.WriteLine();
                        }
                    }
                }
                Console.WriteLine();
            }
        }

        private static readonly bool TestSpeed = true;
        private static readonly bool TestEquidistribution = true;

        public static void TestRngs()
        {
            const int NumSamplesForTesting_Small = 10000;
            const int NumSamplesForTesting_Large = 100000000;

            Action<string> writeToStdOut = (string output) => Console.WriteLine("\t\t{0}", output);

            foreach (Type type in RNGTypes)
            {
                int numUnknownRNGs = 0;
                foreach (RngTest rngTest in new RngTest[]
                    {
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 0 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 1 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { RandomInt() })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type)),
                    })
                {
                    string rngName = string.IsNullOrWhiteSpace(rngTest.Name) ? ("UnknownRNG_" + numUnknownRNGs++) : rngTest.Name;
                    Console.WriteLine(rngName);

                    if (TestSpeed)
                    {
                        Console.WriteLine("\tMeasuring speed: Generating {0} samples...", NumSamplesForTesting_Large);
                        rngTest.Time(NumSamplesForTesting_Large, writeToStdOut);
                        Console.WriteLine();
                    }

                    if (TestEquidistribution)
                    {
                        Console.WriteLine("\tTesting equidistribution property with {0} samples...", NumSamplesForTesting_Small);
                        rngTest.Equidistribution(NumSamplesForTesting_Small, writeToStdOut);
                        Console.WriteLine();
                    }
                }
            }
        }

        private static readonly RNGCryptoServiceProvider CryptoRng = new RNGCryptoServiceProvider();
        private static int RandomInt()
        {
            byte[] b = new byte[4];
            CryptoRng.GetBytes(b);
            return BitConverter.ToInt32(b, 0);
        }
    }
}
