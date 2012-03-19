using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Common.Numerics.Statistics.Distributions;

namespace MDo.Common.Numerics.Random.Test
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
            IRandom rng;
            DateTime start; TimeSpan elapsed;

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                rng.Int32();
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Int: {0:F6} seconds", elapsed.TotalSeconds));

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                rng.Double();
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Double: {0:F6} seconds", elapsed.TotalSeconds));

            rng = this.CreateRNG();
            start = DateTime.Now;
            for (int i = 0; i < numSamples; i++)
            {
                byte[] b = new byte[10];
                rng.GetBytes(b);
            }
            elapsed = DateTime.Now - start;
            writeToStdOut(string.Format("Byte[10]: {0:F6} seconds", elapsed.TotalSeconds));
        }

        public void Equidistribution(int numSamples, Action<string> writeToStdOut)
        {
            IRandom rng = this.CreateRNG();
            double p = DistributionUtils.GoodnessOfFit(numSamples, () => rng.Double(), Uniform.Standard);
            writeToStdOut(string.Format("Goodness-of-fit to U(0,1): p-value = {0:F6}", p));
        }

        public void Diehard_Birthday(int numExperiments, Action<string> writeToStdOut, int M = 10, int N = 24)
        {
            if (N <= 0 || N > 32)
                throw new ArgumentOutOfRangeException("N");
            int poisson_mean_lg = 3 * M - (N + 2);
            if (poisson_mean_lg < 1 || poisson_mean_lg > 16)
                throw new ArgumentOutOfRangeException("M");

            uint mask = ~((~0U) << N);
            int numSamples = 1 << M;
            for (int b = 0; b <= 8*sizeof(uint) - N; b++)
            {
                IRandom rng = this.CreateRNG();
                int[] bdayObs = new int[numExperiments];
                for (int e = 0; e < numExperiments; e++)
                {
                    uint[] bdaySpace = new uint[numSamples];
                    for (int i = 0; i < numSamples; i++)
                    {
                        bdaySpace[i] = (rng.UInt32() >> b) & mask;
                    }
                    Array.Sort(bdaySpace);
                    for (int i = numSamples - 1; i > 0; i--)
                    {
                        bdaySpace[i] -= bdaySpace[i - 1];
                    }
                    Array.Sort(bdaySpace);
                    int numDuplicates = 0;
                    for (int i = 1; i < numSamples; i++)
                    {
                        if (bdaySpace[i] == bdaySpace[i - 1])
                            numDuplicates++;
                    }
                    bdayObs[e] = numDuplicates;
                }
                double p = DistributionUtils.GoodnessOfFit(bdayObs, new Poisson((double)(1 << poisson_mean_lg)));
                writeToStdOut(string.Format("Diehard_Birthday[bits {0} -> {1}]: p-value = {2:F6}", b, b + N - 1, p));
            }
        }

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

        public void Diehard_3DSphere(int numExperiments, Action<string> writeToStdOut)
        {
            if (numExperiments < 30)
                throw new ArgumentOutOfRangeException("numExperiments");

            const int numPoints = 4000;
            const double cubeEdge = 1000.0;

            IRandom rng = this.CreateRNG();
            Func<double> getSample = () =>
            {
                Point3D[] points = new Point3D[numPoints];
                double r3min = double.PositiveInfinity;
                for (int i = 0; i < numPoints; i++)
                {
                    points[i] = new Point3D(cubeEdge * rng.Double(), cubeEdge * rng.Double(), cubeEdge * rng.Double());
                    for (int j = 0; j < i; j++)
                    {
                        double r2 = points[i].SquareDistance(points[j]);
                        double r1 = Math.Sqrt(r2);
                        double r3 = r2 * r1;
                        if (r3 < r3min)
                            r3min = r3;
                    }
                }
                return Math.Exp(-r3min / 30.0);
            };
            double p = DistributionUtils.GoodnessOfFit(numExperiments, getSample, Uniform.Standard);
            writeToStdOut(string.Format("Diehard_3DSphere: p-value = {0:F6}", p));
        }
    }
}
