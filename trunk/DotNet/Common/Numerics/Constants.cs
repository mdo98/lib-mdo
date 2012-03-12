using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public static class Constants
    {
#if X86
        internal const string GSL_PATH = @"lib\GSL\gsl32.dll";
#else
        internal const string GSL_PATH = @"lib\GSL\gsl64.dll";
#endif

        public const double LN_2 = 0.69314718055994530941723212145818;          // ln(2)
        public const double PI_SQUARE = 9.8696044010893586188344909998762;      // Pi^2
        public const double PI_QUADRUPLE = 97.409091034002437236440332688705;   // Pi^4
        public const double SQRT_2PI = 2.506628274631000502415765284811;        // sqrt(2*Pi)
        public const double SQRT_HALFPI = 1.2533141373155002512078826424055;    // sqrt(Pi/2)
    }
}
