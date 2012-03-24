using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.Numerics.LinearAlgebra;

namespace MDo.Common.Numerics.Statistics.Distributions
{
    public class KolmogorovSmirnov : IContinuousDistribution
    {
        #region Constants

        internal const int CDF_DURBIN_N_CRITICAL = 1 << 15;  // 32768
        public const double MEAN_TIMES_SQRT_N = 0.868731160636159141830154582372182480567806271054; // sqrt(Pi/2) * ln(2)
        public const double VARIANCE_TIMES_N = 0.06777320396386507937807970294193058473142;         // Pi^2/12 - MEAN^2
        public const double STDEV_TIMES_SQRT_N = 0.26033287146241267428010314841696233;

        #endregion Constants


        public KolmogorovSmirnov(int numSamples)
        {
            this.N = numSamples;
        }


        #region Fields & Properties

        private int _N;

        public int N
        {
            get
            {
                return _N;
            }
            private set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("N");
                _N = value;
            }
        }

        #endregion Fields & Properties


        #region IDistribution

        public double Mean  { get { return MEAN_TIMES_SQRT_N / Math.Sqrt(this.N);   } }
        public double Stdev { get { return STDEV_TIMES_SQRT_N / Math.Sqrt(this.N);  } }
        public double Variance { get { return VARIANCE_TIMES_N / this.N; } }

        /// <summary>
        /// Calculates the approximate p-value of a value from the Kolmogorov-Smirnov distribution.
        /// </summary>
        /// <remarks>This computation uses the analytic formula from Knuth's The Art of Computer Programming,
        /// 3rd Ed, Vol 2, Section 3.3.1, p.58, with the expansion from Exercise 20 (p.561).</remarks>
        /// <param name="ks">The Kolmogorov-Smirnov statistic.</param>
        /// <param name="numSamples">The number of samples used to compute the Kolmogorov-Smirnov statistic.</param>
        /// <returns>An approximation of the p-value of the Kolmogorov-Smirnov statistic.</returns>
        public double CdfOneSided_Asymptotic_Knuth(double ks)
        {
            // Knuth: Pr(K_N <= ks) = 1 - exp(-2 * ks^2) * (1 - 2/3 * ks/sqrt(N) + (2/3 * ks^2 - 4/9 * ks^4)/N + O(1/sqrt(N^3)))
            const double twoThirds = 2.0 / 3.0;
            double KS = Math.Sqrt(this.N) * ks;
            double KS_Square = KS * KS;
            double twoThirds_KS_Square = twoThirds * KS_Square;
            return 1.0 - Math.Exp(-2.0 * KS_Square) * (1.0 - twoThirds * KS / Math.Sqrt(this.N) + (twoThirds_KS_Square * (1.0 - twoThirds_KS_Square)) / this.N);
        }

        /// <summary>
        /// Calculates the approximate p-value of a value from the Kolmogorov-Smirnov distribution.
        /// </summary>
        /// <remarks>This method uses the Durbin matrix algorithm as described by Marsaglia, Tsang, Wang in
        /// Evaluating Kolmogorov's Distribution. It is an exact computation, but takes O(N^3 lgN) time, and
        /// should be avoided when N is large.
        /// </remarks>
        /// <param name="ks">The Kolmogorov-Smirnov statistic.</param>
        /// <param name="numSamples">The number of samples used to compute the Kolmogorov-Smirnov statistic.</param>
        /// <returns>An approximation of the p-value of the Kolmogorov-Smirnov statistic.</returns>
        internal double Cdf_DurbinMatrix(double ks, bool exactRightTail = false)
        {
            double ks_N = (double)this.N * ks;
            double s = ks * ks_N;
            if (((!exactRightTail) && (s > 8.0 || (s > 4.0 && this.N > 99))) ||
                ((s - this.Mean > 6.0 * this.Stdev) && this.N > CDF_DURBIN_N_CRITICAL))
                return 1.0 - 2.0 * Math.Exp(-(2.000071 + 0.331 / Math.Sqrt(this.N) + 1.409 / this.N) * s);

            int k = (int)ks_N + 1;
            int m = 2 * k - 1;
            double h = (double)k - ks_N;
            double[,] H = new double[m, m];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (i - j + 1 < 0)
                        H[i, j] = 0.0;
                    else
                        H[i, j] = 1.0;
                }
            }
            for (int i = 0; i < m; i++)
            {
                H[i, 0] -= Math.Pow(h, i + 1);
                H[m - 1, i] -= Math.Pow(h, m - i);
            }
            double two_h_minus_one = 2.0 * h - 1.0;
            H[m - 1, 0] += (two_h_minus_one > 0.0 ? Math.Pow(two_h_minus_one, m) : 0);
            /*
            // Original, O(N^3)
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    if (i - j + 1 > 0)
                    {
                        for (int g = 2; g <= i - j + 1; g++)  // Divide by 1 has no effect
                            H[i, j] /= g;
                    }
                }
            }
            */
            // O(N^2)
            double gd = 1.0;
            for (int g = 2; g <= m; g++)  // Divide by 1 has no effect
            {
                gd /= g;
                for (int j = 0, i = j + g - 1; i < m /* j < m-1, by construction */; i++, j++)
                {
                    H[i, j] *= gd;
                }
            }
            double[,] HP; int eHP;
            const int eNorm = 100; const double eNormFactor = 1.0E-100;
            MatrixUtil.Power(H, 0, out HP, out eHP, this.N, eNorm, eNormFactor);

            s = HP[k - 1, k - 1];
            const double eNormInverse = 1.0 / eNormFactor;
            for (int i = 1; i <= this.N; i++)
            {
                s = s * (double)i / (double)this.N;
                if (s < eNormFactor)
                {
                    s *= eNormInverse;
                    eHP -= eNorm;
                }
            }
            s *= Math.Pow(10.0, eHP);
            return s;
        }

        
        #region CDF

        private const int CDF_N_EXACT = 500;
        private const int CDF_N_KOLMO = 100000;

        public double Cdf(double x)
        {
            if (x < 0.0)
                throw new ArgumentOutOfRangeException("x");

            // R. Simard & P. L'Ecuyer, "Computing the Two-Sided Kolmogorov-Smirnov Distribution", Journal of Statistical Software, Vol. 39, Issue 11, Mar 2011
            // http://www.jstatsoft.org/v39/i11/paper
            // http://www.iro.umontreal.ca/~simardr/ksdir/KolmogorovSmirnovDist.java
            double u = Cdf_Special(this.N, x);
            if (!double.IsNaN(u))
                return u;

            double w = this.N * x * x;
            if (this.N <= CDF_N_EXACT)
            {
                if (w < 0.754693)
                    return this.Cdf_DurbinMatrix(x);

                if (w < 4.0)
                    return this.Cdf_Pomeranz(x);

                return 1.0 - this.Cdf_Q(x);
            }

            if ((w * x * this.N <= 7.0) && (this.N <= CDF_N_KOLMO))
                return this.Cdf_DurbinMatrix(x);

            return this.Cdf_Pelz(x);
        }

        private static double Cdf_Special(int numSamples, double ks)
        {
            // The ks distribution is known exactly for these cases
            double ks_N = (double)numSamples * ks;

            // For nx^2 > 18, Fbar(n, ks) is smaller than 5E-16
            if ((ks_N * ks >= 18.0) || (ks >= 1.0))
                return 1.0;

            if (ks_N <= 0.5)
                return 0.0;

            if (numSamples == 1)
                return 2.0 * ks - 1.0;

            if (ks_N <= 1.0)
            {
                double t = 2.0 * ks_N - 1.0;
                if (numSamples <= CDF_N_EXACT)
                {
                    return RapFac(numSamples) * Math.Pow(t, numSamples);
                }
                else
                {
                    return Math.Exp(GetLnFactorial(numSamples) + numSamples * Math.Log(t / numSamples));
                }
            }

            if (ks >= 1.0 - 1.0 / numSamples)
            {
                return 1.0 - 2.0 * Math.Pow(1.0 - ks, numSamples);
            }

            return double.NaN;
        }

        public double Cdf_Q(double x)
        {
            double v = Cdf_Q_Special(this.N, x);
            if (!double.IsNaN(v))
                return v;

            double w = (double)this.N * x * x;
            if (this.N <= CDF_N_EXACT)
            {
                if (w < 4.0)
                    return 1.0 - this.Cdf(x);
                else
                    return 2.0 * this.Cdf_KSPlusBar_Upper(x);
            }

            if (w >= 2.65)
                return 2.0 * this.Cdf_KSPlusBar_Upper(x);

            return 1.0 - this.Cdf(x);
        }

        private static double Cdf_Q_Special(int numSamples, double ks)
        {
            double ks_N = (double)numSamples * ks;
            double w = ks_N * ks;

            if (w >= 370.0 || ks >= 1.0)
                return 0.0;

            if (w <= 0.0274 || ks_N <= 0.5)
                return 1.0;

            if (numSamples == 1)
                return 2.0 - 2.0 * ks;

            if (ks_N <= 1.0)
            {
                double t = 2.0 * ks_N - 1.0;
                if (numSamples <= CDF_N_EXACT)
                {
                    return 1.0 - RapFac(numSamples) * Math.Pow(t, (double)numSamples);
                }
                else
                {
                    return 1.0 - Math.Exp(GetLnFactorial(numSamples) + numSamples * Math.Log(t / numSamples));
                }
            }

            if (ks >= 1.0 - 1.0 / numSamples)
            {
                return 2.0 * Math.Pow(1.0 - ks, numSamples);
            }

            return double.NaN;
        }

        private double Cdf_KSPlusBar_Upper(double ks)
        {
            // Compute the probability of the complementary ks+ distribution in the upper tail
            // using Smirnov's stable formula
            if (this.N > 200000)
                return Cdf_KSPlusBar_Asymptotic(ks);

            int j_max = (int)((1.0 - ks) * this.N);
            // Avoid log(0) for j = j_max and q ~ 1.0
            if ((1.0 - ks - (double)j_max / this.N) <= 0.0)
                j_max--;

            int j_div;
            if (this.N > 3000)
                j_div = 2;
            else
                j_div = 3;

            int j = j_max / j_div + 1;
            double LogCom = GetLnFactorial(this.N) - GetLnFactorial(j) - GetLnFactorial(this.N - j);
            double LOGJMAX = LogCom;

            const double EPSILON = 1.0E-12;
            double q;
            double term;
            double t;
            double sum = 0.0;

            while (j <= j_max)
            {
                q = (double)j / this.N + ks;
                term = LogCom + (j - 1) * Math.Log(q) + (this.N - j) * Math.Log(1.0 - q);
                t = Math.Exp(term);
                sum += t;
                LogCom += Math.Log((double)(this.N - j) / (j + 1));
                if (t <= sum * EPSILON)
                    break;
                j++;
            }

            j = j_max / j_div;
            LogCom = LOGJMAX + Math.Log((double)(j + 1) / (this.N - j));

            while (j > 0)
            {
                q = (double)j / this.N + ks;
                term = LogCom + (j - 1) * Math.Log(q) + (this.N - j) * Math.Log(1.0 - q);
                t = Math.Exp(term);
                sum += t;
                LogCom += Math.Log((double)j / (this.N - j + 1));
                if (t <= sum * EPSILON)
                    break;
                j--;
            }

            sum *= ks;
            // add the term j = 0
            sum += Math.Exp(this.N * Math.Log(1.0 - ks));
            return sum;
        }

        private double Cdf_KSPlusBar_Asymptotic(double ks)
        {
            double t = (6.0 * ks * this.N + 1.0);
            double eighteenTimesNumSamples = 18.0 * this.N;
            double z = t * t / eighteenTimesNumSamples;
            double v = 1.0 - (2.0 * z * z - 4.0 * z - 1.0) / eighteenTimesNumSamples;
            if (v <= 0.0)
                return 0.0;
            v = v * Math.Exp(-z);
            if (v >= 1.0)
                return 1.0;
            else
                return v;
        }

        private double Cdf_Pomeranz(double ks)
        {
            const double EPSILON = 1.0E-15;
            const int ENO = 360;
            double RENO = Math.Pow(2.0, ENO);   // for renormalization of V
            int count_Norm;                     // counter: how many renormalizations
            int i, j, k, s;
            int r1, r2;                         // Indices i and i-1 for V[i][]
            int j_low, j_up, klow, k_up, k_up0;
            double w, sum, minsum;
            double t = (double)this.N * ks;
            double[] A = new double[2 * this.N + 3];
            double[] At_floor = new double[2 * this.N + 3];
            double[] At_ceil = new double[2 * this.N + 3];
            double[,] V = new double[2, this.N + 2];
            double[,] H = new double[4, this.N + 2];     // = pow(w, j) / Factorial(j)

            CalcFloorCeil(this.N, t, A, At_floor, At_ceil);

            for (j = 1; j <= this.N + 1; j++)
            {
                V[0, j] = V[1, j] = 0.0;
            }
            V[1, 1] = RENO;
            count_Norm = 1;

            // Precompute H[][] = (A[j] - A[j-1]^k / k!
            H[0, 0] = 1.0;
            w = 2.0 * A[2] / this.N;
            for (j = 1; j <= this.N + 1; j++)
            {
                H[0, j] = w * H[0, j - 1] / j;
            }
            H[1, 0] = 1;

            w = (1.0 - 2.0 * A[2]) / this.N;
            for (j = 1; j <= this.N + 1; j++)
            {
                H[1, j] = w * H[1, j - 1] / j;
            }

            H[2, 0] = 1;
            w = A[2] / this.N;
            for (j = 1; j <= this.N + 1; j++)
            {
                H[2, j] = w * H[2, j - 1] / j;
            }

            H[3, 0] = 1;
            for (j = 1; j <= this.N + 1; j++)
            {
                H[3, j] = 0;
            }

            r1 = 0;
            r2 = 1;
            for (i = 2; i <= 2 * this.N + 2; i++)
            {
                j_low = (int)(2 + At_floor[i]);
                if (j_low < 1)
                    j_low = 1;

                j_up = (int)(At_ceil[i]);
                if (j_up > this.N + 1)
                    j_up = this.N + 1;

                klow = (int)(2 + At_floor[i - 1]);
                if (klow < 1)
                    klow = 1;
                k_up0 = (int)(At_ceil[i - 1]);

                // Find to which case it corresponds
                w = (A[i] - A[i - 1]) / this.N;
                s = -1;
                for (j = 0; j < 4; j++)
                {
                    if (Math.Abs(w - H[j, 1]) <= EPSILON)
                    {
                        s = j;
                        break;
                    }
                }

                minsum = RENO;
                r1 = (r1 + 1) & 1; // i - 1
                r2 = (r2 + 1) & 1; // i

                for (j = j_low; j <= j_up; j++)
                {
                    k_up = k_up0;
                    if (k_up > j)
                        k_up = j;
                    sum = 0;
                    for (k = k_up; k >= klow; k--)
                        sum += V[r1, k] * H[s, j - k];
                    V[r2, j] = sum;
                    if (sum < minsum)
                        minsum = sum;
                }

                if (minsum < 1.0E-270)
                {
                    // V is too small: renormalize to avoid underflow of probabilities
                    for (j = j_low; j <= j_up; j++)
                        V[r2, j] *= RENO;
                    count_Norm++;   // keep track of log of RENO
                }
            }

            sum = V[r2, this.N + 1];
            w = GetLnFactorial(this.N) - count_Norm * ENO * Constants.LN_2 + Math.Log(sum);
            if (w >= 0.0)
                return 1.0;
            else
                return Math.Exp(w);
        }

        private static void CalcFloorCeil(
           int n,               // sample size
           double t,            // = nx
           double[] A,          // A_i
           double[] At_floor,   // floor (A_i - t)
           double[] At_ceil     // ceiling (A_i + t)
        )
        {
            // Precompute A_i, floors, and ceilings for limits of sums in the
            // Pomeranz algorithm
            int i;
            int ell = (int)t;       // floor (t)
            double z = t - ell;     // t - floor (t)
            double w = Math.Ceiling(t) - t;

            if (z > 0.5)
            {
                for (i = 2; i <= 2 * n + 2; i += 2)
                    At_floor[i] = i / 2 - 2 - ell;
                for (i = 1; i <= 2 * n + 2; i += 2)
                    At_floor[i] = i / 2 - 1 - ell;

                for (i = 2; i <= 2 * n + 2; i += 2)
                    At_ceil[i] = i / 2 + ell;
                for (i = 1; i <= 2 * n + 2; i += 2)
                    At_ceil[i] = i / 2 + 1 + ell;

            }
            else if (z > 0.0)
            {
                for (i = 1; i <= 2 * n + 2; i++)
                    At_floor[i] = i / 2 - 1 - ell;

                for (i = 2; i <= 2 * n + 2; i++)
                    At_ceil[i] = i / 2 + ell;
                At_ceil[1] = 1 + ell;

            }
            else  // z == 0
            {
                for (i = 2; i <= 2 * n + 2; i += 2)
                    At_floor[i] = i / 2 - 1 - ell;
                for (i = 1; i <= 2 * n + 2; i += 2)
                    At_floor[i] = i / 2 - ell;

                for (i = 2; i <= 2 * n + 2; i += 2)
                    At_ceil[i] = i / 2 - 1 + ell;
                for (i = 1; i <= 2 * n + 2; i += 2)
                    At_ceil[i] = i / 2 + ell;
            }

            if (w < z)
                z = w;
            A[0] = A[1] = 0;
            A[2] = z;
            A[3] = 1 - A[2];
            for (i = 4; i <= 2 * n + 1; i++)
                A[i] = A[i - 2] + 1;
            A[2 * n + 2] = n;
        }

        private double Cdf_Pelz(double x)
        {
            /* Approximating the Lower Tail-Areas of the Kolmogorov-Smirnov One-Sample
               Statistic,
               Wolfgang Pelz and I. J. Good,
               Journal of the Royal Statistical Society, Series B.
                   Vol. 38, No. 2 (1976), pp. 152-156
             */
            const int J_MAX = 20;
            const double EPSILON = 1.0E-10;
            double RACN = Math.Sqrt(this.N);
            double z = RACN * x;
            double z2 = z * z;
            double z4 = z2 * z2;
            double z6 = z4 * z2;
            double w = Constants.PI_SQUARE / (2.0 * z * z);
            double ti, term, tom;
            double sum;
            int j;

            term = 1;
            j = 0;
            sum = 0;
            while (j <= J_MAX && term > EPSILON * sum)
            {
                ti = j + 0.5;
                term = Math.Exp(-ti * ti * w);
                sum += term;
                j++;
            }
            sum *= Constants.SQRT_2PI / z;

            term = 1;
            tom = 0;
            j = 0;
            while (j <= J_MAX && Math.Abs(term) > EPSILON * Math.Abs(tom))
            {
                ti = j + 0.5;
                term = (Constants.PI_SQUARE * ti * ti - z2) * Math.Exp(-ti * ti * w);
                tom += term;
                j++;
            }
            sum += tom * Constants.SQRT_HALFPI / (RACN * 3.0 * z4);

            term = 1;
            tom = 0;
            j = 0;
            while (j <= J_MAX && Math.Abs(term) > EPSILON * Math.Abs(tom))
            {
                ti = j + 0.5;
                term = 6 * z6 + 2 * z4 + Constants.PI_SQUARE * (2 * z4 - 5 * z2) * ti * ti +
                       Constants.PI_QUADRUPLE * (1 - 2 * z2) * ti * ti * ti * ti;
                term *= Math.Exp(-ti * ti * w);
                tom += term;
                j++;
            }
            sum += tom * Constants.SQRT_HALFPI / (this.N * 36.0 * z * z6);

            term = 1;
            tom = 0;
            j = 1;
            while (j <= J_MAX && term > EPSILON * tom)
            {
                ti = j;
                term = Constants.PI_SQUARE * ti * ti * Math.Exp(-ti * ti * w);
                tom += term;
                j++;
            }
            sum -= tom * Constants.SQRT_HALFPI / (this.N * 18.0 * z * z2);

            term = 1;
            tom = 0;
            j = 0;
            while (j <= J_MAX && Math.Abs(term) > EPSILON * Math.Abs(tom))
            {
                ti = j + 0.5;
                ti = ti * ti;
                term = -30 * z6 - 90 * z6 * z2 + Constants.PI_SQUARE * (135 * z4 - 96 * z6) * ti +
                       Constants.PI_QUADRUPLE * (212 * z4 - 60 * z2) * ti * ti + Constants.PI_SQUARE * Constants.PI_QUADRUPLE * ti * ti * ti * (5 -
                             30 * z2);
                term *= Math.Exp(-ti * w);
                tom += term;
                j++;
            }
            sum += tom * Constants.SQRT_HALFPI / (RACN * this.N * 3240.0 * z4 * z6);

            term = 1;
            tom = 0;
            j = 1;
            while (j <= J_MAX && Math.Abs(term) > EPSILON * Math.Abs(tom))
            {
                ti = j * j;
                term = (3 * Constants.PI_SQUARE * ti * z2 - Constants.PI_QUADRUPLE * ti * ti) * Math.Exp(-ti * w);
                tom += term;
                j++;
            }
            sum += tom * Constants.SQRT_HALFPI / (RACN * this.N * 108.0 * z6);

            return sum;
        }

        private static readonly double[] PrecomputedLnFactorial =
        {
            0.0,
            0.0,
            0.6931471805599453,
            1.791759469228055,
            3.178053830347946,
            4.787491742782046,
            6.579251212010101,
            8.525161361065415,
            10.60460290274525,
            12.80182748008147,
            15.10441257307552,
            17.50230784587389,
            19.98721449566188,
            22.55216385312342,
            25.19122118273868,
            27.89927138384088,
            30.67186010608066,
            33.50507345013688,
            36.39544520803305,
            39.33988418719949,
            42.33561646075348,
            45.3801388984769,
            48.47118135183522,
            51.60667556776437,
            54.7847293981123,
            58.00360522298051,
            61.26170176100199,
            64.55753862700632,
            67.88974313718154,
            71.257038967168,
            74.65823634883016,
        };

        private static double GetLnFactorial(int n)
        {
            // Returns the natural logarithm of factorial n!
            if (n < PrecomputedLnFactorial.Length)
            {
                return PrecomputedLnFactorial[n];
            }
            else
            {
                double x = (double)(n + 1);
                double y = 1.0 / (x * x);
                double z = ((-(5.95238095238E-4 * y) + 7.936500793651E-4) * y -
                            2.7777777777778E-3) * y + 8.3333333333333E-2;
                z = ((x - 0.5) * Math.Log(x) - x) + 9.1893853320467E-1 + z / x;
                return z;
            }
        }

        /// <summary>
        /// Computes (n! / n^n).
        /// </summary>
        /// <param name="n">An integer n.</param>
        /// <returns>n factorial divided by n to the power of n.</returns>
        private static double RapFac(int n)
        {
            if (n <= 0)
                return Double.NaN;

            double q = 1.0 / (double)n;
            for (int i = 2; i <= n; i++)
            {
                q *= ((double)i / (double)n);
            }
            return q;
        }

        #endregion CDF

        #endregion IDistribution


        /// <summary>
        /// Calculates the Kolmogorov-Smirnov statistic , which measures how well a set of empirical values
        /// fits a reference distribution.
        /// </summary>
        /// <remarks>This computation uses the modified method outlined in Knuth's The Art of Computer
        /// Programming, 3rd Ed, Vol 2, Section 3.3.1, Exercise 23 (p.562), and is linear is linear in time
        /// and space requirements to the number of samples.</remarks>
        /// <param name="numSamples">The number of values to sample from the empirical distribution.</param>
        /// <param name="getSample">A function that returns values from the empirical distribution.</param>
        /// <param name="referenceCdf">A function that returns the cumulative probability of a given value,
        /// if the value came from the reference distribution.</param>
        /// <returns>A p-value in the range of (0.0 .. 1.0) from the CDF of the Kolmogorov-Smirnov statistic
        /// computed for values sampled from the empirical distribution against the reference distribution.
        /// The closer the p-value is to 0.0 or 1.0, the less likely the empirical distribution fits the
        /// reference distribution.</returns>
        public static double GoodnessOfFit(int numSamples, Func<double> getSample, Func<double, double> referenceCdf)
        {
            // Generate samples X, and divide the range of F_X into K >= numSamples bins.
            // Place each sample in a bin, and record the number of samples & the maximum
            // and minimum values of F_X in each bin.
            int K = numSamples;
            int[] count_k = new int[K];
            double[] max_k = new double[K], min_k = new double[K];

            for (int i = 0; i < K; i++)
            {
                /*
                // Always initialized to 0 in .NET
                count_k[i] = 0;
                max_k[i] = 0.0;
                */
                min_k[i] = 1.0;
            }

            for (int i = 0; i < numSamples; i++)
            {
                double s = getSample();
                double f = referenceCdf(s);
                int k = (int)((double)K * f);
                if (k == K)
                    k = K - 1;
                count_k[k]++;
                if (max_k[k] < f)
                    max_k[k] = f;
                if (min_k[k] > f)
                    min_k[k] = f;
            }

            // Process each bin to find max deviations of the empirical CDF from the theoretical CDF.
            int count = 0;
            double k_plus = 0.0, k_minus = 0.0;
            for (int i = 0; i < K; i++)
            {
                if (count_k[i] > 0)
                {
                    k_minus = Math.Max(k_minus, min_k[i] - (double)count / (double)numSamples);
                    count += count_k[i];
                    k_plus = Math.Max(k_plus, (double)count / (double)numSamples - max_k[i]);
                }
            }

            // Compute the Kolmogorov-Smirnov statistic, and return the p-value.
            double ks = Math.Max(k_plus, k_minus);
            return (new KolmogorovSmirnov(numSamples)).Cdf(ks);
        }
    }
}
