using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public class Gamma : IContinuousDistribution
    {
        public Gamma(double k, double theta)
        {
            this.K = k;
            this.Theta = theta;
        }


        #region Fields & Properties

        private double _K, _Theta;

        public double K
        {
            get
            {
                return _K;
            }
            private set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException("K");
                _K = value;
            }
        }

        public double Theta
        {
            get
            {
                return _Theta;
            }
            private set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException("Theta");
                _Theta = value;
            }
        }

        #endregion Fields & Properties


        #region Imports

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_gamma_P(double x, double a, double b);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_gamma_Q(double x, double a, double b);

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

        public double Cdf_Q(double x)
        {
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            if (x == 0.0)
                return 0.0;

            return gsl_cdf_gamma_Q(x, this.K, this.Theta);
        }

        #endregion IDistribution
    }
}
