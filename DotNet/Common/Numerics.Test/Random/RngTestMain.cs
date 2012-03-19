using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
#if !X86
            typeof(NRCombinedRng1),
            typeof(NRCombinedRng2),
#endif
            typeof(MT19937Rng),
            typeof(SuperKissRng1),
            typeof(SuperKissRng2),
        };

#if DEBUG
        public static void ImplementationCheck()
        {
            {
                SuperKissRng1 rng = new SuperKissRng1();
                Console.WriteLine(rng.GetType().FullName);
                uint[] seeds = new uint[SuperKissRng1.QMAX];
                for (int i = 0; i < SuperKissRng1.QMAX; i++)
                {
                    rng.XCNG = SuperKissRng1.CNG(rng.XCNG);
                    rng.XS = SuperKissRng1.XOR_Shift(rng.XS);
                    seeds[i] = rng.XCNG + rng.XS;
                }
                rng.Init(seeds);
                for (int i = 0; i < 999999999; i++)
                {
                    rng.GetNextSample();
                }
                uint s = rng.GetNextSample();
                Console.WriteLine("Expect: 0x{0:X8}", -872412446);
                Console.WriteLine("Actual: 0x{0:X8}", s);
                Console.WriteLine();
            }

            {
                SuperKissRng2 rng = new SuperKissRng2();
                Console.WriteLine(rng.GetType().FullName);
#if X86
                uint[] seeds = new uint[SuperKissRng2.QMAX];
#else
                ulong[] seeds = new ulong[SuperKissRng2.QMAX];
#endif
                for (int i = 0; i < SuperKissRng2.QMAX; i++)
                {
                    rng.XCNG = SuperKissRng2.CNG(rng.XCNG);
                    rng.XS = SuperKissRng2.XOR_Shift(rng.XS);
                    seeds[i] = rng.XCNG + rng.XS;
                }
                rng.Init(seeds);
                for (int i = 0; i < 999999999; i++)
                {
                    rng.Sample();
                }
#if X86
                uint s = rng.Sample();
                Console.WriteLine("Expect: 1809478889");
#else
                ulong s = rng.Sample();
                Console.WriteLine("Expect: 4013566000157423768");
#endif
                Console.WriteLine("Actual: {0}", s);
                Console.WriteLine();
            }
        }
#endif

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
                                var s = rng.UInt32();
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

        public static void TimeRngs()
        {
            const int numSamples = 100000000;   // 100M
            TestRngsHelper(
                Tuple.Create("Speed", numSamples, (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Time(numSamples, writeToStdOut))));
        }

        private static void TestRngsHelper(params Tuple<string, int, Action<RngTest, Action<string>>>[] testMethods)
        {
            if (null == testMethods)
                return;

            Action<string> writeToStdOut = (string output) => Console.WriteLine("\t\t{0}", output);
            foreach (Type type in RNGTypes)
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

        public static void TestRngs(RngTestFlag testFlag
            = RngTestFlag.Equidistribution | RngTestFlag.Diehard_Birthday | RngTestFlag.Diehard_3DSphere)
        {
            var tests = new List<Tuple<string, int, Action<RngTest, Action<string>>>>();

            {
                const int numSamples = 10000;
                if (testFlag.HasFlag(RngTestFlag.Equidistribution))
                    tests.Add(Tuple.Create("Equidistribution", numSamples, (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Equidistribution(numSamples, writeToStdOut))));
            }

            {
                const int numExperiments = 1 << 9;
                if (testFlag.HasFlag(RngTestFlag.Diehard_Birthday))
                    tests.Add(Tuple.Create("Diehard_Birthday", numExperiments, (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_Birthday(numExperiments, writeToStdOut))));
            }

            {
                const int numExperiments = 1 << 5;
                if (testFlag.HasFlag(RngTestFlag.Diehard_3DSphere))
                    tests.Add(Tuple.Create("Diehard_3DSphere", numExperiments, (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_3DSphere(numExperiments, writeToStdOut))));
            }

            TestRngsHelper(tests.ToArray());
        }
    }

    [Flags]
    public enum RngTestFlag
    {
        Equidistribution    = 0x1,
        Diehard_Birthday    = 0x2,
        Diehard_3DSphere    = 0x4,
    }
}
