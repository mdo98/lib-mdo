using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Test.DataGeneration
{
    public struct ClassificationData
    {
        public object[,] Training_X;
        public object[,] Training_Y;
        public object[,] Test_X;
        public object[,] Test_Y;
    }
}
