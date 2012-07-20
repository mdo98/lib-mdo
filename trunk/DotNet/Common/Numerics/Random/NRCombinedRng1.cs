#if !X86
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Random
{
    /// <summary>
    /// Implementation of the Ran combined RNG in Numerical Recipes.
    /// This method is claimed to have a period of 3.138E57.
    /// </summary>
    public class NRCombinedRng1 : RandomNumberGenerator
    {
        #region Constants

        private const ulong U_MULTIPLIER = 2862933555777941757UL;
        private const ulong U_INCREMENT = 7046029254386353087UL;
        private const ulong V_SEED = 4101842887655102017UL;
        private const ulong W_SEED = 1L;
        private const ulong W_MULTIPLER = 4294957665UL;

        #endregion Constants


        #region Fields

        internal readonly ulong Seed;
        private ulong
            U,
            V = V_SEED,
            W = W_SEED;

        #endregion Fields


        #region Constructors

        public NRCombinedRng1() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        public NRCombinedRng1(int seed) : this((ulong)((long)seed - (long)int.MinValue))
        { }

        public NRCombinedRng1(ulong seed)
        {
            Seed = seed;
            if (seed == V_SEED)
                seed--;
            U = seed ^ V;   InternalSample();
            V = U;          InternalSample();
            W = V;          InternalSample();
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
                U = U_MULTIPLIER * U + U_INCREMENT;
                V = V ^ (V >> 17);
                V = V ^ (V << 31);
                V = V ^ (V >>  8);
                W = W_MULTIPLER * (W & 0xFFFFFFFFUL) + (W >> 32);
                ulong X = U ^ (U << 21);
                X = X ^ (X >> 35);
                X = X ^ (X <<  4);
                return (X + V) ^ W;
            }
        }

        #endregion RandomNumberGenerator
    }
}
#endif
