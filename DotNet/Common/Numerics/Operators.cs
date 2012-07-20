using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics
{
    public static class Operators
    {
        public static long Square(int x)
        {
            return ((long)x * (long)x);
        }

        public static long Square(long x)
        {
            return checked(x * x);
        }

        public static double Square(double x)
        {
            return (x * x);
        }

        public static long SquareDifference(int x1, int x2)
        {
            long dif = x1 - x2;
            return (dif * dif);
        }

        public static long SquareDifference(long x1, long x2)
        {
            long dif = x1 - x2;
            return checked(dif * dif);
        }

        public static double SquareDifference(double x1, double x2)
        {
            double dif = x1 - x2;
            return (dif * dif);
        }
    }
}
