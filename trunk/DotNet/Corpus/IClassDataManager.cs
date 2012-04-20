using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus
{
    public interface IClassDataManager : IClassDataProvider
    {
        void AddVariant(ClassMetadata metadata);
        void EditVariant(ClassMetadata metadata);
        bool VariantExists(string className, string variantName);
        void AddItem(ClassMetadata metadata, object[] item);
        void ClearItems(string className, string variantName);
        void RemoveVariant(string className, string variantName);
    }
}
