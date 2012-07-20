using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public class SerializableException<T> : Exception
        where T : Exception
    {
        #region Constants

        private const string Serialization_StackTrace = "(CallStack)";

        #endregion Constants
        

        #region Fields

        protected readonly string _stackTrace;

        #endregion Fields


        public SerializableException(T ex) : base(ex.Message)
        {
            this._stackTrace = ex.StackTrace;
        }

        public SerializableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this._stackTrace = info.GetValue(Serialization_StackTrace, typeof(string)) as string;
        }


        #region Error overrides

        public override string StackTrace
        {
            get { return this._stackTrace; }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(Serialization_StackTrace, this._stackTrace);
        }

        #endregion Error overrides
    }
}
