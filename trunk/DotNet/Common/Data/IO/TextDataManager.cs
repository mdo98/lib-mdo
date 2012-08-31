using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Data.IO
{
    public class TextDataManager : IDataManager
    {
        public const string MetaFileExtension = ".meta";
        public const string DataFileExtension = ".data";

        private const string Header_FieldCount_Identifier   = "dims";
        private const string Header_ItemCount_Identifier    = "items";
        private const string Header_PageSize_Identifier     = "items/page";
        private const string Header_DefaultDim_Identifier   = "defaultdim";

        
        private readonly string BaseDir;

        public TextDataManager(string baseDir)
        {
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);

            this.BaseDir = baseDir;
        }


        #region IDataProvider

        public string[] ListFolders()
        {
            return Directory.GetDirectories(this.BaseDir).Select(item => Path.GetFileName(item)).ToArray();
        }

        public string[] ListFiles(string folderName)
        {
            return Directory.GetFiles(Path.Combine(this.BaseDir, folderName), string.Format("*{0}", MetaFileExtension))
                .Select(item => Path.GetFileNameWithoutExtension(item)).ToArray();
        }

        public bool FileExists(string folderName, string fileName)
        {
            return File.Exists(this.GetMetaPath(folderName, fileName));
        }

        public Metadata GetMetadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            TextMetadata metadata = new TextMetadata(folderName, fileName);
            using (Stream inStream = File.OpenRead(this.GetMetaPath(folderName, fileName)))
            {
                using (TextReader reader = new StreamReader(inStream))
                {
                    ReadMetadata(reader, metadata);
                }
            }
            return metadata;
        }

        public object[] GetItem(string folderName, string fileName, long indx)
        {
            return this.GetItems(folderName, fileName, indx, 1L).FirstOrDefault();
        }

        public ICollection<object[]> GetItems(string folderName, string fileName, long startIndx, long numItems)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (startIndx < 0L)
                throw new ArgumentOutOfRangeException("startIndx");

            if (numItems < 0L)
                throw new ArgumentOutOfRangeException("numItems");

            ICollection<object[]> items = new List<object[]>();

            TextMetadata metadata = (TextMetadata)this.GetMetadata(folderName, fileName);
            if (startIndx >= metadata.ItemCount)
                return items;

            long endIndx = Math.Min(startIndx+numItems, metadata.ItemCount);

            long startPage = startIndx / metadata.PageSize,
                 endPage = endIndx / metadata.PageSize;

            for (long p = startPage; p <= endPage; p++)
            {
                int numItemsToSkip = 0,
                    numItemsToRead = metadata.PageSize;

                if (p == endPage)
                    numItemsToRead = (int)(endIndx % metadata.PageSize);

                if (p == startPage)
                    numItemsToSkip = (int)(startIndx % metadata.PageSize);

                numItemsToRead -= numItemsToSkip;

                using (Stream inStream = File.OpenRead(this.GetDataPath(folderName, fileName, p)))
                {
                    using (TextReader reader = new StreamReader(inStream))
                    {
                        for (int i = 0; i < numItemsToRead; i++)
                        {
                            items.Add(ReadItem(reader, metadata.FieldTypes));
                        }
                    }
                }
            }
            return items;
        }

        public ICollection<object[]> GetItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            ICollection<object[]> items = new List<object[]>();

            TextMetadata metadata = (TextMetadata)this.GetMetadata(folderName, fileName);
            long endPage = metadata.ItemCount / metadata.PageSize;

            for (long p = 0L; p <= endPage; p++)
            {
                int numItemsToRead = (p != endPage ? metadata.PageSize : (int)(metadata.ItemCount % metadata.PageSize));

                using (Stream inStream = File.OpenRead(this.GetDataPath(folderName, fileName, p)))
                {
                    using (TextReader reader = new StreamReader(inStream))
                    {
                        for (int i = 0; i < numItemsToRead; i++)
                        {
                            items.Add(ReadItem(reader, metadata.FieldTypes));
                        }
                    }
                }
            }
            return items;
        }

        public bool SupportsPages { get { return true; } }

        #endregion IDataProvider


        #region IDataManager

        public void AddFile(Metadata metadata)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.FolderName))
                    throw new ArgumentNullException("metadata.FolderName");

                if (string.IsNullOrWhiteSpace(metadata.FileName))
                    throw new ArgumentNullException("metadata.FileName");

                if (null == metadata.FieldNames)
                    throw new ArgumentNullException("metadata.FieldNames");

                if (null == metadata.FieldTypes)
                    throw new ArgumentNullException("metadata.FieldTypes");

                int dimNamesLength = metadata.FieldNames.Length,
                    dimTypesLength = metadata.FieldTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Fields");
            }

            string metaPath = this.GetMetaPath(metadata.FolderName, metadata.FileName);
            string dir = Path.GetDirectoryName(metaPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (this.FileExists(metadata.FolderName, metadata.FileName))
                throw new InvalidOperationException(string.Format(
                    "Folder '{0}', File '{1}' already exists.",
                    metadata.FolderName,
                    metadata.FileName));

            using (Stream outStream = File.Create(metaPath))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteMetadata(writer, new TextMetadata(metadata) { ItemCount = 0L });
                }
            }
        }

        public void EditFile(Metadata metadata)
        {
            {
                if (null == metadata)
                    throw new ArgumentNullException("metadata");

                if (string.IsNullOrWhiteSpace(metadata.FolderName))
                    throw new ArgumentNullException("metadata.FolderName");

                if (string.IsNullOrWhiteSpace(metadata.FileName))
                    throw new ArgumentNullException("metadata.FileName");

                if (null == metadata.FieldNames)
                    throw new ArgumentNullException("metadata.FieldNames");

                if (null == metadata.FieldTypes)
                    throw new ArgumentNullException("metadata.FieldTypes");

                int dimNamesLength = metadata.FieldNames.Length,
                    dimTypesLength = metadata.FieldTypes.Length;

                if (dimNamesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldNames.Length");

                if (dimTypesLength <= 0)
                    throw new ArgumentOutOfRangeException("metadata.FieldTypes.Length");

                if (dimNamesLength != dimTypesLength)
                    throw new ArgumentException("metadata.Fields");
            }

            if (!this.FileExists(metadata.FolderName, metadata.FileName))
                throw new InvalidOperationException(string.Format(
                    "Folder '{0}', File '{1}' does not exist.",
                    metadata.FolderName,
                    metadata.FileName));

            TextMetadata fileMetadata = (TextMetadata)this.GetMetadata(metadata.FolderName, metadata.FileName);

            if (fileMetadata.Desc != metadata.Desc)
            {
                fileMetadata.Desc = metadata.Desc;
            }
            else if (!fileMetadata.SchemaEquals(metadata))
            {
                this.DeleteDataFiles(fileMetadata);
                fileMetadata.ItemCount = 0L;

                fileMetadata.CopySchema(metadata);
            }
            else
            {
                return;
            }

            using (Stream outStream = File.Open(this.GetMetaPath(metadata.FolderName, metadata.FileName), FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteMetadata(writer, fileMetadata);
                }
            }
        }

        public void AddItem(string folderName, string fileName, object[] item)
        {
            this.AddItems(folderName, fileName, new object[][] { item });
        }

        public void AddItems(string folderName, string fileName, IEnumerable<object[]> items)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (null == items)
                throw new ArgumentNullException("items");

            TextMetadata metadata = (TextMetadata)this.GetMetadata(folderName, fileName);
            var itemIterator = items.GetEnumerator();
            bool hasNext = itemIterator.MoveNext();
            while (hasNext)
            {
                long page = metadata.ItemCount / metadata.PageSize;
                int indx = (int)(metadata.ItemCount % metadata.PageSize),
                    pageIndx = indx;

                using (Stream outStream = (pageIndx == 0
                        ? File.Create(this.GetDataPath(folderName, fileName, page))
                        : File.Open(this.GetDataPath(folderName, fileName, page), FileMode.Append, FileAccess.Write, FileShare.None)))
                {
                    using (TextWriter writer = new StreamWriter(outStream))
                    {
                        for (; indx < metadata.PageSize && hasNext; hasNext = itemIterator.MoveNext(), indx++)
                        {
                            WriteItem(writer, itemIterator.Current);
                        }
                    }
                }

                metadata.ItemCount += (indx - pageIndx);
            }

            using (Stream outStream = File.Open(this.GetMetaPath(folderName, fileName), FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteMetadata(writer, metadata);
                }
            }
        }

        public void ClearItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            TextMetadata metadata = (TextMetadata)this.GetMetadata(folderName, fileName);
            this.DeleteDataFiles(metadata);
            metadata.ItemCount = 0L;
            using (Stream outStream = File.Open(this.GetMetaPath(folderName, fileName), FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteMetadata(writer, metadata);
                }
            }
        }

        public void RemoveFile(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            TextMetadata metadata = (TextMetadata)this.GetMetadata(folderName, fileName);
            File.Delete(this.GetMetaPath(folderName, fileName));
            this.DeleteDataFiles(metadata);
        }

        #endregion IDataManager


        #region InternalOps

        private string GetDataPath(string folderName, string fileName, long page)
        {
            return Path.Combine(this.BaseDir, folderName, string.Format("{0}.{1}{2}", fileName, page, DataFileExtension));
        }

        private string GetMetaPath(string folderName, string fileName)
        {
            return Path.Combine(this.BaseDir, folderName, string.Format("{0}{1}", fileName, MetaFileExtension));
        }

        private static void ReadMetadata(TextReader reader, TextMetadata metadata)
        {
            metadata.Desc = reader.ReadLine();

            bool header = true;
            short numCols = 0;
            while (header)
            {
                string[] parts;
                try
                {
                    parts = SplitStringAndCheck(reader.ReadLine(), " ", 2);
                }
                catch
                {
                    break;
                }
                switch (parts[1].Trim().ToLowerInvariant())
                {
                    case Header_FieldCount_Identifier:
                        {
                            short num = short.Parse(parts[0].Trim());
                            if (num < 0 || num > short.MaxValue)
                                ThrowInvalidDataStream(string.Format(
                                    "Metadata: '{0}' must be between [{1}->{2}]",
                                    Header_FieldCount_Identifier,
                                    0,
                                    short.MaxValue));
                            numCols = num;
                        }
                        break;

                    case Header_ItemCount_Identifier:
                        {
                            long num = long.Parse(parts[0].Trim());
                            if (num < 0 || num > long.MaxValue)
                                ThrowInvalidDataStream(string.Format(
                                    "Metadata: '{0}' must be between [{1}->{2}]",
                                    Header_ItemCount_Identifier,
                                    0,
                                    long.MaxValue));
                            metadata.ItemCount = num;
                        }
                        break;

                    case Header_PageSize_Identifier:
                        {
                            int num = int.Parse(parts[0].Trim());
                            if (num < 1 || num > int.MaxValue)
                                ThrowInvalidDataStream(string.Format(
                                    "Metadata: '{0}' must be between [{1}->{2}]",
                                    Header_PageSize_Identifier,
                                    1,
                                    int.MaxValue));
                            metadata.PageSize = num;
                        }
                        break;

                    default:
                        ThrowInvalidDataStream(string.Format(
                            "Metadata: '{0}'/'{1}'/'{2}' expected, found '{3}'",
                            Header_FieldCount_Identifier,
                            Header_ItemCount_Identifier,
                            Header_PageSize_Identifier,
                            parts[1]));
                        break;
                }
            }
            
            if (numCols <= 0)
                ThrowInvalidDataStream(string.Format("Metadata: '{0}' not found", Header_FieldCount_Identifier));

            metadata.FieldNames = new string[numCols];
            metadata.FieldTypes = new Type[numCols];

            ISet<string> colNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] colMetadata = SplitStringAndCheck(reader.ReadLine(), "\t", numCols);
            for (short j = 0; j < numCols; j++)
            {
                string[] colNameAndType = SplitStringAndCheck(colMetadata[j], "::", 2);
                
                string colName = colNameAndType[0].Trim();
                if (colNames.Contains(colName))
                    ThrowInvalidDataStream(string.Format(
                        "Metadata: '{0}' is applied to more than one field",
                        colName));
                colNames.Add(colName);

                Type colType = Type.GetType(colNameAndType[1].Trim());
                if (null == colType)
                    ThrowInvalidDataStream(string.Format(
                        "Metadata: '{0}' is not a valid CLR type",
                        colNameAndType[1].Trim()));

                metadata.FieldNames[j] = colName;
                metadata.FieldTypes[j] = colType;
            }
        }

        private static object[] ReadItem(TextReader reader, Type[] dimTypes)
        {
            object[] item = new object[dimTypes.Length];
            string[] itemParts = SplitStringAndCheck(reader.ReadLine(), "\t", dimTypes.Length, StringSplitOptions.None);
            for (int j = 0; j < dimTypes.Length; j++)
            {
                item[j] = SqlUtility.ToObject(itemParts[j], dimTypes[j]);
            }
            return item;
        }

        private static void WriteMetadata(TextWriter writer, TextMetadata metadata)
        {
            writer.WriteLine(metadata.Desc ?? string.Empty);
            writer.WriteLine(string.Format("{0} {1}", metadata.ItemCount, Header_ItemCount_Identifier));
            writer.WriteLine(string.Format("{0} {1}", metadata.PageSize, Header_PageSize_Identifier));
            writer.WriteLine(string.Format("{0} {1}", metadata.FieldNames.Length, Header_FieldCount_Identifier));
            writer.WriteLine();
            for (short j = 0; j < metadata.FieldNames.Length; j++)
            {
                writer.Write("{0}::{1}", metadata.FieldNames[j], metadata.FieldTypes[j].FullName);
                if (j < metadata.FieldNames.Length - 1)
                    writer.Write("\t");
                else
                    writer.WriteLine();
            }
        }

        private static void WriteItem(TextWriter writer, object[] item)
        {
            for (short j = 0; j < item.Length; j++)
            {
                object obj = item[j];
                if (null != obj)
                    writer.Write(obj.ToString());

                if (j < item.Length - 1)
                    writer.Write("\t");
                else
                    writer.WriteLine();
            }
        }

        private void DeleteDataFiles(TextMetadata metadata)
        {
            long page = metadata.ItemCount / metadata.PageSize;
            for (long p = 0; p <= page; p++)
            {
                File.Delete(this.GetDataPath(metadata.FolderName, metadata.FileName, p));
            }
        }

        private static string[] SplitStringAndCheck(string line, string splitStr, int numParts,
            StringSplitOptions splitOptions = StringSplitOptions.RemoveEmptyEntries)
        {
            if (null == line)
                throw new ArgumentNullException("line");

            string[] parts = line.Split(new string[] { splitStr }, splitOptions);
            if (parts.Length != numParts)
                ThrowInvalidDataStream(string.Format(
                    "String expected to split with '{0}' into {1} parts; found {2}",
                    splitStr,
                    numParts,
                    parts.Length));

            return parts;
        }

        private static void ThrowInvalidDataStream(string reason)
        {
            throw new InvalidDataException(string.Format("Invalid data stream: {0}.", reason));
        }

        #endregion InternalOps
    }
}
