using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Learning
{
    public abstract class LearningException : Exception
    {
        protected LearningException() : base() { }
        protected LearningException(string message) : base(message) { }
        protected LearningException(string message, Exception innerException) : base(message, innerException) { }
    }
}
