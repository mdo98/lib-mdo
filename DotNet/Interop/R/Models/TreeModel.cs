using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public class TreeModel : Model
    {
        static TreeModel()
        {
            RInterop.InternalEval("library(rpart)");
        }


        #region Constructors

        protected TreeModel(IntPtr ptr) : base(ptr) { }

        #endregion Constructors


        #region Model

        protected override void ReadParameters()
        {
            throw new NotImplementedException();
        }

        public override double[] PredictFromParameters(double[,] unknown_X)
        {
            throw new NotImplementedException();
        }

        #endregion Model


        public static TreeModel LinearModelForClassification(object[,] observed_X, object[] observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.GetLength(1);

            if (numFeatures == 0)
                throw new ArgumentOutOfRangeException("observed_X.NumDims");

            return Generate(LinearFormula(numFeatures), "class", observed_X, observed_Y);
        }

        public static TreeModel LinearModelForRegression(object[,] observed_X, object[] observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.GetLength(1);

            if (numFeatures == 0)
                throw new ArgumentOutOfRangeException("observed_X.NumDims");

            return Generate(LinearFormula(numFeatures), "anova", observed_X, observed_Y);
        }

        public static TreeModel Generate(string formula, string method, object[,] observed_X, object[] observed_Y)
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

            #region Set up data frame

            /* data = data.frame(
             * X0 = c(0, 1, 2, 3),
             * X1 = c(4, 5, 6, 7),
             * Y = c(8, 9, 10, 11))
             */

            DataFrame.FromVectors(RInterop.MakePrivateVariable("data"), numObserved, (int indx) =>
            {
                Vector vector = null;
                if (indx < numFeatures)
                {
                    object[] values = new object[numObserved];
                    for (int i = 0; i < numObserved; i++)
                    {
                        values[i] = observed_X[i, indx];
                    }
                    vector = new Vector("X" + indx, values);
                }
                else if (indx == numFeatures)
                {
                    vector = new Vector("Y", observed_Y);
                }
                return vector;
            });

            #endregion Set up data frame

            StringBuilder expr = new StringBuilder();

            #region Fit model

            /* rpart(formula, method = "method", data = data)
             */

            expr.AppendFormat("rpart({0}, method = \"{1}\", data = {2})", formula, method, RInterop.MakePrivateVariable("data"));

            IntPtr treem = RInterop.InternalEval(expr.ToString());

            #endregion Fit model

            return new TreeModel(treem);
        }
    }
}
