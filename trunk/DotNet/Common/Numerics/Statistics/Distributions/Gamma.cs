using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public class Gamma : IDistribution
    {
        public Gamma(double k, double theta)
        {
            if (k <= 0.0)
                throw new ArgumentOutOfRangeException("k");
            if (theta <= 0.0)
                throw new ArgumentOutOfRangeException("theta");

            this.K = k;
            this.Theta = theta;
        }

        public double K     { get; private set; }
        public double Theta { get; private set; }


        #region Imports

        [DllImport(Constants.GSL_PATH)]
        private static extern double gsl_cdf_gamma_P(double x, double a, double b);

        #endregion Imports


        #region IDistribution

        public double Mean
        {
            get { return this.K * this.Theta; }
        }

        public double Stdev
        {
            get { return Math.Sqrt(this.K) * this.Theta; }
        }

        public double Variance
        {
            get { return this.K * this.Theta * this.Theta; }
        }

        public double Cdf(double x)
        {
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            if (x == 0.0)
                return 0.0;

            return gsl_cdf_gamma_P(x, this.K, this.Theta);
        }

        #endregion IDistribution
    }
}
