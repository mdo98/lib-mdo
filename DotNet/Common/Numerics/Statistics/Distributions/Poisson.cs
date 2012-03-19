using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public class Poisson : IDiscreteDistribution
    {
        public Poisson(double mu)
        {
            this.Mu = mu;
        }


        #region Fields & Properties

        private double _Mu;

        public double Mu
        {
            get
            {
                return _Mu;
            }
            private set
            {
                if (value <= 0.0)
                    throw new ArgumentOutOfRangeException("Mu");
                _Mu = value;
            }
        }

        #endregion Fields & Properties


        #region Imports

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_poisson_P(uint k, double mu);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_poisson_Q(uint k, double mu);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_ran_poisson_pdf(uint k, double mu);

        #endregion Imports


        #region IDistribution

        public double Mean
        {
            get { return this.Mu; }
        }

        public double Stdev
        {
            get { return Math.Sqrt(this.Variance); }
        }

        public double Variance
        {
            get { return this.Mu; }
        }

        public long Median
        {
            get { return (long)(this.Mu + 1.0/3.0 - 0.02/this.Mu); }
        }

        public long Mode
        {
            get { return (long)Math.Ceiling(this.Mu) - 1L; }
        }

        public double Cdf(long x)
        {
            if (x < 0L)
                throw new ArgumentOutOfRangeException("x");

            return gsl_cdf_poisson_P((uint)x, this.Mu);
        }

        public double Cdf_Q(long x)
        {
            if (x < 0L)
                throw new ArgumentOutOfRangeException("x");

            return gsl_cdf_poisson_Q((uint)x, this.Mu);
        }

        public double Pmf(long x)
        {
            if (x < 0L)
                throw new ArgumentOutOfRangeException("x");

            return gsl_ran_poisson_pdf((uint)x, this.Mu);
        }

        #endregion IDistribution
    }
}
