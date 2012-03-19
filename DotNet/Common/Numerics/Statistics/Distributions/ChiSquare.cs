using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.Statistics.Distributions
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

        #endregion IDistribution


        public static double GoodnessOfFit(long[] samples, IDiscreteDistribution refDist)
        {
            const int minSamples = 30;

            int numSamples = samples.Length;
            if (numSamples < minSamples)
                throw new ArgumentException("samples");

            int minExpInBin = numSamples >> 7;

            SortedList<long, int> exp = new SortedList<long, int>();
            // Compute the expected count in each bin
            Action<long, Func<long, long>> computeExp = (long x0, Func<long, long> xIncr) =>
            {
                for (long k = x0; ; k = xIncr(k))
                {
                    int exp_k = (int)Math.Round(refDist.Pmf(k) * numSamples, MidpointRounding.ToEven);
                    if (exp_k < minExpInBin)
                        break;
                    exp.Add(k, exp_k);
                }
            };
            computeExp(refDist.Mode - 1L, (x) => (x - 1));
            computeExp(refDist.Mode     , (x) => (x + 1));
            if (exp.Count < 2)
                throw new InvalidOperationException("CHISQ_FIT_INSUFFICIENT_DATA");

            Array.Sort(samples);
            SortedList<long, int> obs = new SortedList<long, int>();
            // Compute the observed count in each bin
            for (int seq_Start = 0; seq_Start < samples.Length; )
            {
                long binId = samples[seq_Start];
                int seq_End = seq_Start + 1;
                while (seq_End < samples.Length && samples[seq_End] == binId)
                    seq_End++;
                if (exp.ContainsKey(binId))
                    obs.Add(binId, seq_End - seq_Start);
                seq_Start = seq_End;
            }
            foreach (long binId in exp.Keys.Except(obs.Keys))
            {
                obs.Add(binId, 0);
            }

            double chisq = Sequence.Sum(exp.Select(e => (double)Operators.SquareDifference(obs.Single(o => o.Key == e.Key).Value, e.Value) / e.Value));
            return (new ChiSquare(exp.Count - 1).Cdf(chisq));
        }
    }
}
