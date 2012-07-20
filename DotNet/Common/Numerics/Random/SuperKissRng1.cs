using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Random
{
    /// <summary>
    /// Implementation of George Marsaglia's Super KISS RNG.
    /// This method is claimed to have a period of 54767*2^1337279, about 10^402565.
    /// </summary>
    /// <remarks>sci.math -- RNGs: A Super KISS.
    /// http://groups.google.com/group/sci.crypt/browse_thread/thread/828e7c8bb187d829/6653ecd4c929fbf5
    /// </remarks>
    public class SuperKissRng1 : RandomNumberGenerator
    {
        #region Constructors

        public SuperKissRng1() : this(BitConverter.ToUInt32(GetSeed(4), 0))
        { }

        public SuperKissRng1(int seed) : this((uint)((long)seed - (long)int.MinValue))
        { }

        public SuperKissRng1(uint seed) : this(new uint[] { seed })
        { }

        public SuperKissRng1(uint[] seeds)
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

        internal const int QMAX = 41790;
        private int indx = QMAX;

        private readonly uint[] Q = new uint[QMAX];
        internal uint C = 362436U, XCNG = 1236789U, XS = 521288629U;

        #endregion Fields


        #region RandomNumberGenerator

        internal uint GetNextSample()
        {
            /*
            static unsigned long Q[41790],indx=41790,C=362436,XCNG=1236789,XS=521288629;

            define CNG ( XCNG=69609*XCNG+123 )  // Congruential
            define XS  ( XS^=XS<<13, XS^=(unsigned)XS>>17, XS^=XS>>5 )  // XOR shift
            define SUPR ( indx<41790 ? Q[indx++] : refill() )
            define KISS SUPR+CNG+XS

            int refill( )
            { int i; unsigned long long t;
              for(i=0;i<41790;i++) { t=7010176LL*Q[i]+C; C=(t>>32); Q[i]=~(t); }
              indx=1; return (Q[0]);
            }

            int main()
            { unsigned long i,x;
              for(i=0;i<41790;i++) Q[i]=CNG+XS;
              for(i=0;i<1000000000;i++) x=KISS;
              printf("x=%d.\nDoes x=-872412446?\n",x);
            }
            */
            if (indx >= QMAX)
            {
                for (int i = 0; i < QMAX; i++)
                {
                    ulong t = unchecked(7010176UL * (ulong)Q[i] + (ulong)C);
                    C = (uint)(t >> 32);
                    Q[i] = ~((uint)t);
                }
                indx = 0;
            }
            XCNG = unchecked(69609U * XCNG + 123U);
            XS = XS ^ (XS << 13);
            XS = XS ^ (XS >> 17);
            XS = XS ^ (XS >>  5);
            return unchecked(Q[indx++] + XCNG + XS);
        }

#if DEBUG
        internal static uint CNG(uint xcng)
        {
            return unchecked(69609U * xcng + 123U);
        }

        internal static uint XOR_Shift(uint xs)
        {
            xs = xs ^ (xs << 13);
            xs = xs ^ (xs >> 17);
            xs = xs ^ (xs >>  5);
            return xs;
        }
#endif

#if X86
        protected override uint InternalSample()
        {
            return this.GetNextSample();
        }
#else
        protected override ulong InternalSample()
        {
            return ToUInt64(this.GetNextSample(), this.GetNextSample());
        }
#endif

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
    }
}