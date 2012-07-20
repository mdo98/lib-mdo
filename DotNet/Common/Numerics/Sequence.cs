using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics
{
    public static class Sequence
    {
        public static TOut[] Cast<TIn, TOut>(this TIn[] seq)
            where TIn   : struct, IConvertible
            where TOut  : struct, IConvertible
        {
            return seq.Select(item => (TOut)Convert.ChangeType(item, typeof(TOut))).ToArray();
        }

        public static double Max(IEnumerable<double> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            double max = double.NegativeInfinity;
            foreach (double d in seq)
            {
                if (d > max)
                    max = d;
            }
            return max;
        }

        public static double Max(out int argMax, params double[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            double max = double.NegativeInfinity;
            argMax = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                double d = seq[i];
                if (d > max)
                {
                    max = d;
                    argMax = i;
                }
            }
            return max;
        }

        public static int Max(IEnumerable<int> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            int max = int.MinValue;
            foreach (int d in seq)
            {
                if (d > max)
                    max = d;
            }
            return max;
        }

        public static int Max(out int argMax, params int[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            int max = int.MinValue;
            argMax = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                int d = seq[i];
                if (d > max)
                {
                    max = d;
                    argMax = i;
                }
            }
            return max;
        }

        public static long Max(IEnumerable<long> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long max = long.MinValue;
            foreach (long d in seq)
            {
                if (d > max)
                    max = d;
            }
            return max;
        }

        public static long Max(out int argMax, params long[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long max = long.MinValue;
            argMax = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                long d = seq[i];
                if (d > max)
                {
                    max = d;
                    argMax = i;
                }
            }
            return max;
        }

        public static double Min(IEnumerable<double> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            double min = double.PositiveInfinity;
            foreach (double d in seq)
            {
                if (d < min)
                    min = d;
            }
            return min;
        }

        public static double Min(out int argMin, params double[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            double min = double.PositiveInfinity;
            argMin = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                double d = seq[i];
                if (d < min)
                {
                    min = d;
                    argMin = i;
                }
            }
            return min;
        }

        public static int Min(IEnumerable<int> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            int min = int.MaxValue;
            foreach (int d in seq)
            {
                if (d < min)
                    min = d;
            }
            return min;
        }

        public static int Min(out int argMin, params int[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            int min = int.MaxValue;
            argMin = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                int d = seq[i];
                if (d < min)
                {
                    min = d;
                    argMin = i;
                }
            }
            return min;
        }

        public static long Min(IEnumerable<long> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long min = long.MaxValue;
            foreach (long d in seq)
            {
                if (d < min)
                    min = d;
            }
            return min;
        }

        public static long Min(out int argMin, params long[] seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long min = long.MaxValue;
            argMin = -1;
            for (int i = 0; i < seq.Length; i++)
            {
                long d = seq[i];
                if (d < min)
                {
                    min = d;
                    argMin = i;
                }
            }
            return min;
        }

        public static double Mean(IEnumerable<double> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            return Sum(seq) / seq.Count();
        }

        public static double Mean(params double[] seq)
        {
            return Mean(seq as IEnumerable<double>);
        }

        public static double Mean(IEnumerable<int> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            return (double)Sum(seq) / seq.Count();
        }

        public static double Mean(params int[] seq)
        {
            return Mean(seq as IEnumerable<int>);
        }

        public static double Mean(IEnumerable<long> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            return (double)Sum(seq) / seq.Count();
        }

        public static double Mean(params long[] seq)
        {
            return Mean(seq as IEnumerable<long>);
        }

        public static double Sum(IEnumerable<double> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            double sum = 0.0;
            foreach (double d in seq)
                sum += d;
            return sum;
        }

        public static double Sum(params double[] seq)
        {
            return Sum(seq as IEnumerable<double>);
        }

        public static long Sum(IEnumerable<int> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long sum = 0L;
            foreach (int d in seq)
                sum += d;
            return sum;
        }

        public static long Sum(params int[] seq)
        {
            return Sum(seq as IEnumerable<int>);
        }

        public static long Sum(IEnumerable<long> seq)
        {
            if (null == seq)
                throw new ArgumentNullException("seq");

            long sum = 0L;
            foreach (long d in seq)
                sum += d;
            return sum;
        }

        public static long Sum(params long[] seq)
        {
            return Sum(seq as IEnumerable<long>);
        }
    }
}
