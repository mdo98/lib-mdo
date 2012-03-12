using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public interface IDistribution
    {
        double Mean     { get; }
        double Stdev    { get; }
        double Variance { get; }
        double Cdf(double x);
    }
}
