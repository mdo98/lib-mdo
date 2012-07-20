using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Random
{
    public class MT19937Rng : RandomNumberGenerator
    {
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

        #endregion RandomNumberGenerator


#if !X86
        #region Constants

        private const int N = 312;
        private const int M = 156;
        private const ulong UPPER_MASK = 0xFFFFFFFF80000000UL;
        private const ulong LOWER_MASK = 0x7FFFFFFFUL;

        private const ulong TEMPERING_MASK_A = 0x5555555555555555UL;
        private const ulong TEMPERING_MASK_B = 0x71D67FFFEDA60000UL;
        private const ulong TEMPERING_MASK_C = 0xFFF7EEE000000000UL;

        private const ulong MATRIX_A = 0xB5026F5AA96619E9UL;
        private static readonly ulong[] MAG01 = { 0UL, MATRIX_A };

        private const ulong INIT_MULTIPLIER = 6364136223846793005UL;
        private const int INIT_NUMBITSHIFT = 62;

        #endregion Constants


        #region Fields

        internal readonly ulong Seed;
        private readonly ulong[] MTState = new ulong[N];
        private int MTI = N + 1;

        #endregion Fields


        #region Constructors

        public MT19937Rng() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        public MT19937Rng(int seed) : this((ulong)((long)seed - (long)int.MinValue))
        { }

        public MT19937Rng(ulong seed)
        {
            Seed = seed;
            this.MTInit(seed);
        }

        #endregion Constructors


        #region RandomNumberGenerator

        protected override ulong InternalSample()
        {
            ulong y;
            if (MTI >= N)
            {
                /*
                // Constructors of this class are expected to init -- there is no reason this step should be performed.
                if (MTI == N + 1)
                    this.MTInit(5489UL);
                */
                int k;
                for (k = 0; k < N - M; k++)
                {
                    y = (MTState[k] & UPPER_MASK) | (MTState[k + 1] & LOWER_MASK);
                    MTState[k] = MTState[k + M] ^ (y >> 1) ^ MAG01[y & 0x1UL];
                }
                for (; k < N - 1; k++)
                {
                    y = (MTState[k] & UPPER_MASK) | (MTState[k + 1] & LOWER_MASK);
                    MTState[k] = MTState[k + (M - N)] ^ (y >> 1) ^ MAG01[y & 0x1UL];
                }
                y = (MTState[N - 1] & UPPER_MASK) | (MTState[0] & LOWER_MASK);
                MTState[N - 1] = MTState[M - 1] ^ (y >> 1) ^ MAG01[y & 0x1UL];
                // Reset MTI
                MTI = 0;
            }
            y = MTState[MTI++];
            // Tempering
            y ^= TEMPERING_SHIFT_U(y) & TEMPERING_MASK_A;
            y ^= TEMPERING_SHIFT_S(y) & TEMPERING_MASK_B;
            y ^= TEMPERING_SHIFT_T(y) & TEMPERING_MASK_C;
            y ^= TEMPERING_SHIFT_L(y);
            return y;
        }

        #endregion RandomNumberGenerator


        #region Internal Operations

        private void MTInit(ulong seed)
        {
            MTState[0] = seed;
            for (MTI = 1; MTI < N; MTI++)
            {
                unchecked
                {
                    MTState[MTI] = INIT_MULTIPLIER * (MTState[MTI - 1] ^ (MTState[MTI - 1] >> INIT_NUMBITSHIFT)) + (ulong)MTI;
                }
            }
        }

        private static ulong TEMPERING_SHIFT_U(ulong y) { return (y >> 29); }
        private static ulong TEMPERING_SHIFT_S(ulong y) { return (y << 17); }
        private static ulong TEMPERING_SHIFT_T(ulong y) { return (y << 37); }
        private static ulong TEMPERING_SHIFT_L(ulong y) { return (y >> 43); }

        #endregion Internal Operations
#else
        #region Constants

        private const int N = 624;
        private const int M = 397;
        private const uint UPPER_MASK = 0x80000000U;
        private const uint LOWER_MASK = 0x7FFFFFFFU;

        private const uint TEMPERING_MASK_B = 0x9D2C5680U;
        private const uint TEMPERING_MASK_C = 0xEFC60000U;

        private const uint MATRIX_A = 0x9908B0DFU;
        private static readonly uint[] MAG01 = { 0U, MATRIX_A };

        private const uint INIT_MULTIPLIER = 1812433253U;
        private const int INIT_NUMBITSHIFT = 30;

        #endregion Constants


        #region Fields

        internal readonly uint Seed;
        private readonly uint[] MTState = new uint[N];
        private int MTI = N + 1;

        #endregion Fields


        #region Constructors

        public MT19937Rng() : this(BitConverter.ToUInt32(GetSeed(4), 0))
        { }

        public MT19937Rng(int seed) : this((uint)((long)seed - (long)int.MinValue))
        { }

        public MT19937Rng(uint seed)
        {
            Seed = seed;
            this.MTInit(seed);
        }

        #endregion Constructors


        #region RandomNumberGenerator

        protected override uint InternalSample()
        {
            uint y;
            if (MTI >= N)
            {
                /*
                // Constructors of this class are expected to init -- there is no reason this step should be performed.
                if (MTI == N + 1)
                    this.MTInit(5489U);
                */
                int k;
                for (k = 0; k < N - M; k++)
                {
                    y = (MTState[k] & UPPER_MASK) | (MTState[k + 1] & LOWER_MASK);
                    MTState[k] = MTState[k + M] ^ (y >> 1) ^ MAG01[y & 0x1U];
                }
                for (; k < N - 1; k++)
                {
                    y = (MTState[k] & UPPER_MASK) | (MTState[k + 1] & LOWER_MASK);
                    MTState[k] = MTState[k + (M - N)] ^ (y >> 1) ^ MAG01[y & 0x1U];
                }
                y = (MTState[N - 1] & UPPER_MASK) | (MTState[0] & LOWER_MASK);
                MTState[N - 1] = MTState[M - 1] ^ (y >> 1) ^ MAG01[y & 0x1U];
                // Reset MTI
                MTI = 0;
            }
            y = MTState[MTI++];
            // Tempering
            y ^= TEMPERING_SHIFT_U(y);
            y ^= TEMPERING_SHIFT_S(y) & TEMPERING_MASK_B;
            y ^= TEMPERING_SHIFT_T(y) & TEMPERING_MASK_C;
            y ^= TEMPERING_SHIFT_L(y);
            return y;
        }

        #endregion RandomNumberGenerator


        #region Internal Operations

        private void MTInit(uint seed)
        {
            MTState[0] = seed;
            for (MTI = 1; MTI < N; MTI++)
            {
                unchecked
                {
                    MTState[MTI] = INIT_MULTIPLIER * (MTState[MTI - 1] ^ (MTState[MTI - 1] >> INIT_NUMBITSHIFT)) + (uint)MTI;
                }
            }
        }

        private static uint TEMPERING_SHIFT_U(uint y) { return (y >> 11); }
        private static uint TEMPERING_SHIFT_S(uint y) { return (y <<  7); }
        private static uint TEMPERING_SHIFT_T(uint y) { return (y << 15); }
        private static uint TEMPERING_SHIFT_L(uint y) { return (y >> 18); }

        #endregion Internal Operations
#endif
    }
}
