using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.Numerics;
using MDo.Common.Numerics.Random;
using MDo.Common.Numerics.Statistics.Distributions;

namespace MDo.Interop.R.Test.DataGeneration
{
    public class ClassificationDataGenerator
    {
        private const double Range = 100.0;
        private const double NumNoiseStdevInRange = 3.0;

        /// <remarks>
        /// 6/(erfc(-3/sqrt(2))-1)
        /// The purpose of the noise corrector is to compress the normal curve between -3sd to +3sd
        /// to a unit distance, while keeping the area under the curve equal to 1.0.
        /// </remarks>
        private const double NoiseCorrector = 6.0162426281631295976;

        /// <remarks>
        /// 1 / (6/(erfc(-3/sqrt(2))-1) * 1/sqrt(2*pi))
        /// Noise under the threshold will be normally distributed; noise above the threshold will have
        /// tails fattened by a linear combination of the normal distribution at the threshold and the
        /// uniform distribution, such that the distribution will be perfectly uniform when noise = 1.0.
        /// The purpose is to keep the definite integral of the distribution equal to the noise level.
        /// </remarks>
        private const double NoiseThreshold = 0.4166434815805158412;

        private static readonly Uniform NoiseDistributionWeight = new Uniform(NoiseThreshold, 1.0);
        private static readonly Normal  StandardNormal = Normal.Standard;

        private readonly RandomNumberGenerator RNG;

        public ClassificationDataGenerator(RandomNumberGenerator rng)
        {
            this.RNG = rng;
        }

        public ClassificationData GetLinearTwoClassifiable(LinearParameters param, int numTraining, int numTest)
        {
            int numFeatures;
            if (null == param.Coefficients || (numFeatures = param.Coefficients.Length) == 0)
                throw new ArgumentOutOfRangeException("param.Parameters.Length");

            if (numTraining <= 0)
                throw new ArgumentOutOfRangeException("numTraining");

            if (numTest <= 0)
                throw new ArgumentOutOfRangeException("numTest");

            // Values will range from 0.0 to 100.0
            // Intercept is irrelevant for simulated classification data
            double mu = 0.0, sigma = 0.0;
            for (int j = 0; j < numFeatures; j++)
            {
                double mu_j     = param.Coefficients[j]  * (0.5 * Range);
                double sigma_j  = mu_j / NumNoiseStdevInRange;
                mu      += mu_j;
                sigma   += (sigma_j * sigma_j); // Assuming features are pairwise independent
            }
            sigma = Math.Sqrt(sigma);
            Action<int, object[,], object[,]> generateSample = (int indx, object[,] features, object[,] labels) =>
            {
                double f = 0.0;
                for (int j = 0; j < numFeatures; j++)
                {
                    double fj = this.RNG.Double() * Range;
                    f += (fj * param.Coefficients[j]);
                    features[indx, j] = fj;
                }
                double noiseProb;
                if (param.Noise > NoiseThreshold)
                {
                    noiseProb
                        = NoiseDistributionWeight.Cdf_Q (param.Noise) * (NoiseThreshold * NoiseCorrector * StandardNormal.Pdf((f-mu)/sigma))
                        + NoiseDistributionWeight.Cdf   (param.Noise) * 1.0;
                }
                else
                {
                    noiseProb = param.Noise * NoiseCorrector * StandardNormal.Pdf((f-mu)/sigma);
                }
                bool v;
                if (this.RNG.Double() < noiseProb)
                    v = this.RNG.Bool();
                else
                    v = (f < mu);
                labels[indx, 0] = v;
            };

            ClassificationData data = new ClassificationData();
            data.Training_X = new object[numTraining, numFeatures];
            data.Training_Y = new object[numTraining, 1];
            data.Test_X     = new object[numTest, numFeatures];
            data.Test_Y     = new object[numTest, 1];

            for (int i = 0; i < numTraining; i++)
                generateSample(i, data.Training_X, data.Training_Y);

            for (int i = 0; i < numTest; i++)
                generateSample(i, data.Test_X, data.Test_Y);

            return data;
        }
    }
}
