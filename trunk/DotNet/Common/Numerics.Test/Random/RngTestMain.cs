using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MDo.Common.App.CLI;

namespace MDo.Common.Numerics.Random.Test
{
#if DEBUG
    public class Numerics_RNG_SuperKissRngImplCheck : ConsoleAppModule
    {
        public static void Run()
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
                const int numSamples = 999999999;
                Console.WriteLine("Generating {0} samples...", numSamples);
                for (int i = 0; i < numSamples; i++)
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
                const int numSamples = 999999999;
                Console.WriteLine("Generating {0} samples...", numSamples);
                for (int i = 0; i < numSamples; i++)
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

        #region ConsoleAppModule

        public override void Run(string[] args)
        {
            Run();
        }

        #endregion ConsoleAppModule
    }
#endif


    public class Numerics_RNG_GenerateSamplesForDiehard : ConsoleAppModule
    {
        public static void Run(int numSamples = 10000000)
        {
            const string RngSamplesOutputExtension = ".out";

            int numUnknownRNGs = 0;
            foreach (Type type in RngTestUtil.RNGTypes)
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

                Console.WriteLine("{0}: Writing {1:N0} samples to {2} for DIEHARD...", rngName, numSamples, txtOutPath);
                using (Stream txtStream = File.OpenWrite(txtOutPath))
                {
                    using (TextWriter txtWriter = new StreamWriter(txtStream))
                    {
                        int samplesLength = 0;
                        for (int i = 0; i < numSamples; i++)
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

        #region ConsoleAppModule

        public override void Run(string[] args)
        {
            int? numSamples = null;
            try
            {
                numSamples = int.Parse(args[0]);
            }
            catch { }

            if (null == numSamples)
                Run();
            else
                Run(numSamples.Value);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0} [OPT:numSamples]", this.Name);
        }

        #endregion ConsoleAppModule
    }


    public class Numerics_RNG_Speed : ConsoleAppModule
    {
        public static void Run(int numSamples = 100000000)
        {
            RngTestUtil.TestRNGsHelper(Tuple.Create("Speed", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Time(numSamples, writeToStdOut))));
        }

        #region ConsoleAppModule

        public override void Run(string[] args)
        {
            int? numSamples = null;
            try
            {
                numSamples = int.Parse(args[0]);
            }
            catch { }

            if (null == numSamples)
                Run();
            else
                Run(numSamples.Value);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0} [OPT:numSamples]", this.Name);
        }

        #endregion ConsoleAppModule
    }


    public class Numerics_RNG_RandomnessCheck : ConsoleAppModule
    {
        public static void Run(RngTestFlag testFlag
            = RngTestFlag.Equidistribution | RngTestFlag.Diehard_Birthday | RngTestFlag.Diehard_CountOnes | RngTestFlag.Diehard_3DSphere)
        {
            var tests = new List<Tuple<string, Action<RngTest, Action<string>>>>();

            if (testFlag.HasFlag(RngTestFlag.Equidistribution))
            {
                const int numSamples = 10000;
                tests.Add(Tuple.Create("Equidistribution", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Equidistribution(numSamples, writeToStdOut))));
            }

            if (testFlag.HasFlag(RngTestFlag.Diehard_Birthday))
            {
                const int numTrials = 1 << 9;
                tests.Add(Tuple.Create("Diehard_Birthday_32", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_Birthday_32(numTrials, writeToStdOut))));
#if !X86
                tests.Add(Tuple.Create("Diehard_Birthday_64", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_Birthday_64(numTrials, writeToStdOut))));
#endif
            }

            if (testFlag.HasFlag(RngTestFlag.Diehard_CountOnes))
            {
                tests.Add(Tuple.Create("Diehard_CountOnes_32", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_CountOnes_32(writeToStdOut))));
#if !X86
                tests.Add(Tuple.Create("Diehard_CountOnes_64", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_CountOnes_64(writeToStdOut))));
#endif
            }

            if (testFlag.HasFlag(RngTestFlag.Diehard_3DSphere))
            {
                const int numTrials = 1 << 5;
                tests.Add(Tuple.Create("Diehard_3DSphere", (Action<RngTest, Action<string>>)((rngTest, writeToStdOut) => rngTest.Diehard_3DSphere(numTrials, writeToStdOut))));
            }

            RngTestUtil.TestRNGsHelper(tests.ToArray());
        }

        #region ConsoleAppModule

        public override void Run(string[] args)
        {
            uint? rngTestFlags = null;
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                try
                {
                    string numAsString = args[0].Trim();
                    if (numAsString.StartsWith("0x"))
                    {
                        numAsString = numAsString.Substring(2);
                        rngTestFlags = uint.Parse(numAsString, NumberStyles.HexNumber);
                    }
                    else
                    {
                        rngTestFlags = uint.Parse(numAsString, NumberStyles.Number);
                    }
                }
                catch { }
            }
            if (null == rngTestFlags)
                Run();
            else
                Run((RngTestFlag)Enum.ToObject(typeof(RngTestFlag), rngTestFlags.Value));
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0} [OPT:RngTestFlags]", this.Name);
            foreach (RngTestFlag value in Enum.GetValues(typeof(RngTestFlag)))
            {
                Console.WriteLine("\t0x{0:X8}\t{1}", (uint)value, value.ToString());
            }
        }

        #endregion ConsoleAppModule
    }


    [Flags]
    public enum RngTestFlag : uint
    {
        Equidistribution    = 0x1U,
        Diehard_Birthday    = 0x2U,
        Diehard_CountOnes   = 0x4U,
        Diehard_3DSphere    = 0x8U,
    }
}
