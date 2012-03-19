using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public static class Gsl
    {
#if X86
        internal const string GSL_PATH = @"lib\GSL\gsl32.dll";
#else
        internal const string GSL_PATH = @"lib\GSL\gsl64.dll";
#endif

        public static void Invoke(Func<int> gslFuncCode)
        {
            int gslCode = gslFuncCode();
            if (gslCode != 0)
                throw new GslException() { Code = gslCode };
        }
    }
}
