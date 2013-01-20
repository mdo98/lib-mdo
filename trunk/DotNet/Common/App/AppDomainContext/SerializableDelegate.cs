using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public class SerializableDelegate<TDelegate> : ISerializable
        where TDelegate : class
     // where TDelegate : System.Delegate
     // is not allowed; enforcing type through explicit casting.
    {
        private const string Delegate_Method = "(Method)";
        private const string Delegate_Target = "(Target)";

        private Delegate Delegate;

        public TDelegate Action
        {
            get         { return this.Delegate as TDelegate; }
            private set { this.Delegate = value as Delegate; }
        }

        public SerializableDelegate(TDelegate action)
        {
            this.Action = action;

            if (null == this.Delegate)
                throw new ArgumentException(string.Format(
                    "action is of type {0}, which cannot be casted to {1}", typeof(TDelegate).FullName, typeof(Delegate).FullName),
                    "action");
        }

        public SerializableDelegate(SerializationInfo info, StreamingContext context)
        {
            Type delegateType = info.GetValue(SerializableObject.Serialization_Type, typeof(Type)) as Type;

            if (info.GetBoolean(SerializableObject.Serialization_IsDirect))
            {
                this.Delegate = info.GetValue(SerializableObject.Serialization_BaseObj, delegateType) as Delegate;
            }
            else
            {
                MethodInfo method = info.GetValue(Delegate_Method, typeof(MethodInfo)) as MethodInfo;
                SerializableObject target = info.GetValue(Delegate_Target, typeof(SerializableObject)) as SerializableObject;
                this.Delegate = Delegate.CreateDelegate(delegateType, target.BaseObject, method);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(SerializableObject.Serialization_Type, typeof(TDelegate));

            // If the delegate is serializable, then serialize it directly.
            if (null == this.Delegate || null == this.Delegate.Target || this.Delegate.Method.DeclaringType.IsSerializable)
            {
                info.AddValue(SerializableObject.Serialization_IsDirect, true);
                info.AddValue(SerializableObject.Serialization_BaseObj, this.Delegate);
            }
            // Otherwise, use the SerializableObject. Anonymous methods likely will need to serialize this way.
            else
            {
                info.AddValue(SerializableObject.Serialization_IsDirect, false);
                info.AddValue(Delegate_Method, this.Delegate.Method);
                info.AddValue(Delegate_Target, new SerializableObject(this.Delegate.Target.GetType(), this.Delegate.Target));
            }
        }
    }
}
