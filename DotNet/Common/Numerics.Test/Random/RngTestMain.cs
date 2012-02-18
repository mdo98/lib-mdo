using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

        private const int NumSamplesToPrint = 1000;
        private const string RngSamplesOutputExtension = ".out";

        public static void TestRngs()
        {
            int numUnknownRNGs = 0;
            foreach (Type type in RNGTypes)
            {
                foreach (RngTest rngTest in new RngTest[]
                    {
                        new RngTest(() => (IRandom)Activator.CreateInstance(type)),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 0 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 1 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 2 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 3 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { RandomNonNegativeInt() })),
                    })
                {
                    string rngTestName = string.IsNullOrWhiteSpace(rngTest.Name) ? ("UnknownRNG_" + numUnknownRNGs++) : rngTest.Name;

                    Console.WriteLine(rngTestName);

                    string pathSafeRngTestName = rngTestName;
                    foreach (char invalidChar in Path.GetInvalidFileNameChars())
                    {
                        pathSafeRngTestName = pathSafeRngTestName.Replace(invalidChar, '-');
                    }

                    Console.WriteLine("\tPrinting out the first {0} samples...", NumSamplesToPrint);
                    using (Stream outStream = FS.OpenWrite(pathSafeRngTestName + RngSamplesOutputExtension))
                    {
                        rngTest.PrintSamples(NumSamplesToPrint, outStream);
                    }
                }
            }
        }

        private static readonly RNGCryptoServiceProvider CryptoRng = new RNGCryptoServiceProvider();
        private static int RandomNonNegativeInt()
        {
            byte[] b = new byte[4];
            CryptoRng.GetBytes(b);
            int i = BitConverter.ToInt32(b, 0);
            return (int.MinValue == i ? 0 : Math.Abs(i));
        }
    }
}
