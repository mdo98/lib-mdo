using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public class RandomForestModel : TreeModel
    {
        static RandomForestModel()
        {
            RInterop.Eval("library(randomForest)", null);
        }


        #region Constructors

        public RandomForestModel(string name, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(name, purpose, formula)
        { }

        public RandomForestModel(IntPtr ptr, string name, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(ptr, name, purpose, formula)
        { }

        #endregion Constructors


        #region Model

        public override void Train(RVector observed_X, RVector observed_Y)
        {
            this.SetPtr(GenerateRModel(this.Name, observed_X, observed_Y, this.Formula, this.Config));
        }

        protected override RVector InitRVectorFromRSxprResultPtr(IntPtr val)
        {
            RInterop.RSEXPREC ans = RInterop.RSEXPREC.FromPointer(val);
            return new RVector(new object[ans.Content.VLength, 1]);
        }

        #endregion Model


        #region Static Methods

        private static IntPtr GenerateRModel(string name, RVector observed_X, RVector observed_Y, ModelFormula formula, string config = null)
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

            /* randomForest(formula, data = data)
             */
            return GenerateRModelHelper(name, observed_X, observed_Y, (string data) => string.Format("randomForest({0}, data = {1})", getFormula(numFeatures), data));
        }

        public static new RandomForestModel LinearModelForClassification(RVector observed_X, RVector observed_Y, string config = null, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = AutoName<RandomForestModel>();
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Classification;
            return new RandomForestModel(GenerateRModel(name, observed_X, observed_Y, formula, config), name, purpose, formula);
        }

        #endregion Static Methods
    }
}
