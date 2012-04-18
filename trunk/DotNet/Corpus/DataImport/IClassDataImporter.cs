using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus.DataImport
{
    public interface IClassDataImporter : IClassDataProvider
    {
        void Import(string className, string variantName, IClassDataManager destStore);
    }
}
