using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public class LaggedFibonacciRng : RandomNumberGenerator
    {
        #region Fields

#if !X86
        private readonly IList<ulong> Seeds = new List<ulong>();
#else
        private readonly IList<uint> Seeds = new List<uint>();
#endif

        private readonly bool SubtractiveMethod;
        private readonly int L1, L2;
        private int J, K;

        #endregion Fields


        #region Constructors

        public LaggedFibonacciRng() : this(BitConverter.ToUInt64(GetSeed(8), 0))
        { }

        public LaggedFibonacciRng(ulong seed, int lag1 = 24, int lag2 = 55, bool useSubtractiveMethod = true)
        {
            J = L1 = lag1;
            K = L2 = lag2;
            SubtractiveMethod = useSubtractiveMethod;
        }

        #endregion Constructors


        #region RandomNumberGenerator

        protected override ulong Sample()
        {
            ulong val;
            unchecked { val = Seeds[K] = (SubtractiveMethod ? (Seeds[K] - Seeds[J]) : (Seeds[K] + Seeds[J])); }
            J--; K--;
            if (J == 0) J = L2;
            if (K == 0) K = L2;
            return val;
        }

        #endregion RandomNumberGenerator
    }
}
