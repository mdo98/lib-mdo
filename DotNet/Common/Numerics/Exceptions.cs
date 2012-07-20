using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Numerics
{
    public abstract class NumericException : Exception
    {
    }

    public class GslException : NumericException
    {
        public int Code { get; internal set; }
        public override string Message
        {
            get
            {
                StringBuilder s = new StringBuilder(base.Message);
                s.AppendLine();
                s.AppendFormat("GslErrorCode: {0}", this.Code);
                s.AppendLine();
                return s.ToString();
            }
        }
    }
}
