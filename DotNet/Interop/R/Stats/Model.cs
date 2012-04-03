using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Stats
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

            StringBuilder expr = new StringBuilder();

            #region Declare data frame

            /* unkn = data.frame(
             * X1 = c(0, 1, 2, 3),
             * X2 = c(4, 5, 6, 7))
             */

            expr.Append("data.frame(");
            for (int j = 0; j < numFeatures; j++)
            {
                expr.AppendFormat("X{0} = c(", j);
                for (int i = 0; i < numObserved; i++)
                {
                    expr.Append(unknown_X[i, j]);
                    if (i < numObserved - 1)
                        expr.Append(", ");
                    else
                        expr.Append(")");
                }
                if (j < numFeatures - 1)
                    expr.Append(", ");
                else
                    expr.Append(")");
            }

            RInterop.SetPrivateVariable("unkn", expr.ToString());

            #endregion Declare data frame

            expr.Clear();

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
    }
}
