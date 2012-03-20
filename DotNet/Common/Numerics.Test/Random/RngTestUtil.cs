using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MDo.Common.Numerics.Random.Test
{
    public static class RngTestUtil
    {
        internal static readonly Type[] RNGTypes =
        {
            typeof(SystemRandom),
            typeof(LaggedFibonacciRng),
#if !X86
            typeof(NRCombinedRng1),
            typeof(NRCombinedRng2),
#endif
            typeof(MT19937Rng),
            typeof(SuperKissRng1),
            typeof(SuperKissRng2),
        };

        internal static void TestRNGsHelper(params Tuple<string, int, Action<RngTest, Action<string>>>[] testMethods)
        {
            if (null == testMethods)
                return;

            Action<string> writeToStdOut = (string output) => Console.WriteLine("\t\t{0}", output);
            foreach (Type type in RngTestUtil.RNGTypes)
            {
                int numUnknownRNGs = 0;
                foreach (RngTest rngTest in new RngTest[]
                    {
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 0 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { 1 })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type, new object[] { RngTestUtil.RandomInt() })),
                        new RngTest(() => (IRandom)Activator.CreateInstance(type)),
                    })
                {
                    string rngName = string.IsNullOrWhiteSpace(rngTest.Name) ? ("UnknownRNG_" + numUnknownRNGs++) : rngTest.Name;
                    Console.WriteLine(rngName);

                    foreach (var testMethod in testMethods)
                    {
                        Console.WriteLine("\tTesting {0}, samplespace = {1:N0}...", testMethod.Item1, testMethod.Item2);
                        testMethod.Item3(rngTest, writeToStdOut);
                        Console.WriteLine();
                    }
                }
            }
        }

        private static readonly RNGCryptoServiceProvider CryptoRng = new RNGCryptoServiceProvider();

        public static int RandomInt()
        {
            byte[] b = new byte[4];
            CryptoRng.GetBytes(b);
            return BitConverter.ToInt32(b, 0);
        }
    }
}
