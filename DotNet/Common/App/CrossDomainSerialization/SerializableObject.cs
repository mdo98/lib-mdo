using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MDo.Common.App
{
    [Serializable]
    internal sealed class SerializableObject : ISerializable
    {
        public readonly object _baseObject;

        public SerializableObject(object baseObject)
        {
            _baseObject = baseObject;
        }

        public SerializableObject(SerializationInfo info, StreamingContext context)
        {
            Type baseType = info.GetValue("(Type)", typeof(Type)) as Type;
            if (baseType != null)
            {
                try
                {
                    _baseObject = Activator.CreateInstance(baseType, true);
                    foreach (FieldInfo field in baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                            field.SetValue(_baseObject, (info.GetValue(field.Name, typeof(SerializableDelegate<>)) as SerializableDelegate<Delegate>).Action);
                        else if (field.FieldType.IsSerializable)
                            field.SetValue(_baseObject, info.GetValue(field.Name, field.FieldType));
                        else if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                            field.SetValue(_baseObject, (info.GetValue(field.Name, typeof(SerializableObject)) as SerializableObject)._baseObject);
                    }
                }
                catch { }
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Type baseType = _baseObject == null ? null : _baseObject.GetType();
            info.AddValue("(Type)", baseType);
            if (baseType != null)
            {
                foreach (FieldInfo field in baseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                        info.AddValue(field.Name, new SerializableDelegate<Delegate>(field.GetValue(_baseObject) as Delegate));
                    else if (field.FieldType.IsSerializable)
                        info.AddValue(field.Name, field.GetValue(_baseObject));
                    else if (field.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                        info.AddValue(field.Name, new SerializableObject(field.GetValue(_baseObject)));
                }
            }
        }
    }
}
