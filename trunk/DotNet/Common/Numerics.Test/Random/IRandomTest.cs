using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random.Test
{
    public abstract class IRandomTest<T>
        where T : IRandom, new()
    {
        private readonly T RNG = new T();

        public sealed double Equidistribution(int numSamples)
        {
            for (int i = 0; i < numSamples; i++)
            {
                
            }
        }
    }
}
