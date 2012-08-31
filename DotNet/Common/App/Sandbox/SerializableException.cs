using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public class SerializableException : Exception, IEquatable<Exception>, IEquatable<SerializableException>
    {
        public Exception Exception { get; private set; }
        public Type ExceptionType  { get; private set; }

        public SerializableException(Exception ex)
        {
            if (null == ex)
                throw new ArgumentNullException("ex");

            this.Exception = ex;
            this.ExceptionType = ex.GetType();
        }

        public SerializableException(SerializationInfo info, StreamingContext context)
        {
            this.ExceptionType = info.GetValue(SerializableObject.Serialization_Type, typeof(Type)) as Type;

            if (info.GetBoolean(SerializableObject.Serialization_IsDirect))
            {
                this.Exception = info.GetValue(SerializableObject.Serialization_BaseObj, this.ExceptionType) as Exception;
            }
            else
            {
                ConstructorInfo exceptionConstructor = null;
                Type serializationInfoType = info.GetType(),
                     streamingContextType = context.GetType();
                for (Type exceptionType = this.ExceptionType; null != exceptionConstructor && exceptionType != typeof(object); exceptionType = exceptionType.BaseType)
                {
                    exceptionConstructor = exceptionType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { serializationInfoType, streamingContextType }, null);
                }
                if (null != exceptionConstructor)
                {
                    exceptionConstructor.Invoke(new object[] { info, context });
                }
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializableObject.Serialization_Type, this.ExceptionType);

            // If the exception is serializable, then serialize it directly.
            if (this.ExceptionType.IsSerializable)
            {
                info.AddValue(SerializableObject.Serialization_IsDirect, true);
                info.AddValue(SerializableObject.Serialization_BaseObj, this.Exception);
            }
            // Otherwise, serialize as much as we can.
            else
            {
                info.AddValue(SerializableObject.Serialization_IsDirect, false);
                this.Exception.GetObjectData(info, context);
            }
        }

        public new Exception InnerException
        {
            get { return this.Exception.InnerException; }
        }

        public override IDictionary Data
        {
            get { return this.Exception.Data; }
        }

        public override string HelpLink
        {
            get { return this.Exception.HelpLink;   }
            set { this.Exception.HelpLink = value;  }
        }

        public override string Message
        {
            get { return this.Exception.Message; }
        }

        public override string Source
        {
            get { return this.Exception.Source;     }
            set { this.Exception.Source = value;    }
        }

        public override string StackTrace
        {
            get { return this.Exception.StackTrace; }
        }

        public override Exception GetBaseException()
        {
            return this.Exception.GetBaseException();
        }

        public override int GetHashCode()
        {
            return this.Exception.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.ExceptionType.FullName, this.Exception.ToString());
        }

        public bool Equals(Exception other)
        {
            if (null == other)
                return false;
            else if (other is SerializableException)
                return this.Exception.Equals((other as SerializableException).Exception);
            else
                return this.Exception.Equals(other);
        }

        public bool Equals(SerializableException other)
        {
            if (null == other)
                return false;
            else
                return this.Exception.Equals(other.Exception);
        }

        public override bool Equals(object obj)
        {
            return (obj is Exception && this.Equals(obj as Exception));
        }
    }
}
