using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MDo.Learning.Data
{
    [DataContract]
    internal class DataDescription
    {
        [DataMember]
        public string Name;

        [DataMember]
        public DataPropertyCollection Features = new DataPropertyCollection();

        [DataMember]
        public DataProperty Label;

        public static DataDescription FromType(Type type)
        {
            DataDescription dataDescription = new DataDescription();
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] dataAttributes = property.GetCustomAttributes(typeof(DataAttribute), false);
                if (dataAttributes.Length == 1)
                {
                    DataProperty prop = new DataProperty()
                    {
                        Name = property.Name,
                        Type = property.PropertyType,
                        TypeBinding = property.MemberType,
                    };
                    if (dataAttributes[0] is DataFeatureAttribute)
                    {
                        dataDescription.Features.Add(prop);
                    }
                    else if (dataAttributes[0] is DataLabelAttribute)
                    {
                        if (null != dataDescription.Label)
                            ThrowDataLabelAlreadyDefined();
                        dataDescription.Label = prop;
                    }
                }
            }
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] dataAttributes = field.GetCustomAttributes(typeof(DataAttribute), false);
                if (dataAttributes.Length == 1)
                {
                    DataProperty prop = new DataProperty()
                    {
                        Name = field.Name,
                        Type = field.FieldType,
                        TypeBinding = field.MemberType,
                    };
                    if (dataAttributes[0] is DataFeatureAttribute)
                    {
                        dataDescription.Features.Add(prop);
                    }
                    else if (dataAttributes[0] is DataLabelAttribute)
                    {
                        if (null != dataDescription.Label)
                            ThrowDataLabelAlreadyDefined();
                        dataDescription.Label = prop;
                    }
                }
            }
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object[] dataAttributes = method.GetCustomAttributes(typeof(DataAttribute), false);
                if (dataAttributes.Length == 1)
                {
                    if (method.GetParameters().Length > 0)
                        throw new DataDescriptionException(string.Format(
                            "Method {0} is not parameterless and cannot be added to DataDescription.",
                            method));
                    DataProperty prop = new DataProperty()
                    {
                        Name = method.Name,
                        Type = method.ReturnType,
                        TypeBinding = method.MemberType,
                    };
                    if (dataAttributes[0] is DataFeatureAttribute)
                    {
                        dataDescription.Features.Add(prop);
                    }
                    else if (dataAttributes[0] is DataLabelAttribute)
                    {
                        if (null != dataDescription.Label)
                            ThrowDataLabelAlreadyDefined();
                        dataDescription.Label = prop;
                    }
                }
            }
            return dataDescription;
        }

        private static void ThrowDataLabelAlreadyDefined()
        {
            throw new DataDescriptionException(string.Format(
                "{0} is already defined.",
                typeof(DataLabelAttribute)));
        }
    }
}
