using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public class SqlMetadata : Metadata
    {
        internal SqlMetadata(string folderName, string fileName)
            : base(folderName, fileName)
        {
        }

        internal SqlMetadata(Metadata metadata)
            : base(metadata)
        {
        }

        public bool     SupportsIndexing    { get; internal set; }
        public long?    StartIndex          { get; internal set; }
    }
}
