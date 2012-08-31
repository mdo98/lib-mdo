using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public class ArgumentMissingException : Exception
    {
        public ArgumentMissingException() : base() { }
        public ArgumentMissingException(string paramName) : base() { this.ParameterName = paramName; }
        protected ArgumentMissingException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public string ParameterName { get; private set; }
    }
}
