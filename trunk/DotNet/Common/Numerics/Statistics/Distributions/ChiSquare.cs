using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public class ChiSquare : IDistribution
    {
        public ChiSquare(int k)
        {
            this.K = k;
        }

        public int K { get; private set; }


        #region Imports

        [DllImport(Constants.GSL_PATH)]
        private static extern double gsl_cdf_chisq_P(double x, double nu);

        #endregion Imports


        #region IDistribution

        public double Mean
        {
            get { return this.K; }
        }

        public double Stdev
        {
            get { return Math.Sqrt(this.Variance); }
        }

        public double Variance
        {
            get { return 2.0 * this.K; }
        }

        public double Cdf(double x)
        {
            return gsl_cdf_chisq_P(x, this.K); 
        }

        #endregion IDistribution
    }
}
