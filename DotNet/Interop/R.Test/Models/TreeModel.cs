using System;
using System.Collections.Generic;
using System.Numerics;
using System.Numerics.Random;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;
using MDo.Interop.R.Test;
using MDo.Interop.R.Test.DataGeneration;

namespace MDo.Interop.R.Models.Test
{
    public class Interop_R_Models_TreeModel : ConsoleAppModule
    {
        public static void Run(
            Func<RVector, RVector, TreeModel> getModel,
            Func<RVector, RVector, bool[]> getPredictionErrors,
            Action<RVector, RVector, RVector, bool[]> printData,
            bool verbose = true,
            int numIterations = 100,
            double noise = 0.05,
            int numFeatures = 4,
            int numTraining = 200,
            int numTest = 100)
        {
            if (numIterations <= 0)
                throw new ArgumentOutOfRangeException("numIterations");

            if (noise < 0.0 || noise > 1.0)
                throw new ArgumentOutOfRangeException("noise");

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("numFeatures");

            if (numTraining <= 0)
                throw new ArgumentOutOfRangeException("numTraining");

            if (numTest <= 0)
                throw new ArgumentOutOfRangeException("numTest");

            RandomNumberGenerator rng = new MT19937Rng(TestUtils.RNG.UInt64());
            ClassificationDataGenerator classDataGen = new ClassificationDataGenerator(rng);

            double[] errorRates_train = new double[numIterations],
                     errorRates_test  = new double[numIterations];
            for (int n = 0; n < numIterations; n++)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("Iteration: {0}", n);
                Console.WriteLine();

                double[] cof = new double[numFeatures];
                for (int j = 0; j < numFeatures; j++)
                {
                    cof[j] = TestUtils.RNG.Double();
                }
                LinearParameters param = new LinearParameters()
                {
                    Coefficients = cof,
                    Noise = noise,
                };
                Console.WriteLine("Generating linear two-classifiable data, with {0} training items and {1} test items...", numTraining, numTest);
                Console.WriteLine("\tCoefficients:");
                for (int i = 0; i < param.Coefficients.Length; i++)
                {
                    Console.WriteLine("\t\t{0}", param.Coefficients[i]);
                }
                Console.WriteLine("\tNoise: {0}", param.Noise);
                ClassificationData data = classDataGen.GetLinearTwoClassifiable(param, numTraining, numTest);
                RVector training_X = new RVector(data.Training_X),
                        training_Y = new RVector(data.Training_Y),
                        test_X     = new RVector(data.Test_X),
                        test_Y     = new RVector(data.Test_Y);
                Console.WriteLine();

                Console.WriteLine("Generating decision tree with linear classifiable assumption...");
                TreeModel model = null;
                TimeSpan elapsed = TestUtils.Time(() => model = getModel(training_X, training_Y));
                Console.WriteLine("\tTime: {0:F3} seconds.", elapsed.TotalSeconds);

                RInterop.Print(model.Ptr);
                Console.WriteLine();

                int errorCount;

                Console.WriteLine("Fitting model over training set...");
                RVector training_Y_Fit = model.Predict(training_X);
                bool[] training_errors = getPredictionErrors(training_Y, training_Y_Fit);
                if (verbose)
                {
                    printData(training_X, training_Y, training_Y_Fit, training_errors);
                }
                errorCount = training_errors.Count(item => item);
                errorRates_train[n] = (double)errorCount/numTraining;
                Console.WriteLine("\t# ERRORS = {0} ({1:P})", errorCount, errorRates_train[n]);
                Console.WriteLine();

                Console.WriteLine("Fitting model over test set...");
                RVector test_Y_Fit = model.Predict(test_X);
                bool[] test_errors = getPredictionErrors(test_Y, test_Y_Fit);
                if (verbose)
                {
                    printData(test_X, test_Y, test_Y_Fit, test_errors);
                }
                errorCount = test_errors.Count(item => item);
                errorRates_test[n] = (double)errorCount/numTest;
                Console.WriteLine("\t# ERRORS = {0} ({1:P})", errorCount, errorRates_test[n]);
                Console.WriteLine();
            }
            Console.WriteLine("========================================");
            Console.WriteLine("Avg train error = {0:P}", Sequence.Mean(errorRates_train));
            Console.WriteLine("Avg test error  = {0:P}", Sequence.Mean(errorRates_test));
            Console.WriteLine();
        }

        public virtual TreeModel GetModel(RVector observed_X, RVector observed_Y)
        {
            return TreeModel.LinearModelForClassification(observed_X, observed_Y);
        }

        public virtual bool[] GetPredictionErrors(RVector y_actual, RVector y_Fit)
        {
            int numObserved = y_actual.NumRows;
            bool[] y_errors = new bool[numObserved];

            int falseCol = y_Fit.ColNames.IndexOf(false.ToString()),
                trueCol  = y_Fit.ColNames.IndexOf(true.ToString());

            for (int i = 0; i < numObserved; i++)
            {
                y_errors[i] = ((bool)y_actual.Values[i,0]
                    // Resolve equal-prob predictions to false
                    ? ((double)y_Fit.Values[i,falseCol] >= (double)y_Fit.Values[i,trueCol])
                    : ((double)y_Fit.Values[i,falseCol] <  (double)y_Fit.Values[i,trueCol]));
            }
            return y_errors;
        }

        public virtual void PrintData(RVector x, RVector y_actual, RVector y_Fit, bool[] test_errors)
        {
            int numFeatures = x.NumCols, numObserved = x.NumRows;
            for (int j = 0; j < numFeatures; j++)
            {
                Console.Write(string.Format("X{0}\t", j));
            }
            Console.WriteLine("Y_actual\tY_Fit_False\tY_Fit_True\tERROR");

            int falseCol = y_Fit.ColNames.IndexOf(false.ToString()),
                trueCol  = y_Fit.ColNames.IndexOf(true.ToString());
            for (int i = 0; i < numObserved; i++)
            {
                for (int j = 0; j < numFeatures; j++)
                {
                    Console.Write(string.Format("{0:F3}\t", x.Values[i, j]));
                }
                Console.WriteLine("{0}\t{1:F4}\t{2:F4}\t{3}", y_actual.Values[i, 0], y_Fit.Values[i, falseCol], y_Fit.Values[i, trueCol], test_errors[i] ? "X" : string.Empty);
            }
        }


        #region ConsoleAppModule

        public override int Run(string[] args)
        {
            if (null == args || args.Length == 0)
            {
                Run(this.GetModel,
                    this.GetPredictionErrors,
                    this.PrintData);
            }
            else
            {
                int numIterations = 100,
                    numFeatures = 4,
                    numTraining = 200,
                    numTest = 100;
                double noise = 0.05;
                foreach (string arg in args)
                {
                    string[] kv = arg.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2)
                        throw new ArgumentException(string.Format(
                            "Invalid argument: {0}",
                            arg));

                    switch (kv[0].Trim().ToUpperInvariant())
                    {
                        case "ITER":
                            numIterations = int.Parse(kv[1].Trim());
                            break;

                        case "DIM":
                            numFeatures = int.Parse(kv[1].Trim());
                            break;

                        case "TRAIN":
                            numTraining = int.Parse(kv[1].Trim());
                            break;

                        case "TEST":
                            numTest = int.Parse(kv[1].Trim());
                            break;

                        case "NOISE":
                            noise = double.Parse(kv[1].Trim());
                            break;

                        default:
                            throw new ArgumentException(string.Format(
                                "Switch {0} not recognized.",
                                kv[0]));
                    }
                }
                Run(this.GetModel,
                    this.GetPredictionErrors,
                    this.PrintData,
                    true,
                    numIterations,
                    noise,
                    numFeatures,
                    numTraining,
                    numTest);
            }
            return (int)ReturnCode.Normal;
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [OPT: ITER=(numIterations)] [OPT: DIM=(numFeatures)] [OPT: TRAIN=(numTraining)] [OPT: TEST=(numTest)] [OPT: NOISE=(noise)]", this.Name);
        }

        #endregion ConsoleAppModule
    }
}
