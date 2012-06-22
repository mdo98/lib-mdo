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
            RInterop.Eval("library(rpart)", null);
        }


        #region Constructors

        public TreeModel(string name, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(name, purpose)
        {
            this.Formula = formula;
        }

        public TreeModel(IntPtr ptr, string name, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(ptr, name, purpose)
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
            this.SetPtr(GenerateRModel(this.Name, observed_X, observed_Y, this.Formula, this.Purpose, this.Config));
        }

        protected override bool ParametersAvailable { get { return false; } }

        protected override void ReadParameters()
        {
            // No-Op (TODO -- too complex!)
        }

        protected override RVector PredictFromParameters(RVector unknown_X)
        {
            throw new NotSupportedException();
        }

        #endregion Model


        #region Static Methods

        private static IntPtr GenerateRModel(string name, RVector observed_X, RVector observed_Y, ModelFormula formula, ModelPurpose purpose, string config = null)
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

            /* rpart(formula, method = "method", data = data, control = config)
             */
            Func<string, string> getModelExprForData = (string data) =>
            {
                StringBuilder expr = new StringBuilder();
                expr.AppendFormat("rpart({0}, method = \"{1}\", data = {2}", getFormula(numFeatures), method, data);
                if (!string.IsNullOrWhiteSpace(config))
                    expr.AppendFormat(", control = rpart.control({0})", config);
                expr.Append(")");
                return expr.ToString();
            };
            return GenerateRModelHelper(name, observed_X, observed_Y, getModelExprForData);
        }

        public static TreeModel LinearModelForClassification(RVector observed_X, RVector observed_Y, string config = null, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = AutoName<TreeModel>();
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Classification;
            return new TreeModel(GenerateRModel(name, observed_X, observed_Y, formula, purpose, config), name, purpose, formula);
        }

        public static TreeModel LinearModelForRegression(RVector observed_X, RVector observed_Y, string config = null, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = AutoName<TreeModel>();
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Regression;
            return new TreeModel(GenerateRModel(name, observed_X, observed_Y, formula, purpose, config), name, purpose, formula);
        }

        #endregion Static Methods
    }
}
