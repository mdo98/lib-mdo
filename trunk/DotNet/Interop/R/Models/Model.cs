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
        protected abstract RVector PredictFromParameters(RVector unknown_X);
        protected abstract RVector RVectorFromRSxprResultPtr(IntPtr ptr);

        #endregion Abstract Methods


        #region Methods

        public RVector Predict(RVector unknown_X)
        {
            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numObserved = unknown_X.NumRows;
            int numFeatures = unknown_X.NumCols;

            if (numObserved <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumRows");

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumCols");

            if (IntPtr.Zero == this.ModelPtr)
                return this.PredictFromParameters(unknown_X);
            else
                return RInterop.RsxprPtrToClrValue(PredictHelper(unknown_X, this.ModelPtr), this.RVectorFromRSxprResultPtr);
        }

        #endregion Methods


        #region Static Methods

        protected static IntPtr GenerateHelper(RVector observed_X, RVector observed_Y, Func<string, string> getRModelExprForData)
        {
            if (null == getRModelExprForData)
                throw new ArgumentNullException("getRModelExprForData");

            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            if (null == observed_Y)
                throw new ArgumentNullException("observed_Y");

            int numObserved = observed_X.NumRows;
            int numFeatures = observed_X.NumCols;

            if (numObserved <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumRows");

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            if (observed_Y.NumRows != numObserved)
                throw new ArgumentOutOfRangeException("observed_Y.NumRows");

            if (observed_Y.NumCols != 1)
                throw new ArgumentOutOfRangeException("observed_Y.NumCols");

            #region Set up data frame

            /* data = data.frame(
             * X0 = c(0, 1, 2, 3),
             * X1 = c(4, 5, 6, 7),
             * Y = c(8, 9, 10, 11))
             */

            RVector x = new RVector(observed_X.Values);
            x.SetXVarColNames();

            RVector y = new RVector(observed_Y.Values);
            y.SetYVarColNames();

            DataFrame.FromVectors(RInterop.MakePrivateVariable("data"), x, y);

            #endregion Set up data frame

            StringBuilder expr = new StringBuilder();

            #region Fit model

            IntPtr model = RInterop.InternalEval(getRModelExprForData(RInterop.MakePrivateVariable("data")));

            #endregion Fit model

            return model;
        }

        protected static IntPtr PredictHelper(RVector unknown_X, IntPtr model)
        {
            if (IntPtr.Zero == model)
                throw new NullReferenceException("model cannot be a zero-pointer.");

            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numObserved = unknown_X.NumRows;
            int numFeatures = unknown_X.NumCols;

            if (numObserved <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumRows");

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumCols");

            RInterop.InternalSetPrivateVariable("model", model);

            #region Set up data frame

            /* unkn = data.frame(
             * X0 = c(0, 1, 2, 3),
             * X1 = c(4, 5, 6, 7))
             */

            RVector x = new RVector(unknown_X.Values);
            x.SetXVarColNames();

            DataFrame.FromVectors(RInterop.MakePrivateVariable("unkn"), x);

            #endregion Set up data frame

            StringBuilder expr = new StringBuilder();

            #region Predict

            /* predict(model, data = unkn)
             */

            expr.AppendFormat("predict({0}, newdata = {1})",
                RInterop.MakePrivateVariable("model"),
                RInterop.MakePrivateVariable("unkn"));

            IntPtr unknown_Y = RInterop.InternalEval(expr.ToString());

            #endregion Predict

            return unknown_Y;
        }

        public static IntPtr GenerateAndPredict(RVector observed_X, RVector observed_Y, RVector unknown_X, Func<string, string> getRModelExprForData)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numFeatures = observed_X.NumCols;

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            if (unknown_X.NumCols != numFeatures)
                throw new ArgumentOutOfRangeException("unknown_X.NumCols");

            return PredictHelper(unknown_X, GenerateHelper(observed_X, observed_Y, getRModelExprForData));
        }

        #endregion Static Methods
    }
}
