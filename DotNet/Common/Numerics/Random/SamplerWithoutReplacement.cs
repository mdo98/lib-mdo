using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Random
{
    public class SamplerWithoutReplacement
    {
        private readonly IList<object> UnusedSamples = new List<object>();

        public SamplerWithoutReplacement(RandomNumberGenerator rng, int start, int range) : this(rng)
        {
            if (range <= 0)
                throw new ArgumentOutOfRangeException("range");

            if (start > int.MaxValue - (range - 1))
                throw new ArgumentOutOfRangeException("start");

            for (int i = 0; i < range; i++)
                this.UnusedSamples.Add(start + i);
        }

        public SamplerWithoutReplacement(RandomNumberGenerator rng, IEnumerable<object> samples) : this(rng)
        {
            if (null == samples)
                throw new ArgumentNullException("samples");

            foreach (object sample in samples)
                this.UnusedSamples.Add(sample);
        }

        private SamplerWithoutReplacement(RandomNumberGenerator rng)
        {
            if (null == rng)
                throw new ArgumentNullException("rng");

            this.RNG = rng;
        }

        public RandomNumberGenerator RNG    { get; private set; }

        public object Next()
        {
            if (this.UnusedSamples.Count <= 0)
                return null;

            int indx = this.RNG.Int32(0, this.UnusedSamples.Count);
            object item = this.UnusedSamples[indx];
            this.UnusedSamples.RemoveAt(indx);
            return item;
        }
    }
}
