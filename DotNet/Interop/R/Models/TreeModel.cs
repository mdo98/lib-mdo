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

        public TreeModel(ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(purpose)
        {
            this.Formula = formula;
        }

        public TreeModel(IntPtr ptr, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(ptr, purpose)
        {
            this.Formula = formula;
        }

        #endregion Constructors


        #region Properties

        public ModelFormula Formula { get; set; }

        #endregion Properties


        #region Model

        public override void Train(RVector observed_X, RVector observed_Y)
        {
            this.SetModelPtr(GenerateRModel(observed_X, observed_Y, this.Formula, this.Purpose));
        }

        protected override bool ParametersAvailable { get { return false; } }

        protected override void ReadParameters()
        {
            // No-Op (TODO -- too complex!)
        }

        protected override RVector PredictFromParameters(RVector unknown_X)
        {
            throw new NotImplementedException();
        }

        protected override RVector RVectorFromRSxprResultPtr(IntPtr val)
        {
            return RSxprUtils.RVectorFromStdRSxpr(val);
        }

        #endregion Model


        #region Static Methods

        private static IntPtr GenerateRModel(RVector observed_X, RVector observed_Y, ModelFormula formula, ModelPurpose purpose)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.NumCols;

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            Func<int, string> getFormula;
            switch (formula)
            {
                case ModelFormula.Linear:
                    getFormula = LinearModel.LinearFormula;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("formula");
            }

            string method;
            switch (purpose)
            {
                case ModelPurpose.Classification:
                    method = "class";
                    break;

                case ModelPurpose.Regression:
                    method = "anova";
                    break;

                default:
                    throw new ArgumentOutOfRangeException("purpose");
            }

            /* rpart(formula, method = "method", data = data)
             */
            return GenerateRModelHelper(observed_X, observed_Y, (string data) => string.Format("rpart({0}, method = \"{1}\", data = {2})", getFormula(numFeatures), method, data));
        }

        public static TreeModel LinearModelForClassification(RVector observed_X, RVector observed_Y)
        {
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Classification;
            return new TreeModel(GenerateRModel(observed_X, observed_Y, formula, purpose), purpose, formula);
        }

        public static TreeModel LinearModelForRegression(RVector observed_X, RVector observed_Y)
        {
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Regression;
            return new TreeModel(GenerateRModel(observed_X, observed_Y, formula, purpose), purpose, formula);
        }

        #endregion Static Methods
    }
}
