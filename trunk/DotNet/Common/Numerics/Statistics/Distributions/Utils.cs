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
            return GoodnessOfFit(numSamples, getSample, refDist.Cdf);
        }

        public static double GoodnessOfFit(int numSamples, Func<double> getSample, Func<double, double> referenceCdf)
        {
            return KolmogorovSmirnov.GoodnessOfFit(numSamples, getSample, referenceCdf);
        }

        public static double GoodnessOfFit<T>(T[] samples, IDictionary<T, int> expected)
            where T : IEquatable<T>
        {
            return ChiSquare.GoodnessOfFit(samples, expected);
        }

        public static double GoodnessOfFit(long[] samples, IDiscreteDistribution refDist)
        {
            int numSamples = samples.Length;
            int minExpInBin = numSamples >> 7;

            SortedList<long, int> exp = new SortedList<long, int>();
            // Compute the expected count in each bin
            Action<long, Func<long, long>> computeExp = (long x0, Func<long, long> xIncr) =>
            {
                for (long k = x0; ; k = xIncr(k))
                {
                    int exp_k = (int)Math.Round(refDist.Pmf(k) * numSamples, MidpointRounding.ToEven);
                    if (exp_k < minExpInBin)
                        break;
                    exp.Add(k, exp_k);
                }
            };
            computeExp(refDist.Mode - 1L, (x) => (x - 1));
            computeExp(refDist.Mode     , (x) => (x + 1));

            return GoodnessOfFit(samples, exp);
        }
    }
}
