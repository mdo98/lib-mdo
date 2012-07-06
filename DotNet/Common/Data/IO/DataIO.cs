using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public static class DataIO
    {
        public static void CopyData(IDataProvider src, IDataManager dest, CopyOptions options)
        {
            ICollection<Exception> exceptions = new List<Exception>();
            foreach (string folderName in src.ListFolders())
            {
                foreach (string fileName in src.ListFiles(folderName))
                {
                    try
                    {
                        Metadata srcMetadata = src.GetMetadata(folderName, fileName);

                        if (!dest.FileExists(folderName, fileName))
                        {
                            dest.AddFile(srcMetadata);
                        }
                        else
                        {
                            if (!options.AppendDest)
                                dest.ClearItems(folderName, fileName);

                            dest.EditFile(srcMetadata);
                        }

                        Metadata destMetadata = dest.GetMetadata(folderName, fileName);
                        long startIndx = options.EchoSource ? 0L : destMetadata.ItemCount;
                        long numItems = srcMetadata.ItemCount - startIndx;

                        if (numItems <= 0L)
                            continue;

                        dest.AddItems(folderName, fileName, src.GetItems(folderName, fileName, startIndx, numItems));
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
