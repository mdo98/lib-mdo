using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus
{
    public interface IClassDataProvider
    {
        string[] ListClasses();
        string[] ListVariants(string className);
        ClassMetadata GetMetadata(string className, string variantName);
        object[] GetItem(string className, string variantName, int indx);
    }
}
