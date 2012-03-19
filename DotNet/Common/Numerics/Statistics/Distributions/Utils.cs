using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public static class DistributionUtils
    {
        public static double GoodnessOfFit(int numSamples, Func<double> getSample, IContinuousDistribution refDist)
        {
            return KolmogorovSmirnov.GoodnessOfFit(numSamples, getSample, refDist.Cdf);
        }

        public static double GoodnessOfFit<TNumeric>(TNumeric[] samples, IDiscreteDistribution refDist)
            where TNumeric : struct, IConvertible
        {
            return ChiSquare.GoodnessOfFit(Sequence.Cast<TNumeric, long>(samples), refDist);
        }
    }
}
