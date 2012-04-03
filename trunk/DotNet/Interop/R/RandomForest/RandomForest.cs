using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.RandomForest
{
    public class RandomForest
    {
        static RandomForest()
        {
            RInterop.InternalEval("library(randomForest)");
        }
    }
}
