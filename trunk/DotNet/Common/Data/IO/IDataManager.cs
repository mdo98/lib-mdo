using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public interface IDataManager
    {
        void AddFile(Metadata metadata);
        void EditFile(Metadata metadata);
        bool FileExists(string folderName, string fileName);
        void AddItem(Metadata metadata, object[] item);
        void ClearItems(string folderName, string fileName);
        void RemoveFile(string folderName, string fileName);
    }
}
