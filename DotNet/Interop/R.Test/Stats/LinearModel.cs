﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Common.App;
using MDo.Common.IO;
using MDo.Common.Numerics;

using MDo.Interop.R.Test;

namespace MDo.Interop.R.Stats.Test
{
    public class Interop_R_Stats_LinearModel : ConsoleAppModule
    {
        public void Execute(bool verbose = true, params string[] testFileSuffixes)
        {
            if (null == testFileSuffixes || testFileSuffixes.Length == 0)
                testFileSuffixes = new string[] { string.Empty };

            foreach (string testFileSuffix in testFileSuffixes)
            {
                string[] testFiles = TestUtils.GetDataFiles(StatsTestUtils.Namespace, string.Format("lm{0}", testFileSuffix));
                foreach (string testFile in testFiles)
                {
                    try
                    {
                        double[,] x; double[] y;
                        int numItems, numTrainingItems, numTestItems;
                        using (Stream input = File.OpenRead(testFile))
                        {
                            numItems = StatsTestUtils.Parse_LinearModelData(input, out x, out y);
                        }

                        if (numItems < 10)
                            numTrainingItems = numItems - 1;
                        else
                            numTrainingItems = (int)(0.8 * numItems);
                        numTestItems = numItems - numTrainingItems;

                        IList<int> samplingWithNoReplacement = new List<int>();
                        for (int i = 0; i < numItems; i++)
                            samplingWithNoReplacement.Add(i);

                        int numFeatures = x.GetLength(1);
                        double[,] training_X = new double[numTrainingItems, numFeatures], test_X = new double[numTestItems, numFeatures];
                        double[] training_Y = new double[numTrainingItems], test_Y = new double[numTestItems];

                        for (int i = 0; i < numTrainingItems; i++)
                        {
                            int sIndx = TestUtils.RNG.Int32(0, samplingWithNoReplacement.Count);
                            int indx = samplingWithNoReplacement[sIndx];
                            samplingWithNoReplacement.RemoveAt(sIndx);
                            for (int j = 0; j < numFeatures; j++)
                            {
                                training_X[i, j] = x[indx, j];
                            }
                            training_Y[i] = y[indx];
                        }
                        for (int i = 0; i < numTestItems; i++)
                        {
                            int indx = samplingWithNoReplacement[i];
                            for (int j = 0; j < numFeatures; j++)
                            {
                                test_X[i, j] = x[indx, j];
                            }
                            test_Y[i] = y[indx];
                        }

                        Console.WriteLine("Generating linear model with {0} observations...", numTrainingItems);
                        IntPtr model = LinearModel.Generate(training_X, training_Y);
                        RInterop.Print(model);
                        Console.WriteLine();

                        Console.WriteLine("Fitting model over training set...");
                        double[] training_Y_Fit = Model.Predict(model, training_X);
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
                                    Console.Write(string.Format("{0:F3}\t", training_X[i, j]));
                                }
                                Console.WriteLine("{0:F4}\t{1:F4}\t{2:F4}", training_Y[i], training_Y_Fit[i], training_Y[i] - training_Y_Fit[i]);
                            }
                        }
                        Console.WriteLine();

                        Console.WriteLine("Computing training MSE...");
                        double[] training_mse = new double[numTrainingItems];
                        for (int i = 0; i < numTrainingItems; i++)
                            training_mse[i] = Operators.SquareDifference(training_Y_Fit[i], training_Y[i]);
                        double training_meanSquaredError = Sequence.Mean(training_mse);
                        Console.WriteLine("\tTraining MSE = {0:F4}", training_meanSquaredError);
                        Console.WriteLine();

                        Console.WriteLine("Predicting holdout dataset with {0} items...", numTestItems);
                        double[] test_Y_Fit = Model.Predict(model, test_X);
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
                                    Console.Write(string.Format("{0:F3}\t", test_X[i, j]));
                                }
                                Console.WriteLine("{0:F4}\t{1:F4}\t{2:F4}", test_Y[i], test_Y_Fit[i], test_Y[i] - test_Y_Fit[i]);
                            }
                        }
                        Console.WriteLine();

                        Console.WriteLine("Computing test MSE...");
                        double[] test_mse = new double[numTestItems];
                        for (int i = 0; i < numTestItems; i++)
                            test_mse[i] = Operators.SquareDifference(test_Y_Fit[i], test_Y[i]);
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

        public override void Run(string[] args)
        {
            if (null == args || args.Length == 0)
                Execute(true, string.Empty);
            else
                Execute(true, args);
        }

        public override void PrintUsage()
        {
            Console.WriteLine("{0}: [OPT:DataFileSuffixes]", this.Name);
        }
    }
}
