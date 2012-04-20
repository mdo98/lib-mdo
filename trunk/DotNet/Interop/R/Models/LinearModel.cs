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

        public LinearModel() : base(ModelPurpose.Regression) { }

        public LinearModel(IntPtr ptr) : base(ptr, ModelPurpose.Regression) { }

        #endregion Constructors


        #region Properties

        public LinearModelParameters Parameters { get; protected set; }

        #endregion Properties


        #region Model

        protected override bool ParametersAvailable
        {
            get { return (this.Parameters != null); }
        }

        protected override void Train(RVector observed_X, RVector observed_Y)
        {
            this.SetModelPtr(GenerateRModel(observed_X, observed_Y));
        }

        protected override void ReadParameters()
        {
            if (RInterop.RSEXPREC.FromPointer(this.ModelPtr).Header.SxpInfo.Type != RInterop.RSXPTYPE.VECSXP)
                throw new ArgumentException("this.ModelPtr");

            this.Parameters = new LinearModelParameters(RInterop.RsxprPtrToClrValue(RInterop.RSEXPREC.VecSxp_GetElement(this.ModelPtr, 0), RSxprUtils.RVectorFromStdRSxpr));
            this.Parameters.Validate();
        }

        protected override RVector PredictFromParameters(RVector unknown_X)
        {
            int numObserved = unknown_X.NumRows;
            int numFeatures = unknown_X.NumCols;

            if (numFeatures != this.Parameters.NumRows + 1)
                throw new ArgumentOutOfRangeException("unknown_X.NumCols");

            RVector y = new RVector(new object[numObserved, 1]);
            for (int i = 0; i < numObserved; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < numFeatures; j++)
                {
                    sum += (this.Parameters.Coefficient(j) * (double)unknown_X.Values[i,j]);
                }
                y.Values[i,0] = sum + this.Parameters.Intercept;
            }
            return y;
        }

        protected override RVector RVectorFromRSxprResultPtr(IntPtr val)
        {
            return RSxprUtils.RVectorFromStdRSxpr(val);
        }

        #endregion Model


        #region Static Methods

        public static LinearModel Generate(RVector observed_X, RVector observed_Y)
        {
            return new LinearModel(GenerateRModel(observed_X, observed_Y));
        }

        private static IntPtr GenerateRModel(RVector observed_X, RVector observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.NumCols;

            /* lm(Y ~ X0+X1,
             * data = data)
             */
            return GenerateRModelHelper(observed_X, observed_Y, (string data) => string.Format("lm({0}, data = {1})", LinearFormula(numFeatures), data));
        }

        public static string LinearFormula(int numFeatures)
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

        #endregion Static Methods


        #region Helper Classes

        /// <remarks>
        /// The parameters of a m-feature linear model is an RVector of size [m+1, 1], where
        /// [0, 0] is the intercept and [j+1, 0] is the coefficient of the j-th feature.
        /// </remarks>
        public class LinearModelParameters : RVector
        {
            public int NumFeatures  { get { return this.NumRows - 1; } }
            public double Intercept { get { return (double)this.Values[0, 0]; } }

            public LinearModelParameters(RVector v)
            {
                this.Values = v.Values;
                foreach (string name in v.RowNames)
                    this.RowNames.Add(name);
                foreach (string name in v.ColNames)
                    this.ColNames.Add(name);
            }
            
            public double Coefficient(int indx)
            {
                if (indx < 0 || indx >= this.NumFeatures)
                    throw new ArgumentOutOfRangeException("indx");

                return (double)this.Values[indx+1, 0];
            }

            public override void Validate()
            {
                base.Validate();

                if (this.NumFeatures <= 0)
                    throw new RInteropException("Invalid LinearModelParameters: 0-feature model.");
            }
        }

        #endregion Helper Classes
    }
}
