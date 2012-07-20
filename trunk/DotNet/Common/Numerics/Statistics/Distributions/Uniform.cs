using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics.Statistics.Distributions
{
    public class Uniform : IContinuousDistribution
    {
        public Uniform(double lowerBound, double upperBound)
        {
            this.SetBounds(lowerBound, upperBound);
        }

        public static Uniform Standard
        {
            get { return new Uniform(0.0, 1.0); }
        }


        #region Properties

        public double Lower { get; private set; }
        public double Upper { get; private set; }

        #endregion Properties


        public void SetBounds(double lower, double upper)
        {
            if (upper <= lower)
                throw new ArgumentOutOfRangeException("upper");

            this.Lower = lower;
            this.Upper = upper;
        }


        #region IDistribution

        public double Mean
        {
            get { return (0.5 * (Lower + Upper)); }
        }

        public double Stdev
        {
            get { return Math.Sqrt(this.Variance); }
        }

        public double Variance
        {
            get { return Operators.SquareDifference(Upper, Lower) / 12.0; }
        }

        public double Cdf(double x)
        {
            if (x < this.Lower || x > this.Upper)
                throw new ArgumentOutOfRangeException("x");

            return (x - Lower) / (Upper - Lower);
        }

        public double Cdf_Q(double x)
        {
            if (x < this.Lower || x > this.Upper)
                throw new ArgumentOutOfRangeException("x");

            return (Upper - x) / (Upper - Lower);
        }

        public double Pdf(double x)
        {
            if (x < this.Lower || x > this.Upper)
                throw new ArgumentOutOfRangeException("x");

            return 1.0 / (Upper - Lower);
        }

        #endregion IDistribution
    }
}
