using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.Statistics.Distributions
{
    public class ChiSquare : IContinuousDistribution
    {
        public ChiSquare(int k)
        {
            this.K = k;
        }


        #region Fields & Properties

        private int _K;

        public int K
        {
            get
            {
                return _K;
            }
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("K");
                _K = value;
            }
        }

        #endregion Fields & Properties


        #region Imports

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_chisq_P(double x, double nu);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_cdf_chisq_Q(double x, double nu);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern double gsl_ran_chisq_pdf(double x, double nu);

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
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            if (x == 0.0)
                return 0.0;

            return gsl_cdf_chisq_P(x, this.K); 
        }

        public double Cdf_Q(double x)
        {
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            if (x == 0.0)
                return 0.0;

            return gsl_cdf_chisq_Q(x, this.K); 
        }

        public double Pdf(double x)
        {
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            if (x == 0.0)
                return 0.0;

            return gsl_ran_chisq_pdf(x, this.K);
        }

        #endregion IDistribution


        public static double GoodnessOfFit<T>(T[] observed, IDictionary<T, int> exp)
            where T : IEquatable<T>
        {
            if (exp.Count < 2)
                throw new InvalidOperationException("CHISQ_FIT_INSUFFICIENT_DATA");

            const int minSamples = 30;
            if (observed.Length < minSamples)
                throw new ArgumentException("samples");

            IDictionary<T, int> obs = new Dictionary<T, int>();
            // Compute the observed count in each bin
            foreach (T o in observed)
            {
                if (exp.ContainsKey(o))
                {
                    if (obs.ContainsKey(o))
                        obs[o] = obs[o] + 1;
                    else
                        obs.Add(o, 1);
                }
            }
            foreach (T binId in exp.Keys.Except(obs.Keys))
            {
                obs.Add(binId, 0);
            }

            double chisq = Sequence.Sum(exp.Select(e => (double)Operators.SquareDifference(obs.Single(o => o.Key.Equals(e.Key)).Value, e.Value) / e.Value));
            return (new ChiSquare(exp.Count - 1).Cdf(chisq));
        }
    }
}
