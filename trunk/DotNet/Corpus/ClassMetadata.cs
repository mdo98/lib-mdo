using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus
{
    public class ClassMetadata
    {
        internal ClassMetadata(string className, string variantName)
        {
            this.Class = className;
            this.Variant = FixVariantName(variantName);
        }

        public string   Class           { get; private set;  }
        public string   Variant         { get; private set;  }
        public string[] VariantNames    { get; internal set; }
        public string[] DimensionNames  { get; internal set; }
        public Type[]   DimensionTypes  { get; internal set; }
        public short    DefaultDim      { get; internal set; }
        public int      NumItems        { get; internal set; }
        public string   Desc            { get; internal set; }

        public const string DefaultVariantName = "default";
        public static string FixVariantName(string variantName)
        {
            if (string.IsNullOrWhiteSpace(variantName))
                return DefaultVariantName;
            else
                return variantName;
        }
    }
}
