using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data
{
    public class Metadata
    {
        internal Metadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            this.FolderName = folderName;
            this.FileName = fileName;
        }

        public string   FolderName      { get; private set;  }
        public string   FileName        { get; private set;  }
        public string[] FieldNames      { get; internal set; }
        public Type[]   FieldTypes      { get; internal set; }
        public short    DefaultField    { get; internal set; }
        public long     ItemCount       { get; internal set; }
        public string   Desc            { get; internal set; }
    }
}
