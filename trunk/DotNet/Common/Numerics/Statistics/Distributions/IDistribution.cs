using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Statistics.Distributions
{
    public interface IDistribution
    {
        double Mean     { get; }
        double Stdev    { get; }
        double Variance { get; }
    }

    public interface IContinuousDistribution : IDistribution
    {
        double Cdf(double x);
        double Cdf_Q(double x);
        double Pdf(double x);
    }

    public interface IDiscreteDistribution : IDistribution
    {
        long Median     { get; }
        long Mode       { get; }
        double Cdf(long x);
        double Cdf_Q(long x);
        double Pmf(long x);
    }
}
