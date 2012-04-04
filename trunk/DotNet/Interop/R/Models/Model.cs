using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public abstract class Model
    {
        #region Constructors

        protected Model(IntPtr ptr)
        {
            this.ModelPtr = ptr;
            this.ReadParameters();
        }

        #endregion Constructors


        #region Properties

        public IntPtr ModelPtr  { get; private set; }

        #endregion Properties


        #region Abstract Methods

        protected abstract void ReadParameters();
        public abstract double[] PredictFromParameters(double[,] unknown_X);

        #endregion Abstract Methods


        #region Methods

        public double[] Predict(double[,] unknown_X)
        {
            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numObserved = unknown_X.GetLength(0);
            int numFeatures = unknown_X.GetLength(1);

            if (numObserved == 0)
                throw new ArgumentOutOfRangeException("unknown_X.Count");

            if (numFeatures == 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumDims");

            if (IntPtr.Zero == this.ModelPtr)
                return this.PredictFromParameters(unknown_X);

            RInterop.InternalSetPrivateVariable("model", this.ModelPtr);

            #region Set up data frame

            /* unkn = data.frame(
             * X0 = c(0, 1, 2, 3),
             * X1 = c(4, 5, 6, 7))
             */

            DataFrame.FromVectors(RInterop.MakePrivateVariable("unkn"), numObserved, (int indx) =>
            {
                Vector vector = null;
                if (indx < numFeatures)
                {
                    object[] values = new object[numObserved];
                    for (int i = 0; i < numObserved; i++)
                    {
                        values[i] = unknown_X[i, indx];
                    }
                    vector = new Vector("X" + indx, values);
                }
                return vector;
            });

            #endregion Set up data frame

            StringBuilder expr = new StringBuilder();

            #region Predict

            /* predict(model, data = unkn)
             */

            expr.AppendFormat("predict({0}, data = {1})",
                RInterop.MakePrivateVariable("model"),
                RInterop.MakePrivateVariable("unkn"));
            object[] unknown_Y = RInterop.Eval(expr.ToString());

            #endregion Predict

            return unknown_Y.Select(item => (double)item).ToArray();
        }

        #endregion Methods


        protected static string LinearFormula(int numFeatures)
        {
            StringBuilder formula = new StringBuilder();
            formula.Append("Y ~ ");
            for (int j = 0; j < numFeatures; j++)
            {
                formula.AppendFormat("X{0}", j);
                if (j < numFeatures-1)
                    formula.Append("+");
            }
            return formula.ToString();
        }
    }
}
