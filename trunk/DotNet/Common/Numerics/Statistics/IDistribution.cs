using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Statistics
{
    public interface IDistribution
    {
        double Mean     { get; }
        double Stdev    { get; }
        double Cdf(double s);
    }
}
