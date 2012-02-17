using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.App
{
    public class ArgumentMissingException : Exception
    {
        public ArgumentMissingException() : base() { }
        public ArgumentMissingException(string paramName) : base() { this.ParameterName = paramName; }
        public ArgumentMissingException(string paramName, string message) : base(message) { this.ParameterName = paramName; }

        public string ParameterName { get; private set; }
    }
}
