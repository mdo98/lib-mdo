using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Numerics.Random;
using System.Numerics.Statistics.Distributions;
using System.Text;
using System.Threading.Tasks;

namespace MDo.Common.Numerics.Test.Random
{
    public class RngTest
    {
        public RngTest(Func<IRandom> createRng)
        {
            if (null == createRng)
                throw new ArgumentNullException("createRng");
            IRandom rng = createRng();
            if (null == rng)
                throw new ArgumentException("createRng");

            this.CreateRNG = createRng;
            this.Name = rng.Name;
        }

        public Func<IRandom> CreateRNG { get; private set; }
        public string Name { get; private set; }

        public void PrintSamples(int numSamples, Stream outStream)
        {
            IRandom rng = this.CreateRNG();
            using (TextWriter writer = new StreamWriter(outStream))
            {
                writer.WriteLine(rng.Name);
                RandomNumberGenerator rngWithSampling = rng as RandomNumberGenerator;
                for (int i = 0; i < numSamples; i++)
                {
                    writer.WriteLine(null != rngWithSampling ? rngWithSampling.Sample().ToString() : rng.UInt32().ToString());
                }
            }
        }

        public void PrintSamplesBinary(int numSamples, Stream outStream)
        {
            IRandom rng = this.CreateRNG();
            RandomNumberGenerator rngWithSampling = rng as RandomNumberGenerator;
            for (int i = 0; i < numSamples; i++)
            {
                byte[] b = (null != rngWithSampling ? BitConverter.GetBytes(rngWithSampling.Sample()) : BitConverter.GetBytes(rng.UInt32()));
                outStream.Write(b, 0, b.Length);
            }
        }

        public void Time(int numSamples, Action<string> writeToStdOut)
        {
            writeToStdOut(string.Format("#Samples = {0:N0}", numSamples));

            IRandom rng;
            DateTime start; TimeSpan elapsed;

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                rng.Int32();
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Int:       {0,7:F3} seconds", elapsed.TotalSeconds));

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                rng.Double();
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Double:    {0,7:F3} seconds", elapsed.TotalSeconds));

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                byte[] b = new byte[10];
                rng.GetBytes(b);
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Byte[10]:  {0,7:F3} seconds", elapsed.TotalSeconds));
        }

        public void Equidistribution(int numSamples, Action<string> writeToStdOut)
        {
            writeToStdOut(string.Format("#Samples = {0:N0}", numSamples));

            IRandom rng = this.CreateRNG();
            double p = DistributionUtils.GoodnessOfFit(numSamples, () => rng.Double(), Uniform.Standard);
            writeToStdOut(string.Format("Goodness-of-fit to U(0,1): p-value = {0:F6}", p));
        }

        public void Diehard_Birthday_32(int numTrials, Action<string> writeToStdOut, int M = 10, int N = 24)
        {
            if (N < 18 || N >= 32)
                throw new ArgumentOutOfRangeException("N");
            int poisson_mean_lg = 3*M - (N+2);
            if (poisson_mean_lg < 1 || poisson_mean_lg > 16)
                throw new ArgumentOutOfRangeException("M");

            writeToStdOut(string.Format("#Trials = {0:N0}", numTrials));

            writeToStdOut(string.Format("#Bdays=2^{0}, #Days=2^{1}, SpaceR~Poisson(Mu={2})", M, N, 1 << poisson_mean_lg));
            writeToStdOut(string.Format("Bits\tMean\tp-value"));
            uint mask = ~((~0U) << N);
            int numSamples = 1 << M;
            for (int b = 0; b < 32-(N-1); b++)
            {
                IRandom rng = this.CreateRNG();
                long[] bdayObs = new long[numTrials];
                Parallel.For(0, numTrials, (int e) =>
                {
                    uint[] bdaySpace = new uint[numSamples];
                    for (int i = 0; i < numSamples; i++)
                    {
                        bdaySpace[i] = (rng.UInt32() >> b) & mask;
                    }
                    Array.Sort(bdaySpace);
                    for (int i = numSamples - 1; i > 0; i--)
                    {
                        bdaySpace[i] -= bdaySpace[i-1];
                    }
                    Array.Sort(bdaySpace);
                    long numDuplicates = 0L;
                    for (int i = 1; i < numSamples; i++)
                    {
                        if (bdaySpace[i] == bdaySpace[i-1])
                            numDuplicates++;
                    }
                    bdayObs[e] = numDuplicates;
                });
                double p = DistributionUtils.GoodnessOfFit(bdayObs, new Poisson(1 << poisson_mean_lg));
                writeToStdOut(string.Format("{0,2}->{1,2}\t{2:F2}\t{3:F6}", b, b+(N-1), Sequence.Mean(bdayObs), p));
            }
        }

        public void Diehard_CountOnes_32(Action<string> writeToStdOut)
        {
            const int numWords = 1 << 18;
            const int df = 2500; const double stdev = 70.71067811865475244 /* sqrt(5000) */;
            writeToStdOut(string.Format("#Words = {0:N0}; ChiSquare(df = 5^4-5^3 = 2500)", numWords));

            double[] prob = { 37.0/256.0, 56.0/256.0, 70.0/256.0, 56.0/256.0, 37.0/256.0 };
            Func<byte, int> byteToLetter = (byte b) =>
            {
                const byte mask = 0x1;
                int numOnes = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (((b >> i) & mask) == mask)
                        numOnes++;
                }
                numOnes -= 2;
                if (numOnes < 0) return 0;
                else if (numOnes > 4) return 4;
                else return numOnes;
            };
            
            IRandom rng = this.CreateRNG();
            uint sample = 0U; const int maxShifts = 4; int numShifts = 0;
            Func<byte> nextByte = () =>
            {
                if (numShifts == 0)
                    sample = rng.UInt32();
                byte b = (byte)((sample >> (8 * numShifts)) & 0xFFU);
                if (++numShifts >= maxShifts)
                    numShifts = 0;
                return b;
            };
            int[] f4 = new int[625], f5 = new int[3125];
            int word = 125*byteToLetter(nextByte()) + 25*byteToLetter(nextByte()) + 5*byteToLetter(nextByte()) + byteToLetter(nextByte());
            for (int i = 0; i < numWords; i++)
            {
                f4[word]++;
                word = 5*word + byteToLetter(nextByte());   // Shift word left, add new letter
                f5[word]++;
                word = word % 625;                          // Erase leftmost letter of word
            }
            double chisq = 0.0;
            foreach (int numLettersPerWord in new int[] { 4, 5 })
            {
                int[] f;
                if (numLettersPerWord == 4)
                {
                    f = f4;
                }
                else
                {
                    f = f5;
                    chisq = -chisq;
                }
                for (int i = 0; i < f.Length; i++)
                {
                    word = i;
                    double exp = numWords;
                    for (int j = 0; j < numLettersPerWord; j++)
                    {
                        int letter = word % 5;
                        exp *= prob[letter];
                        word = word / 5;
                    }
                    chisq += (Operators.SquareDifference(f[i], exp) / exp);
                }
            }
            writeToStdOut("chisq\tz-score\tp-value");
            double p = (new Normal(df, stdev)).Cdf(chisq);
            writeToStdOut(string.Format("{0:F2}\t{1:F4}\t{2:F6}", chisq, (chisq-df)/stdev, p));
        }

#if !X86
        public void Diehard_Birthday_64(int numTrials, Action<string> writeToStdOut, int M = 12, int N = 30)
        {
            if (N < 18 || N >= 64)
                throw new ArgumentOutOfRangeException("N");
            int poisson_mean_lg = 3*M - (N+2);
            if (poisson_mean_lg < 1 || poisson_mean_lg > 16)
                throw new ArgumentOutOfRangeException("M");

            writeToStdOut(string.Format("#Trials = {0:N0}", numTrials));

            writeToStdOut(string.Format("#Bdays=2^{0}, #Days=2^{1}, SpaceR~Poisson(Mu={2})", M, N, 1 << poisson_mean_lg));
            writeToStdOut(string.Format("Bits\tMean\tp-value"));
            ulong mask = ~((~0UL) << N);
            int numSamples = 1 << M;
            for (int b = 0; b < 64-(N-1); b++)
            {
                IRandom rng = this.CreateRNG();
                long[] bdayObs = new long[numTrials];
                Parallel.For(0, numTrials, (int e) =>
                {
                    ulong[] bdaySpace = new ulong[numSamples];
                    for (int i = 0; i < numSamples; i++)
                    {
                        bdaySpace[i] = (rng.UInt64() >> b) & mask;
                    }
                    Array.Sort(bdaySpace);
                    for (int i = numSamples - 1; i > 0; i--)
                    {
                        bdaySpace[i] -= bdaySpace[i-1];
                    }
                    Array.Sort(bdaySpace);
                    long numDuplicates = 0L;
                    for (int i = 1; i < numSamples; i++)
                    {
                        if (bdaySpace[i] == bdaySpace[i-1])
                            numDuplicates++;
                    }
                    bdayObs[e] = numDuplicates;
                });
                double p = DistributionUtils.GoodnessOfFit(bdayObs, new Poisson(1 << poisson_mean_lg));
                writeToStdOut(string.Format("{0,2}->{1,2}\t{2:F2}\t{3:F6}", b, b+(N-1), Sequence.Mean(bdayObs), p));
            }
        }

        public void Diehard_CountOnes_64(Action<string> writeToStdOut)
        {
            const int numWords = 1 << 20;
            const int df = 2500; const double stdev = 70.71067811865475244 /* sqrt(5000) */;
            writeToStdOut(string.Format("#Words = {0:N0}; ChiSquare(df = 5^4-5^3 = 2500)", numWords));

            double[] prob = { 37.0/256.0, 56.0/256.0, 70.0/256.0, 56.0/256.0, 37.0/256.0 };
            Func<byte, int> byteToLetter = (byte b) =>
            {
                const byte mask = 0x1;
                int numOnes = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (((b >> i) & mask) == mask)
                        numOnes++;
                }
                numOnes -= 2;
                if (numOnes < 0) return 0;
                else if (numOnes > 4) return 4;
                else return numOnes;
            };
            
            IRandom rng = this.CreateRNG();
            ulong sample = 0UL; const int maxShifts = 8; int numShifts = 0;
            Func<byte> nextByte = () =>
            {
                if (numShifts == 0)
                    sample = rng.UInt64();
                byte b = (byte)((sample >> (8 * numShifts)) & 0xFFUL);
                if (++numShifts >= maxShifts)
                    numShifts = 0;
                return b;
            };
            int[] f4 = new int[625], f5 = new int[3125];
            int word = 125*byteToLetter(nextByte()) + 25*byteToLetter(nextByte()) + 5*byteToLetter(nextByte()) + byteToLetter(nextByte());
            for (int i = 0; i < numWords; i++)
            {
                f4[word]++;
                word = 5*word + byteToLetter(nextByte());   // Shift word left, add new letter
                f5[word]++;
                word = word % 625;                          // Erase leftmost letter of word
            }
            double chisq = 0.0;
            foreach (int numLettersPerWord in new int[] { 4, 5 })
            {
                int[] f;
                if (numLettersPerWord == 4)
                {
                    f = f4;
                }
                else
                {
                    f = f5;
                    chisq = -chisq;
                }
                for (int i = 0; i < f.Length; i++)
                {
                    word = i;
                    double exp = numWords;
                    for (int j = 0; j < numLettersPerWord; j++)
                    {
                        int letter = word % 5;
                        exp *= prob[letter];
                        word = word / 5;
                    }
                    chisq += (Operators.SquareDifference(f[i], exp) / exp);
                }
            }
            writeToStdOut("chisq\tz-score\tp-value");
            double p = (new Normal(df, stdev)).Cdf(chisq);
            writeToStdOut(string.Format("{0:F2}\t{1:F4}\t{2:F6}", chisq, (chisq-df)/stdev, p));
        }
#endif

        private class Point3D
        {
            public Point3D(double x, double y, double z)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
            public double X; public double Y; public double Z;
            public double SquareDistance(Point3D other)
            {
                return  Operators.SquareDifference(this.X, other.X)
                    +   Operators.SquareDifference(this.Y, other.Y)
                    +   Operators.SquareDifference(this.Z, other.Z);
            }
        }

        public void Diehard_3DSphere(int numTrials, Action<string> writeToStdOut)
        {
            if (numTrials < 30)
                throw new ArgumentOutOfRangeException("numTrials");

            writeToStdOut(string.Format("#Trials = {0:N0}", numTrials));

            const int numPoints = 4000;
            const double cubeEdge = 1000.0;

            IRandom rng = this.CreateRNG();
            Func<double> getSample = () =>
            {
                Point3D[] points = new Point3D[numPoints];
                for (int i = 0; i < numPoints; i++)
                {
                    points[i] = new Point3D(cubeEdge * rng.Double(), cubeEdge * rng.Double(), cubeEdge * rng.Double());
                }
                object syncObj = new object();
                double r3min = double.PositiveInfinity;
                Parallel.For(1, numPoints, (int i) =>
                {
                    double[] r3 = new double[i];
                    for (int j = 0; j < i; j++)
                    {
                        double r2 = points[i].SquareDistance(points[j]);
                        double r1 = Math.Sqrt(r2);
                        r3[j] = r2 * r1;
                    }
                    double r3min_j = Sequence.Min(r3);
                    lock (syncObj)
                    {
                        if (r3min_j < r3min)
                            r3min = r3min_j;
                    }
                });
                double uniformEquiv = 1.0 - Math.Exp(-r3min / 30.0);
                writeToStdOut(string.Format("{0,7:F3}\t{1:F6}", r3min, uniformEquiv));
                return uniformEquiv;
            };
            writeToStdOut("R^3\tU(0,1)");
            double p = DistributionUtils.GoodnessOfFit(numTrials, getSample, Uniform.Standard);
            writeToStdOut(string.Format("Diehard_3DSphere: p-value = {0:F6}", p));
        }
    }
}
