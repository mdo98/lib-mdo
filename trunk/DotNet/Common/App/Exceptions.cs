using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public class ArgumentMissingException : ArgumentException
    {
        private ArgumentMissingException() : base() { }
        public ArgumentMissingException(string paramName) : base(Message(paramName), paramName) { }
        protected ArgumentMissingException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        private static string Message(string paramName)
        {
            return string.Format("Missing parameter: {0}", paramName);
        }
    }
}
