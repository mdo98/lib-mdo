using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Numerics.Random;
using System.Text;

using MDo.Interop.R.Core;
using MDo.Interop.R.Test;

namespace MDo.Interop.R.Models.Test
{
    public class Interop_R_Models_LinearModel : ConsoleAppModule
    {
        public static void Execute(bool verbose = true, params string[] testFileSuffixes)
        {
            if (null == testFileSuffixes || testFileSuffixes.Length == 0)
                testFileSuffixes = new string[] { string.Empty };

            foreach (string testFileSuffix in testFileSuffixes)
            {
                string[] testFiles = TestUtils.GetDataFiles(ModelsTestUtils.Namespace, string.Format("lm{0}", testFileSuffix));
                foreach (string testFile in testFiles)
                {
                    try
                    {
                        Console.WriteLine("========================================");
                        Console.WriteLine("Data: {0}", testFile);
                        Console.WriteLine();

                        double[,] x; double[,] y;
                        int numItems, numTrainingItems, numTestItems;
                        using (Stream input = File.OpenRead(testFile))
                        {
                            numItems = ModelsTestUtils.Parse_LinearModelData(input, out x, out y);
                        }

                        if (numItems < 10)
                            numTrainingItems = numItems - 1;
                        else
                            numTrainingItems = (int)(0.8 * numItems);
                        numTestItems = numItems - numTrainingItems;

                        int numFeatures = x.GetLength(1);
                        RVector training_X = new RVector(new object[numTrainingItems, numFeatures]),
                                training_Y = new RVector(new object[numTrainingItems, 1]),
                                test_X     = new RVector(new object[numTestItems, numFeatures]),
                                test_Y     = new RVector(new object[numTestItems, 1]);

                        SamplerWithoutReplacement sampler = new SamplerWithoutReplacement(TestUtils.RNG, 0, numItems);
                        for (int i = 0; i < numTrainingItems; i++)
                        {
                            int indx = (int)sampler.Next();
                            for (int j = 0; j < numFeatures; j++)
                            {
                                training_X.Values[i, j] = x[indx, j];
                            }
                            training_Y.Values[i, 0] = y[indx, 0];
                        }
                        for (int i = 0; i < numTestItems; i++)
                        {
                            int indx = (int)sampler.Next();
                            for (int j = 0; j < numFeatures; j++)
                            {
                                test_X.Values[i, j] = x[indx, j];
                            }
                            test_Y.Values[i, 0] = y[indx, 0];
                        }

                        Console.WriteLine("Generating linear model with {0} observations...", numTrainingItems);
                        LinearModel model = null;
                        TimeSpan elapsed = TestUtils.Time(() => model = LinearModel.Generate(training_X, training_Y));
                        Console.WriteLine("\tTime: {0:F3} seconds.", elapsed.TotalSeconds);

                        RInterop.Print(model.Ptr);
                        Console.WriteLine();

                        Console.WriteLine("Fitting model over training set...");
                        RVector training_Y_Fit = model.Predict(training_X);
                        if (verbose)
                        {
                            for (int j = 0; j < numFeatures; j++)
                            {
                                Console.Write(string.Format("X{0}\t", j));
                            }
                            Console.WriteLine("Y_actual\tY_Fit\tDif(Y_actual,Y_Fit)");
                            for (int i = 0; i < numTrainingItems; i++)
                            {
                                for (int j = 0; j < numFeatures; j++)
                                {
                                    Console.Write(string.Format("{0:F3}\t", training_X.Values[i, j]));
                                }
                                Console.WriteLine("{0:F4}\t{1:F4}\t{2:F4}", training_Y.Values[i, 0], training_Y_Fit.Values[i, 0], (double)training_Y.Values[i, 0] - (double)training_Y_Fit.Values[i, 0]);
                            }
                        }
                        Console.WriteLine();

                        Console.WriteLine("Computing training MSE...");
                        double[] training_mse = new double[numTrainingItems];
                        for (int i = 0; i < numTrainingItems; i++)
                            training_mse[i] = Operators.SquareDifference((double)training_Y_Fit.Values[i, 0], (double)training_Y.Values[i, 0]);
                        double training_meanSquaredError = Sequence.Mean(training_mse);
                        Console.WriteLine("\tTraining MSE = {0:F4}", training_meanSquaredError);
                        Console.WriteLine();

                        Console.WriteLine("Predicting holdout dataset with {0} items...", numTestItems);
                        RVector test_Y_Fit = model.Predict(test_X);
                        if (verbose)
                        {
                            for (int j = 0; j < numFeatures; j++)
                            {
                                Console.Write(string.Format("X{0}\t", j));
                            }
                            Console.WriteLine("Y_actual\tY_Fit\tDif(Y_actual,Y_Fit)");
                            for (int i = 0; i < numTestItems; i++)
                            {
                                for (int j = 0; j < numFeatures; j++)
                                {
                                    Console.Write(string.Format("{0:F3}\t", test_X.Values[i, j]));
                                }
                                Console.WriteLine("{0:F4}\t{1:F4}\t{2:F4}", test_Y.Values[i, 0], test_Y_Fit.Values[i, 0], (double)test_Y.Values[i, 0] - (double)test_Y_Fit.Values[i, 0]);
                            }
                        }
                        Console.WriteLine();

                        Console.WriteLine("Computing test MSE...");
                        double[] test_mse = new double[numTestItems];
                        for (int i = 0; i < numTestItems; i++)
                            test_mse[i] = Operators.SquareDifference((double)test_Y_Fit.Values[i, 0], (double)test_Y.Values[i, 0]);
                        double test_meanSquaredError = Sequence.Mean(test_mse);
                        Console.WriteLine("\tTest MSE = {0:F4}", test_meanSquaredError);
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error while processing {0}: {1}", testFile, ex.ToString());
                    }
                }
            }
        }

        public override int Run(string[] args)
        {
            if (null == args || args.Length == 0)
                Execute(true, string.Empty);
            else
                Execute(true, args);
            return (int)ReturnCode.Normal;
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [OPT:DataFileSuffixes]", this.Name);
        }
    }
}
