using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Statistics.Distributions
{
    public class Normal : IContinuousDistribution
    {
        public Normal(double mu, double sigma)
        {
            this.Mu = mu;
            this.Sigma = sigma;
        }

        public static Normal Standard
        {
            get { return new Normal(0.0, 1.0); }
        }


        #region Fields & Properties

        private double _Mu, _Sigma;

        public double Mu
        {
            get
            {
                return _Mu;
            }
            private set
            {
                _Mu = value;
            }
        }

        public double Sigma
        {
            get
            {
                return _Sigma;
            }
            set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException("Sigma");
                _Sigma = value;
            }
        }

        #endregion Fields & Properties


        #region Imports

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_gaussian_P(double x, double sigma);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_gaussian_Q(double x, double sigma);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_ran_gaussian_pdf(double x, double sigma);

        #endregion Imports


        #region IDistribution

        public double Mean
        {
            get { return this.Mu; }
        }

        public double Stdev
        {
            get { return this.Sigma; }
        }

        public double Variance
        {
            get { return this.Sigma * this.Sigma; }
        }

        public double Cdf(double x)
        {
            return gsl_cdf_gaussian_P(x - this.Mu, this.Sigma);
        }

        public double Cdf_Q(double x)
        {
            return gsl_cdf_gaussian_Q(x - this.Mu, this.Sigma);
        }

        public double Pdf(double x)
        {
            return gsl_ran_gaussian_pdf(x - this.Mu, this.Sigma);
        }

        #endregion IDistribution
    }
}
