using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            writeToStdOut(string.Format("#Bdays=2^{0}, #Days=2^{1}, SpaceR~Poisson(Mu={2})", M, N, 1 << poisson_mean_lg));
            writeToStdOut(string.Format("Bits\tMean\tp-value"));
            uint mask = ~((~0U) << N);
            int numSamples = 1 << M;
            for (int b = 0; b < 8*sizeof(uint)-(N-1); b++)
            {
                IRandom rng = this.CreateRNG();
                int[] bdayObs = new int[numExperiments];
                Parallel.For(0, numExperiments, (int e) =>
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
                });
                double p = DistributionUtils.GoodnessOfFit(bdayObs, new Poisson((double)(1 << poisson_mean_lg)));
                writeToStdOut(string.Format("{0,2}->{1,2}\t{2:F2}\t{3:F6}", b, b+(N-1), Sequence.Mean(bdayObs), p));
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
            double p = DistributionUtils.GoodnessOfFit(numExperiments, getSample, Uniform.Standard);
            writeToStdOut(string.Format("Diehard_3DSphere: p-value = {0:F6}", p));
        }
    }
}
