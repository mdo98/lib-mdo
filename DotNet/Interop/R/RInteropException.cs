using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R
{
    public class RInteropException : Exception
    {
        public RInteropException() : base() { }
        public RInteropException(string message) : base(message) { }
    }
}
