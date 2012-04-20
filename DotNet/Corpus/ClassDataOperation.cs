using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus
{
    public static class ClassDataOperation
    {
        public static void Import(IClassDataProvider src, IClassDataManager dest, string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            ClassMetadata metadata = src.GetMetadata(className, variantName);
            if (dest.VariantExists(className, variantName))
            {
                dest.ClearItems(metadata.Class, metadata.Variant);
                dest.EditVariant(metadata);
            }
            else
            {
                dest.AddVariant(metadata);
            }

            for (int i = 0; i < metadata.NumItems; i++)
            {
                dest.AddItem(metadata, src.GetItem(className, variantName, i));
            }
        }
    }
}
