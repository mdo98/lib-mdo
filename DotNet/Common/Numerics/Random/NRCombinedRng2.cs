#if !X86
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    /// <summary>
    /// Implementation of the Ranq2 combined RNG in Numerical Recipes.
    /// This method is claimed to have a period of 8.5E37.
    /// </summary>
    public class NRCombinedRng2 : RandomNumberGenerator
    {
        #region Constants

        private const ulong V_SEED = 4101842887655102017UL;
        private const ulong W_SEED = 1L;
        private const ulong W_MULTIPLER = 4294957665UL;

        #endregion Constants


        #region Fields

        internal readonly ulong Seed;
        private ulong
            V = V_SEED,
            W = W_SEED;

        #endregion Fields


        #region Constructors

        public NRCombinedRng2() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        internal NRCombinedRng2(int seed) : this((ulong)((long)seed - (long)int.MinValue))
        { }

        public NRCombinedRng2(ulong seed)
        {
            Seed = seed;
            if (seed == V_SEED)
                seed--;
            V = seed ^ V;
            W = InternalSample();
            V = InternalSample();
        }

        #endregion Constructors


        #region RandomNumberGenerator

        public override string Name
        {
            get
            {
                return string.Format(
                    "{0} (Seed = {1})",
                    base.Name,
                    this.Seed);
            }
        }

        protected override ulong InternalSample()
        {
            unchecked
            {
                V = V ^ (V >> 17);
                V = V ^ (V << 31);
                V = V ^ (V >>  8);
                W = W_MULTIPLER * (W & 0xFFFFFFFFUL) + (W >> 32);
                return V ^ W;
            }
        }

        #endregion RandomNumberGenerator
    }
}
#endif
