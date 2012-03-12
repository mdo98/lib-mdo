using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    /// <summary>
    /// Implementation of George Marsaglia's revised Super KISS RNG for 32 and 64-bit machines.
    /// This method is claimed to have a period of 5*2^1320481 (10^397505) for the 32-bit version,
    /// and 5*2^1320480 (10^397504) for the 64-bit version.
    /// </summary>
    /// <remarks>sci.math -- SuperKISS for 32- and 64-bit RNGs in both C and Fortran.
    /// http://groups.google.com/group/sci.math/browse_thread/thread/4224eb6ead177b23/af781ad30191a4fe
    /// </remarks>
    public class SuperKissRng2 : RandomNumberGenerator
    {
        #region RandomNumberGenerator

        public override string Name
        {
            get
            {
                if (null == this.SeedArray || this.SeedArray.Length == 0)
                    return base.Name;

                StringBuilder seedArray = new StringBuilder();
                seedArray.Append("{ ");
                seedArray.Append(this.SeedArray[0]);
                for (int i = 1; i < this.SeedArray.Length; i++)
                {
                    seedArray.Append(", ");
                    seedArray.Append(this.SeedArray[i]);
                }
                seedArray.Append(" }");
                return string.Format(
                    "{0} (Seed = {1})",
                    base.Name,
                    seedArray.ToString());
            }
        }

        #endregion RandomNumberGenerator

#if !X86
        #region Constructors

        public SuperKissRng2() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        internal SuperKissRng2(int seed) : this((ulong)((long)seed - (long)int.MinValue))
        { }

        public SuperKissRng2(ulong seed) : this(new ulong[] { seed })
        { }

        public SuperKissRng2(ulong[] seeds)
        {
            this.Init(seeds);
        }

        internal void Init(ulong[] seeds)
        {
            if (null == seeds || seeds.Length == 0)
                throw new ArgumentNullException("seeds");

            this.SeedArray = seeds;
            int s = Math.Min(seeds.Length, QMAX) - 1;
            Array.Copy(seeds, 0, Q, 0, s);
            if (s < QMAX - 1)
            {
                int numExtraSeeds = QMAX - s;
                ulong[] extraSeeds = GetSeedArray(numExtraSeeds, seeds[s]);
                Array.Copy(extraSeeds, 0, Q, s, numExtraSeeds);
            }
            else
            {
                Q[s] = seeds[s];
            }
        }

        #endregion Constructors


        #region Fields

        internal ulong[] SeedArray;

        internal const int QMAX = 20632;
        private int indx = QMAX;

        private readonly ulong[] Q = new ulong[QMAX];
        internal ulong C = 36243678541UL, XCNG = 12367890123456UL, XS = 521288629546311UL;

        #endregion Fields


        #region RandomNumberGenerator

        protected override ulong InternalSample()
        {
            if (indx >= QMAX)
            {
                for (int i = 0; i < QMAX; i++)
                {
                    ulong h = C & 1UL;
                    ulong z = ((Q[i] << 41) >> 1) + ((Q[i] << 39) >> 1) + (C >> 1);
                    C = (Q[i] >> 23) + (Q[i] >> 25) + (z >> 63);
                    Q[i] = ~((z << 1) + h); 
                }
                indx = 0;
            }
            XCNG = 6906969069UL * XCNG + 123UL;
            XS = XS ^ (XS << 13);
            XS = XS ^ (XS >> 17);
            XS = XS ^ (XS << 43);
            return unchecked(Q[indx++] + XCNG + XS);
        }

#if DEBUG
        internal static ulong CNG(ulong xcng)
        {
            return 6906969069UL * xcng + 123UL;
        }

        internal static ulong XOR_Shift(ulong xs)
        {
            xs = xs ^ (xs << 13);
            xs = xs ^ (xs >> 17);
            xs = xs ^ (xs << 43);
            return xs;
        }
#endif
        #endregion RandomNumberGenerator
#else
        #region Constructors

        public SuperKissRng2() : this(BitConverter.ToUInt32(GetSeed(4), 0))
        { }

        public SuperKissRng2(int seed) : this((uint)((long)seed - (long)int.MinValue))
        { }

        public SuperKissRng2(uint seed) : this(new uint[] { seed })
        { }

        public SuperKissRng2(uint[] seeds)
        {
            this.Init(seeds);
        }

        internal void Init(uint[] seeds)
        {
            if (null == seeds || seeds.Length == 0)
                throw new ArgumentNullException("seeds");

            this.SeedArray = seeds;
            int s = Math.Min(seeds.Length, QMAX) - 1;
            Array.Copy(seeds, 0, Q, 0, s);
            if (s < QMAX - 1)
            {
                int numExtraSeeds = QMAX - s;
                uint[] extraSeeds = GetSeedArray(numExtraSeeds, seeds[s]);
                Array.Copy(extraSeeds, 0, Q, s, numExtraSeeds);
            }
            else
            {
                Q[s] = seeds[s];
            }
        }

        #endregion Constructors


        #region Fields

        internal uint[] SeedArray;

        internal const int QMAX = 41265;
        private int indx = QMAX;

        private readonly uint[] Q = new uint[QMAX];
        internal uint C = 362U, XCNG = 1236789U, XS = 521288629U;

        #endregion Fields


        #region RandomNumberGenerator

        protected override uint InternalSample()
        {
            if (indx >= QMAX)
            {
                for (int i = 0; i < QMAX; i++)
                {
                    uint h = C & 1U;
                    uint z = ((Q[i] << 9) >> 1) + ((Q[i] << 7) >> 1) + (C >> 1);
                    C = (Q[i] >> 23) + (Q[i] >> 25) + (z >> 31);
                    Q[i] = ~((z << 1) + h); 
                }
                indx = 0;
            }
            XCNG = 69069U * XCNG + 123U;
            XS = XS ^ (XS << 13);
            XS = XS ^ (XS >> 17);
            XS = XS ^ (XS <<  5);
            return unchecked(Q[indx++] + XCNG + XS);
        }

#if DEBUG
        internal static uint CNG(uint xcng)
        {
            return 69069U * xcng + 123U;
        }

        internal static uint XOR_Shift(uint xs)
        {
            xs = xs ^ (xs << 13);
            xs = xs ^ (xs >> 17);
            xs = xs ^ (xs <<  5);
            return xs;
        }
#endif
        #endregion RandomNumberGenerator
#endif
    }
}