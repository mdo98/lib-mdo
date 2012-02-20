using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.Numerics.Statistics.Distributions;

namespace MDo.Common.Numerics.Statistics.Test
{
    public static partial class StatisticsTest
    {
        public static void KolmogorovSmirnov_CdfOneSided_KnuthAsymptoteFormula()
        {
            double[] p_Precomp = { 0.01, 0.05, 0.25, 0.50, 0.75, 0.95, 0.99 };
            Action<KolmogorovSmirnov, double, double> computeWithAsympFormulaAndWriteResult = (KolmogorovSmirnov ksDist, double ks_sqrtN, double p_Expected) =>
            {
                Console.WriteLine("\t\tks*sqrt(N) = {0}", ks_sqrtN);
                double p = ksDist.CdfOneSided_Asymptotic_Knuth(ks_sqrtN / Math.Sqrt(ksDist.N));
                double dp = p - p_Expected;
                Console.WriteLine("\t\t\tExpect p = {0:F8}", p_Expected);
                Console.WriteLine("\t\t\tActual p = {0:F8}", p);
                Console.WriteLine("\t\t\tDelta dp = {0:F8} ({1:F6} %)", dp, 100.0 * Math.Abs(dp) / Math.Min(p_Expected, 1.0 - p_Expected));
            };

            {
                IDictionary<int, double[]> ks_Precomp = new SortedList<int, double[]>()
                {
                    { 1,    new double[]    {0.01000,   0.05000,    0.2500,     0.5000,     0.7500,     0.9500,     0.9900}},
                    { 2,    new double[]    {0.01400,   0.06749,    0.2929,     0.5176,     0.7071,     1.0980,     1.2728}},
                    { 3,    new double[]    {0.01699,   0.07919,    0.3112,     0.5147,     0.7539,     1.1017,     1.3589}},
                    { 4,    new double[]    {0.01943,   0.08789,    0.3202,     0.5110,     0.7642,     1.1304,     1.3777}},
                    { 5,    new double[]    {0.02152,   0.09471,    0.3249,     0.5245,     0.7674,     1.1392,     1.4024}},
                    { 6,    new double[]    {0.02336,   0.1002,     0.3272,     0.5319,     0.7703,     1.1463,     1.4144}},
                    { 7,    new double[]    {0.02501,   0.1048,     0.3280,     0.5364,     0.7755,     1.1537,     1.4246}},
                    { 8,    new double[]    {0.02650,   0.1086,     0.3280,     0.5392,     0.7797,     1.1586,     1.4327}},
                    { 9,    new double[]    {0.02786,   0.1119,     0.3274,     0.5411,     0.7825,     1.1624,     1.4388}},
                    {10,    new double[]    {0.02912,   0.1147,     0.3297,     0.5426,     0.7845,     1.1658,     1.4440}},
                    {11,    new double[]    {0.03028,   0.1172,     0.3330,     0.5439,     0.7863,     1.1688,     1.4484}},
                    {12,    new double[]    {0.03137,   0.1193,     0.3357,     0.5453,     0.7880,     1.1714,     1.4521}},
                    {15,    new double[]    {0.03424,   0.1244,     0.3412,     0.5500,     0.7926,     1.1773,     1.4606}},
                    {20,    new double[]    {0.03807,   0.1298,     0.3461,     0.5547,     0.7975,     1.1839,     1.4698}},
                    {30,    new double[]    {0.04354,   0.1351,     0.3509,     0.5605,     0.8036,     1.1916,     1.4801}},
                };
                Console.WriteLine("Testing Knuth's Kolmogorov-Smirnov asymptote CDF formula with precomputed ks values for small sample size N.");
                foreach (KeyValuePair<int, double[]> ks in ks_Precomp)
                {
                    Console.WriteLine("\tN = {0}:", ks.Key);
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(ks.Key);
                    for (int i = 0; i < p_Precomp.Length; i++)
                    {
                        computeWithAsympFormulaAndWriteResult(ksDist, ks.Value[i], p_Precomp[i]);
                    }
                }
                Console.WriteLine();
            }

            {
                double[] ks_Asymp = new double[p_Precomp.Length];
                for (int i = 0; i < p_Precomp.Length; i++)
                {
                    ks_Asymp[i] = Math.Sqrt(0.5 * (0.0 - Math.Log(1.0 - p_Precomp[i])));
                }
                Console.WriteLine("Computing p-values with Knuth's Kolmogorov-Smirnov asymptote CDF formula, asymptotic & estimated ks values as N => Inf, and sample sizes N >= 30.");
                foreach (int N in new int[] { 30, 100, 300, 1000, 3000, 10000, 1000000, 100000000 })
                {
                    Console.WriteLine("\tN = {0}:", N);
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(N);
                    for (int i = 0; i < ks_Asymp.Length; i++)
                    {
                        double ks = ks_Asymp[i];
                        computeWithAsympFormulaAndWriteResult(ksDist, ks, p_Precomp[i]);
                    }
                    for (int i = 0; i < ks_Asymp.Length; i++)
                    {
                        double ks = ks_Asymp[i] - 1.0 / (6.0 * Math.Sqrt(N));
                        computeWithAsympFormulaAndWriteResult(ksDist, ks, p_Precomp[i]);
                    }
                }
                Console.WriteLine();
            }
        }

        public static void KolmogorovSmirnov_Cdf_DurbinMatrix()
        {
            Action<KolmogorovSmirnov, double, double, bool> computeWithDurbinMatrixAndWriteResult = (KolmogorovSmirnov ksDist, double ks, double p_Expected, bool time) =>
            {
                Console.WriteLine("\t\tks = {0}", ks);
                DateTime start = DateTime.Now;
                double p = ksDist.Cdf_DurbinMatrix(ks, true);
                if (time)
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    Console.WriteLine("\t\t\tElased: {0} seconds.", elapsed.TotalSeconds);
                }
                double dp = p - p_Expected;
                Console.WriteLine("\t\t\tActual p = {0:F8}", p);
                if (!double.IsNaN(p_Expected))
                {
                    Console.WriteLine("\t\t\tExpect p = {0:F8}", p_Expected);
                    Console.WriteLine("\t\t\tDelta dp = {0:F8} ({1:F6} %)", dp, 100.0 * Math.Abs(dp) / Math.Min(p_Expected, 1.0 - p_Expected));
                }
            };
            
            {
                int[] N =
                {
                    2000,
                    16000,
                };
                double[][] KS = 
                {
                    new double[] { 0.04, 0.06 },
                    new double[] { 0.016 },
                };
                double[][] p_Expected =
                {
                    new double[] { 0.99676943191713676985, 0.99999893956930568118 },
                    new double[] { 0.99945234913828052085 },
                };
                Console.WriteLine("Testing Durbin matrix algorithm for computing Kolmogorov-Smirnov asymptote CDF formula.");
                for (int n = 0; n < N.Length; n++)
                {
                    Console.WriteLine("\tN = {0}:", N[n]);
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(N[n]);
                    for (int k = 0; k < KS[n].Length; k++)
                    {
                        computeWithDurbinMatrixAndWriteResult(ksDist, KS[n][k], p_Expected[n][k], false);
                    }
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("Timing Durbin matrix algorithm with ks between -3 and +6 stdev and varying sample size.");
            foreach (int N in new int[] { 1 << 12, 1 << 14, 1 << 15, 1 << 16 })
            {
                Console.WriteLine("\tN = {0}:", N);
                KolmogorovSmirnov ksDist = new KolmogorovSmirnov(N);
                foreach (double deviation in new double[] { -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 })
                {
                    Console.WriteLine("\t\tDeviation = {0}", deviation);
                    double ks = Math.Max(0.0, ksDist.Mean + deviation * ksDist.Stdev);
                    computeWithDurbinMatrixAndWriteResult(ksDist, ks, double.NaN, true);
                }
            }
            Console.WriteLine();
            
            {
                int[] N = { 11000, 21000, 21001, 42001, 62000 };
                double[] ks = { 0.0004135, 0.0005, 0.0005, 0.00024, 0.004 };
                Console.WriteLine("Testing Simard & L'Ecuyer's claims of singularity.");
                for (int i = 0; i < N.Length; i++)
                {
                    Console.WriteLine("\tN = {0}:", N);
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(N[i]);
                    computeWithDurbinMatrixAndWriteResult(ksDist, ks[i], double.NaN, false);
                }
                Console.WriteLine();
            }
        }

        public static void KolmogorovSmirnov_Cdf()
        {
            Action<KolmogorovSmirnov, double, double> computeAndWriteResult = (KolmogorovSmirnov ksDist, double ks, double p_Expected) =>
            {
                Console.WriteLine("\t\tks = {0}", ks);
                DateTime start = DateTime.Now;
                double p = ksDist.Cdf(ks);
                TimeSpan elapsed = DateTime.Now - start;
                Console.WriteLine("\t\t\tElased: {0} seconds.", elapsed.TotalSeconds);
                double dp = p - p_Expected;
                Console.WriteLine("\t\t\tActual p = {0:F8}", p);
                if (!double.IsNaN(p_Expected))
                {
                    Console.WriteLine("\t\t\tExpect p = {0:F8}", p_Expected);
                    Console.WriteLine("\t\t\tDelta dp = {0:F8} ({1:F6} %)", dp, 100.0 * Math.Abs(dp) / Math.Min(p_Expected, 1.0 - p_Expected));
                }
            };

            {
                double[] deviations = { -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 10.0 };
                double[] p_Expected = new double[deviations.Length];
                {
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(KolmogorovSmirnov.CDF_DURBIN_N_CRITICAL);
                    for (int i = 0; i < deviations.Length; i++)
                    {
                        double ks = Math.Max(0.0, ksDist.Mean + deviations[i] * ksDist.Stdev);
                        p_Expected[i] = ksDist.Cdf_DurbinMatrix(ks, deviations[i] <= 6.0);
                    }
                }
                Console.WriteLine("Kolmogorov-Smirnov CDF with ks between -3 and +10 stdev and varying sample size.");
                foreach (int N in new int[] { 1 << 2, 1 << 4, 1 << 6, 1 << 8, 1 << 10, 1 << 12, 1 << 15, 1 << 18, 1 << 21, 1 << 24, 1 << 28 })
                {
                    Console.WriteLine("\tN = {0}:", N);
                    KolmogorovSmirnov ksDist = new KolmogorovSmirnov(N);
                    for (int i = 0; i < deviations.Length; i++)
                    {
                        Console.WriteLine("\t\tDeviation = {0}", deviations[i]);
                        double ks = Math.Max(0.0, ksDist.Mean + deviations[i] * ksDist.Stdev);
                        computeAndWriteResult(ksDist, ks, (N < KolmogorovSmirnov.CDF_DURBIN_N_CRITICAL ? ksDist.Cdf_DurbinMatrix(ks, true) : p_Expected[i]));
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
