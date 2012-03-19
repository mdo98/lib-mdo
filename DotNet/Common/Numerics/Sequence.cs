using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public static class Sequence
    {
        public static TOut[] Cast<TIn, TOut>(this TIn[] seq)
            where TIn   : struct, IConvertible
            where TOut  : struct, IConvertible
        {
            return seq.Select(item => (TOut)Convert.ChangeType(item, typeof(TOut))).ToArray();
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

        public static double Sum(params int[] seq)
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

        public static double Sum(params long[] seq)
        {
            return Sum(seq as IEnumerable<long>);
        }
    }
}
