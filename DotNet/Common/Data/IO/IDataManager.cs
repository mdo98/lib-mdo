using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public interface IDataManager : IDataProvider
    {
        void AddFile(Metadata metadata);
        void EditFile(Metadata metadata);
        void AddItem(string folderName, string fileName, object[] item);
        void AddItems(string folderName, string fileName, IEnumerable<object[]> items);
        void ClearItems(string folderName, string fileName);
        void RemoveFile(string folderName, string fileName);
    }
}
