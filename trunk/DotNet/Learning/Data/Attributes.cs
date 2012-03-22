using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Learning.Data
{
    public abstract class DataAttribute : Attribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method,
        AllowMultiple = false)]
    public class DataFeatureAttribute : DataAttribute
    {
    }

    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method,
        AllowMultiple = false)]
    public class DataLabelAttribute : DataAttribute
    {
    }
}
