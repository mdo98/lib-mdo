using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    }
}
