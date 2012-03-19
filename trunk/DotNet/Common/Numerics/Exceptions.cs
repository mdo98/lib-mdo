using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public abstract class NumericException : Exception
    {
    }

    public class GslException : NumericException
    {
        public int Code;
    }
}
