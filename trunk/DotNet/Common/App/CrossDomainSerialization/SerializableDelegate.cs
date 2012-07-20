using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public class SerializableDelegate<TDelegateType> : ISerializable
        where TDelegateType : class
     // where TDelegateType : System.Delegate
     // is not allowed; type restriction through explicit casting in property get/set.
    {
        private Delegate _delegateAction;

        public TDelegateType Action
        {
            get { return _delegateAction as TDelegateType; }
            private set { _delegateAction = value as Delegate; }
        }

        public SerializableDelegate(TDelegateType action)
        {
            this.Action = action;
        }

        public SerializableDelegate(SerializationInfo info, StreamingContext context)
        {
            Type delegateType = info.GetValue("Type", typeof(Type)) as Type;

            // If the delegate is "simple", then deserialize it directly
            if (info.GetBoolean("Serializable"))
                this.Action = info.GetValue("Delegate", delegateType) as TDelegateType;
            // Otherwise, deserialize anonymous class
            else
            {
                MethodInfo method = info.GetValue("Method", typeof(MethodInfo)) as MethodInfo;
                SerializableObject objectWrapper = info.GetValue("Target", typeof(SerializableObject)) as SerializableObject;
                this.Action = Delegate.CreateDelegate(delegateType, objectWrapper._baseObject, method) as TDelegateType;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Type", typeof(TDelegateType));

            // If the delegate is "simple", then serialize it directly
            if (_delegateAction == null || _delegateAction.Target == null || _delegateAction.Method.DeclaringType.IsSerializable)
            {
                info.AddValue("Serializable", true);
                info.AddValue("Delegate", _delegateAction);
            }
            // Otherwise, wrap target and serialize
            else
            {
                info.AddValue("Serializable", false);
                info.AddValue("Method", _delegateAction.Method);
                info.AddValue("Target", new SerializableObject(_delegateAction.Target));
            }
        }
    }
}
