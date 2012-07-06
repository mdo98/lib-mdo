using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public static class DataIO
    {
        internal const string MetaSchemaResx = "DataIO.sql";
        internal const string MetaSchemaName = "DataIO";

        public static void ProvisionDatabase(string connString)
        {
            string script;
            Type thisType = typeof(DataIO);
            using (Stream resxStream = thisType.Assembly.GetManifestResourceStream(string.Format(
                   "{0}.{1}", thisType.Namespace, DataIO.MetaSchemaResx)))
            {
                using (TextReader reader = new StreamReader(resxStream))
                {
                    script = reader.ReadToEnd();
                }
            }
            SqlUtility.ExecuteSqlScript(connString, script);
        }

        public static void ProvisionDatabase(string server, string database, string userId, string password)
        {
            string script;
            Type thisType = typeof(DataIO);
            using (Stream resxStream = thisType.Assembly.GetManifestResourceStream(string.Format(
                   "{0}.{1}", thisType.Namespace, DataIO.MetaSchemaResx)))
            {
                using (TextReader reader = new StreamReader(resxStream))
                {
                    script = reader.ReadToEnd();
                }
            }
            SqlUtility.ExecuteSqlScript(server, database, userId, password, script);
        }

        public static void ImportData(IDataProvider src, IDataManager dest, bool append)
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
                            long srcItemCount = srcMetadata.ItemCount;
                            srcMetadata.ItemCount = 0L;
                            dest.AddFile(srcMetadata);
                            srcMetadata.ItemCount = srcItemCount;
                        }
                        else
                        {
                            if (!append)
                                dest.ClearItems(folderName, fileName);

                            dest.EditFile(srcMetadata);
                        }

                        Metadata destMetadata = dest.GetMetadata(folderName, fileName);
                        long startIndx = destMetadata.ItemCount;
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
