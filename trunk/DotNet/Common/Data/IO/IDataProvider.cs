using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public interface IDataProvider
    {
        string[] ListFolders();
        string[] ListFiles(string folderName);
        Metadata GetMetadata(string folderName, string fileName);
        object[] GetItem(string folderName, string fileName, long indx);
    }
}
