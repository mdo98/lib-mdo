using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public class LaggedFibonacciRng : RandomNumberGenerator
    {
        #region Fields

        private readonly bool SubtractiveMethod;
        private readonly int L1, L2;
        private int J, K;

        #endregion Fields


        #region RandomNumberGenerator

        public override string Name
        {
            get
            {
                return string.Format(
                    "{0}: X_n = X_(n-{3}) {1} X_(n-{2}) (Seed = {4})",
                    base.Name,
                    this.SubtractiveMethod ? "-" : "+",
                    this.L1,
                    this.L2,
                    this.Seed);
            }
        }

        #endregion RandomNumberGenerator


#if !X86
        #region Fields

        private readonly ulong Seed;
        private readonly ulong[] Seeds;

        #endregion Fields


        #region Constructors

        public LaggedFibonacciRng() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        public LaggedFibonacciRng(int seed) : this((ulong)((long)seed - (long)int.MinValue))
        { }

        public LaggedFibonacciRng(ulong seed, int lag1 = 24, int lag2 = 55, bool useSubtractiveMethod = true)
        {
            if (lag1 < 1)
                throw new ArgumentOutOfRangeException("lag1");
            if (lag2 < lag1)
                throw new ArgumentOutOfRangeException("lag2");

            SubtractiveMethod = useSubtractiveMethod;
            L1 = lag1;
            L2 = lag2;

            J = lag2 - lag1;
            K = 0;

            Seed = seed;
            Seeds = GetSeedArray(L2, seed);
        }

        #endregion Constructors


        #region RandomNumberGenerator

        protected override ulong InternalSample()
        {
            unchecked { Seeds[K] = (SubtractiveMethod ? (Seeds[K] - Seeds[J]) : (Seeds[K] + Seeds[J])); }
            J++; K++;
            if (J >= L2) J = 0;
            if (K >= L2) K = 0;
            return Seeds[K];
        }

        #endregion RandomNumberGenerator
#else
        #region Fields
        
        private readonly uint Seed;
        private readonly uint[] Seeds;

        #endregion Fields


        #region Constructors

        public LaggedFibonacciRng() : this(BitConverter.ToUInt32(GetSeed(4), 0))
        { }

        public LaggedFibonacciRng(int seed) : this((uint)((long)seed - (long)int.MinValue))
        { }

        public LaggedFibonacciRng(uint seed, int lag1 = 24, int lag2 = 55, bool useSubtractiveMethod = true)
        {
            if (lag1 < 1)
                throw new ArgumentOutOfRangeException("lag1");
            if (lag2 < lag1)
                throw new ArgumentOutOfRangeException("lag2");

            SubtractiveMethod = useSubtractiveMethod;
            J = L1 = lag1;
            K = L2 = lag2;

            Seed = seed;
            Seeds = GetSeedArray(L2, seed);
        }

        #endregion Constructors


        #region RandomNumberGenerator

        protected override uint InternalSample()
        {
            unchecked { Seeds[K] = (SubtractiveMethod ? (Seeds[K] - Seeds[J]) : (Seeds[K] + Seeds[J])); }
            J--; K--;
            if (J == 0) J = L2;
            if (K == 0) K = L2;
            return Seeds[K];
        }

        #endregion RandomNumberGenerator
#endif
    }
}
