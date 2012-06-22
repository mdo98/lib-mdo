using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public abstract class Model : RObject
    {
        #region Constructors

        protected Model(IntPtr ptr, string name, ModelPurpose purpose)
            : base(ptr, name)
        {
            this.Purpose = purpose;
        }

        protected Model(string name, ModelPurpose purpose)
            : this(IntPtr.Zero, name, purpose)
        {
        }

        #endregion Constructors


        #region Properties

        public bool         Trained { get { return (IntPtr.Zero != this.Ptr || this.ParametersAvailable); } }
        
        public ModelPurpose Purpose { get; private set; }
        
        public string       Config  { get; set; }

        #endregion Properties


        #region RObject

        protected override void OnPtrSet()
        {
            this.ReadParameters();
        }

        #endregion RObject


        #region Abstract Members

        public abstract void Train(RVector observed_X, RVector observed_Y);
        protected abstract bool ParametersAvailable { get; }
        protected abstract void ReadParameters();
        protected abstract RVector PredictFromParameters(RVector unknown_X);

        #endregion Abstract Members


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

            if (this.ParametersAvailable)
                return this.PredictFromParameters(unknown_X);
            else
                return this.PredictHelper(unknown_X);
        }

        protected virtual RVector InitRVectorFromRSxprResultPtr(IntPtr val)
        {
            return RSxprUtils.RVectorFromStdRSxpr(val);
        }

        protected RVector PredictHelper(RVector unknown_X)
        {
            if (IntPtr.Zero == this.Ptr)
                throw new InvalidOperationException("Cannot predict: Model has not been trained.");

            if (null == unknown_X)
                throw new ArgumentNullException("unknown_X");

            int numObserved = unknown_X.NumRows;
            int numFeatures = unknown_X.NumCols;

            if (numObserved <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumRows");

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("unknown_X.NumCols");

            /* data.frame(
             * X0 = c(0, 1, 2, 3),
             * X1 = c(4, 5, 6, 7))
             */

            RVector x = new RVector(unknown_X.Values);
            x.SetXVarColNames();

            /* predict(model, newdata = data)
             */

            StringBuilder expr = new StringBuilder();

            expr.AppendFormat("predict({0}, newdata = {1})",
                this.Name,
                DataFrame.ExpressionFromVectors(x));

            return RInterop.EvalToVector(expr.ToString(), this.InitRVectorFromRSxprResultPtr);
        }

        #endregion Methods


        #region Static Methods

        protected static IntPtr GenerateRModelHelper(string name, RVector observed_X, RVector observed_Y, Func<string, string> getRModelExprForData)
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

            #endregion Set up data frame
            
            #region Fit model

            IntPtr model = RInterop.Eval(getRModelExprForData(DataFrame.ExpressionFromVectors(x, y)), name);

            #endregion Fit model

            return model;
        }

        public static RVector GenerateAndPredict(RVector observed_X, RVector observed_Y, RVector unknown_X, Func<string, string> getRModelExprForData)
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

            string name = AutoName<GenericModel>();
            return (new GenericModel(GenerateRModelHelper(name, observed_X, observed_Y, getRModelExprForData), name)).PredictHelper(unknown_X);
        }

        #endregion Static Methods
    }

    internal class GenericModel : Model
    {
        public GenericModel(IntPtr ptr, string name) : base(ptr, name, ModelPurpose.Unknown) { }

        public override void Train(RVector observed_X, RVector observed_Y)
        {
            throw new NotSupportedException();
        }

        protected override bool ParametersAvailable
        {
            get { return false; }
        }

        protected override void ReadParameters()
        {
        }

        protected override RVector PredictFromParameters(RVector unknown_X)
        {
            throw new NotSupportedException();
        }
    }
}
