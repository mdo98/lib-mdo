using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    [Serializable]
    public class SerializableObject : ISerializable
    {
        internal const string Serialization_Type     = "(Type)";
        internal const string Serialization_IsDirect = "(Serializable)";
        internal const string Serialization_BaseObj  = "(BObj)";
        private  const string Serialization_FieldFmt = "(Fld-{0})";

        public readonly Type BaseType;
        public readonly object BaseObject;

        public static SerializableObject NullVoid
        {
            get { return new SerializableObject(typeof(void), null); }
        }

        public SerializableObject(Type type, object obj)
        {
            if (null == type)
                throw new ArgumentNullException("type");

            if (null == obj)
                this.BaseType = type;
            else
                this.BaseType = obj.GetType();

            this.BaseObject = obj;
        }

        public SerializableObject(SerializationInfo info, StreamingContext context)
        {
            this.BaseType = info.GetValue(Serialization_Type, typeof(Type)) as Type;

            if (info.GetBoolean(Serialization_IsDirect))
            {
                this.BaseObject = info.GetValue(Serialization_BaseObj, this.BaseType);
            }
            else
            {
                this.BaseObject = Activator.CreateInstance(this.BaseType, true);

                foreach (FieldInfo field in this.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string fieldName = GetFieldNameInSerializationContext(field.Name);

                    if (field.FieldType.IsSerializable)
                        field.SetValue(this.BaseObject, info.GetValue(fieldName, field.FieldType));

                    else if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        field.SetValue(this.BaseObject, (info.GetValue(fieldName, typeof(SerializableObject)) as SerializableObject).BaseObject);
                }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(Serialization_Type, this.BaseType);

            if (this.BaseType.IsSerializable)
            {
                info.AddValue(Serialization_IsDirect, true);
                info.AddValue(Serialization_BaseObj, this.BaseObject);
            }
            else
            {
                info.AddValue(Serialization_IsDirect, false);

                foreach (FieldInfo field in this.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string fieldName = GetFieldNameInSerializationContext(field.Name);

                    if (field.FieldType.IsSerializable)
                        info.AddValue(fieldName, field.GetValue(this.BaseObject));

                    else if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        info.AddValue(fieldName, new SerializableObject(field.FieldType, field.GetValue(this.BaseObject)));
                }
            }
        }

        protected static string GetFieldNameInSerializationContext(string fieldName)
        {
            return string.Format(Serialization_FieldFmt, fieldName);
        }
    }
}
