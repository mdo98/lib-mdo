using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R.Models
{
    public class RandomForestModel : Model
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
            throw new NotImplementedException();
        }

        public override double[] PredictFromParameters(double[,] unknown_X)
        {
            throw new NotImplementedException();
        }

        #endregion Model
    }
}
