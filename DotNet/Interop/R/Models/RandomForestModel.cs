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

        protected RandomForestModel(IntPtr ptr) : base(ptr) { }

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


        public static new RandomForestModel LinearModelForClassification(RVector observed_X, RVector observed_Y)
        {
            if (null == observed_X)
                throw new ArgumentNullException("observed_X");

            int numFeatures = observed_X.NumCols;

            if (numFeatures <= 0)
                throw new ArgumentOutOfRangeException("observed_X.NumCols");

            return new RandomForestModel(GenerateHelper(observed_X, observed_Y, (string data) => string.Format("randomForest({0}, data = {1})", LinearModel.LinearFormula(numFeatures), data)));
        }
    }
}
