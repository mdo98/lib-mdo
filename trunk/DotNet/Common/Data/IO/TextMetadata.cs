using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Data.IO
{
    public class TextMetadata : Metadata
    {
        private const int DefaultPageSize = 10000;

        internal TextMetadata(string folderName, string fileName)
            : base(folderName, fileName)
        {
            this.PageSize = DefaultPageSize;
        }

        internal TextMetadata(Metadata metadata)
            : base(metadata)
        {
            this.PageSize = DefaultPageSize;
        }

        public int PageSize { get; internal set; }
    }
}
