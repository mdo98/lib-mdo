using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Learning.Data
{
    public class DataDescriptionException : LearningException
    {
        public DataDescriptionException() : base() { }
        public DataDescriptionException(string message) : base(message) { }
        public DataDescriptionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
