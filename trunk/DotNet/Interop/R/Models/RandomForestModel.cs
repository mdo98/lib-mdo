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
            RInterop.InternalEval("library(randomForest)");
        }


        #region Constructors

        public RandomForestModel(ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(purpose, formula)
        { }

        public RandomForestModel(IntPtr ptr, ModelPurpose purpose, ModelFormula formula = ModelFormula.Linear)
            : base(ptr, purpose, formula)
        { }

        #endregion Constructors


        #region Model

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
            RInterop.RSEXPREC ans = RInterop.RSEXPREC.FromPointer(val);
            return new RVector(new object[ans.Content.VLength, 1]);
        }

        #endregion Model

        private static IntPtr GenerateRModel(RVector observed_X, RVector observed_Y, ModelFormula formula)
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
            return GenerateRModelHelper(observed_X, observed_Y, (string data) => string.Format("randomForest({0}, data = {1})", getFormula(numFeatures), data));
        }

        public static new RandomForestModel LinearModelForClassification(RVector observed_X, RVector observed_Y)
        {
            ModelFormula formula = ModelFormula.Linear;
            ModelPurpose purpose = ModelPurpose.Classification;
            return new RandomForestModel(GenerateRModel(observed_X, observed_Y, formula), purpose, formula);
        }
    }
}
