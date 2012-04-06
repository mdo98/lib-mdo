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

        protected TreeModel(IntPtr ptr) : base(ptr) { }

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
            return RSxprUtils.RVectorFromStdRSxpr(val);
        }

        #endregion Model


        public static TreeModel LinearModelForClassification(RVector observed_X, RVector observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.NumCols;

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            return new TreeModel(GenerateHelper(observed_X, observed_Y, (string data) => GetTreeModelExpr(LinearModel.LinearFormula(numFeatures), "class", data)));
        }

        public static TreeModel LinearModelForRegression(RVector observed_X, RVector observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.NumCols;

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            return new TreeModel(GenerateHelper(observed_X, observed_Y, (string data) => GetTreeModelExpr(LinearModel.LinearFormula(numFeatures), "anova", data)));
        }

        protected static string GetTreeModelExpr(string formula, string method, string data)
        {
            /* rpart(formula, method = "method", data = data)
             */
            return string.Format("rpart({0}, method = \"{1}\", data = {2})", formula, method, data);
        }
    }
}
