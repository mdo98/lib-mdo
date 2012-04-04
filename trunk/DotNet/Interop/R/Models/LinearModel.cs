using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public class LinearModel : Model
    {
        #region Constructors

        protected LinearModel(IntPtr ptr) : base(ptr) { }

        #endregion Constructors


        #region Properties

        public double[] Coefficients    { get; protected set; }
        public double   Intercept       { get; protected set; }

        #endregion Properties


        #region Model

        protected override void ReadParameters()
        {
            if (RInterop.RSEXPREC.FromPointer(this.ModelPtr).Header.SxpInfo.Type != RInterop.RSXPTYPE.VECSXP)
                throw new ArgumentException("this.ModelPtr");

            object[] modelParams = RInterop.RsxprPtrToClrValue(RInterop.RSEXPREC.VecSxp_GetElement(this.ModelPtr, 0));
            this.Intercept = (double)modelParams[0];
            this.Coefficients = new double[modelParams.Length - 1];
            for (int i = 1; i < modelParams.Length; i++)
            {
                double cof = (double)modelParams[i];
                this.Coefficients[i-1] = double.IsNaN(cof) ? 0.0 : cof;
            }
        }

        public override double[] PredictFromParameters(double[,] unknown_X)
        {
            int numObserved = unknown_X.GetLength(0);
            int numFeatures = unknown_X.GetLength(1);

            if (numFeatures != this.Coefficients.Length)
                throw new ArgumentOutOfRangeException("unknown_X.NumDims");

            double[] y = new double[numObserved];
            for (int i = 0; i < numObserved; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < numFeatures; j++)
                {
                    sum += (this.Coefficients[j] * unknown_X[i,j]);
                }
                y[i] = sum + this.Intercept;
            }
            return y;
        }

        #endregion Model

        public static LinearModel Generate(double[,] observed_X, double[] observed_Y)
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
                object[] values;
                if (indx < numFeatures)
                {
                    values = new object[numObserved];
                    for (int i = 0; i < numObserved; i++)
                    {
                        values[i] = observed_X[i, indx];
                    }
                    vector = new Vector("X" + indx, values);
                }
                else if (indx == numFeatures)
                {
                    values = new object[numObserved];
                    for (int i = 0; i < numObserved; i++)
                    {
                        values[i] = observed_Y[i];
                    }
                    vector = new Vector("Y", values);
                }
                return vector;
            });

            #endregion Set up data frame

            StringBuilder expr = new StringBuilder();

            #region Fit model

            /* lm(Y ~ X0+X1,
             * data = data)
             */

            expr.AppendFormat("lm({0}, data = {1})", LinearFormula(numFeatures), RInterop.MakePrivateVariable("data"));

            IntPtr lm = RInterop.InternalEval(expr.ToString());

            #endregion Fit model

            return new LinearModel(lm);
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

            LinearModel lm = Generate(observed_X, observed_Y);
            return lm.Predict(unknown_X);
        }
    }
}
