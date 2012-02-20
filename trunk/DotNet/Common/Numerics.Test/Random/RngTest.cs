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
                    writer.WriteLine(null != rngWithSampling ? rngWithSampling.Sample().ToString() : rng.Int().ToString());
                }
            }
        }

        public void PrintSamplesBinary(int numSamples, Stream outStream)
        {
            IRandom rng = this.CreateRNG();
            RandomNumberGenerator rngWithSampling = rng as RandomNumberGenerator;
            for (int i = 0; i < numSamples; i++)
            {
                byte[] b = (null != rngWithSampling ? BitConverter.GetBytes(rngWithSampling.Sample()) : BitConverter.GetBytes(rng.Int()));
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
                rng.Int();
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

        public void Equidistribution(int numSamples, Action<string> writeToStandardOut)
        {
            IRandom rng = this.CreateRNG();
            double p = KolmogorovSmirnov.TestGoodnessOfFit(numSamples, () => rng.Double(), (double s) => s);
            writeToStandardOut(string.Format("Kolmogorov-Smirnov test: p-value = {0:F6}", p));
        }
    }
}
