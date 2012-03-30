using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Stats
{
    public static class LinearModel
    {
        public static IntPtr Generate(double[,] observed_X, double[] observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            if (null == observed_Y)
                throw new ArgumentNullException("observed_Y");

            int numObserved = observed_X.GetLength(0);
            int numFeatures = observed_X.GetLength(1);

            if (numObserved == 0)
                throw new ArgumentOutOfRangeException("observed_X.Count");

            if (numFeatures == 0)
                throw new ArgumentOutOfRangeException("observed_X.NumDims");

            if (observed_Y.Length != numObserved)
                throw new ArgumentOutOfRangeException("observed_Y.Count");

            StringBuilder expr = new StringBuilder();
            
            #region Declare data frame

            /* data = data.frame(
             * X1 = c(0, 1, 2, 3),
             * X2 = c(4, 5, 6, 7),
             * Y = c(8, 9, 10, 11))
             */

            expr.Append("data.frame(");
            for (int j = 0; j < numFeatures; j++)
            {
                expr.AppendFormat("X{0} = c(", j);
                for (int i = 0; i < numObserved; i++)
                {
                    expr.Append(observed_X[i, j]);
                    if (i < numObserved - 1)
                        expr.Append(", ");
                    else
                        expr.Append("), ");
                }
            }
            expr.Append("Y = c(");
            for (int i = 0; i < numObserved; i++)
            {
                expr.Append(observed_Y[i]);
                if (i < numObserved - 1)
                    expr.Append(", ");
                else
                    expr.Append("))");
            }

            RInterop.SetPrivateVariable("data", expr.ToString());

            #endregion Declare data frame

            expr.Clear();

            #region Fit model

            /* lm(Y ~ X1+X2,
             * data = data)
             */

            expr.Append("lm(Y ~ ");
            for (int j = 0; j < numFeatures; j++)
            {
                expr.AppendFormat("X{0}", j);
                if (j < numFeatures - 1)
                    expr.Append("+");
                else
                    expr.Append(", ");
            }
            expr.AppendFormat("data = {0})", RInterop.MakePrivateVariable("data"));

            IntPtr lm = RInterop.InternalEval(expr.ToString());

            #endregion Fit model

            return lm;
        }

        public static double[] GenerateAndPredict(double[,] observed_X, double[] observed_Y, double[,] unknown_X)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numFeatures = observed_X.GetLength(1);

            if (numFeatures == 0)
                throw new ArgumentOutOfRangeException("observed_X.NumDims");

            if (unknown_X.GetLength(1) != numFeatures)
                throw new ArgumentOutOfRangeException("unknown_X.NumDims");

            IntPtr lm = Generate(observed_X, observed_Y);
            return Model.Predict(lm, unknown_X);
        }
    }
}
