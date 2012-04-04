using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.Numerics.Random;

namespace MDo.Interop.R.Test.DataGeneration
{
    public class ClassificationDataGenerator
    {
        private readonly RandomNumberGenerator RNG;

        public ClassificationDataGenerator(RandomNumberGenerator rng)
        {
            this.RNG = rng;
        }
    }
}
