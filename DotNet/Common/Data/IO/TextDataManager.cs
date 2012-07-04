using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Common.IO;

namespace MDo.Common.Data.IO
{
    class TextDataManager : IDataManager
    {
        private const string DataFileExtension = ".data";

        private const string Header_FieldCount_Identifier   = "dims";
        private const string Header_ItemCount_Identifier    = "items";
        private const string Header_DefaultDim_Identifier   = "defaultdim";

        
        private readonly string BaseDir;

        public TextDataManager(string baseDir)
        {
            this.BaseDir = baseDir;
        }


        #region IDataProvider

        public string[] ListFolders()
        {
            return Directory.GetDirectories(this.BaseDir);
        }

        public string[] ListFiles(string folderName)
        {
            return Directory.GetFiles(Path.Combine(this.BaseDir, folderName), string.Format("*{0}", DataFileExtension));
        }

        public Metadata GetMetadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            Metadata metadata = new Metadata(folderName, fileName);
            using (Stream inStream = FS.OpenRead(this.GetPath(folderName, fileName)))
            {
                using (TextReader reader = new StreamReader(inStream))
                {
                    ReadHeader(reader, metadata);
                }
            }
            return metadata;
        }

        public object[] GetItem(string folderName, string fileName, long indx)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            if (indx < 0L)
                throw new ArgumentOutOfRangeException("indx");

            object[] item;
            using (Stream inStream = FS.OpenRead(this.GetPath(folderName, fileName)))
            {
                using (TextReader reader = new StreamReader(inStream))
                {
                    Metadata metadata = new Metadata(folderName, fileName);
                    ReadHeader(reader, metadata);
                    for (long i = 0; i < indx; i++)
                        reader.ReadLine();

                    item = ReadItem(reader, metadata.FieldTypes);
                }
            }
            return item;
        }

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

            if (FileExists(metadata.FolderName, metadata.FileName))
                throw new InvalidOperationException("Folder '{0}', File '{1}' already exists.");

            using (Stream outStream = File.Create(this.GetPath(metadata.FolderName, metadata.FileName)))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteHeader(writer, metadata);
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

            throw new NotSupportedException();
        }

        public bool FileExists(string folderName, string fileName)
        {
            return File.Exists(this.GetPath(folderName, fileName));
        }

        public void AddItem(Metadata metadata, object[] item)
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

                if (null == item)
                    throw new ArgumentNullException("item");

                if (item.Length != dimNamesLength)
                    throw new ArgumentOutOfRangeException("item.Length");
            }

            if (!FileExists(metadata.FolderName, metadata.FileName))
                throw new InvalidOperationException(string.Format(
                    "Folder '{0}', File '{1}' does not exist.",
                    metadata.FolderName,
                    metadata.FileName));

            using (Stream outStream = File.Open(this.GetPath(metadata.FolderName, metadata.FileName), FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                using (TextWriter writer = new StreamWriter(outStream))
                {
                    WriteItem(writer, item);
                }
            }
        }

        public void ClearItems(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            throw new NotSupportedException();
        }

        public void RemoveFile(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            File.Delete(this.GetPath(folderName, fileName));
        }

        #endregion IDataManager


        #region InternalOps

        private string GetPath(string folderName, string fileName)
        {
            return Path.Combine(this.BaseDir, folderName, string.Format("{0}{1}", fileName, DataFileExtension));
        }

        protected static void ReadHeader(TextReader reader, Metadata metadata)
        {
            metadata.Desc = reader.ReadLine();

            short numCols = 0;
            while (true)
            {
                string[] parts = SplitStringAndCheck(reader.ReadLine(), " ", 2);
                switch (parts[1].Trim().ToLowerInvariant())
                {
                    case Header_FieldCount_Identifier:
                        {
                            short num = short.Parse(parts[0].Trim());
                            if (num < 0 || num > short.MaxValue)
                                ThrowInvalidDataStream(string.Format(
                                    "Header: '{0}' must be between [{1}->{2}]",
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
                                    "Header: '{0}' must be between [{1}->{2}]",
                                    Header_ItemCount_Identifier,
                                    0,
                                    long.MaxValue));
                            metadata.ItemCount = num;
                        }
                        break;

                    default:
                        ThrowInvalidDataStream(string.Format(
                            "Header: '{0}'/'{1}' expected, found '{2}'",
                            Header_FieldCount_Identifier,
                            Header_ItemCount_Identifier,
                            parts[1]));
                        break;
                }
                if (numCols > 0 && metadata.ItemCount > 0L)
                {
                    metadata.FieldNames = new string[numCols];
                    metadata.FieldTypes = new Type[numCols];
                    break;
                }
            }

            string[] defaultColMeta = SplitStringAndCheck(reader.ReadLine(), "=", 2);
            if (defaultColMeta[0].Trim().ToLowerInvariant() != Header_DefaultDim_Identifier)
                ThrowInvalidDataStream(string.Format(
                    "Header: '{0}' expected, found '{1}'",
                    Header_DefaultDim_Identifier,
                    defaultColMeta[0]));
            string defaultColName = defaultColMeta[1].Trim();

            ISet<string> colNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] colMetadata = SplitStringAndCheck(reader.ReadLine(), "\t", numCols);
            for (short j = 0; j < numCols; j++)
            {
                string[] colNameAndType = SplitStringAndCheck(colMetadata[j], "::", 2);
                
                string colName = colNameAndType[0].Trim();
                if (colNames.Contains(colName))
                    ThrowInvalidDataStream(string.Format(
                        "Header: '{0}' is applied to more than one dimension",
                        colName));

                Type colType = Type.GetType(colNameAndType[1].Trim());
                if (null == colType)
                    ThrowInvalidDataStream(string.Format(
                        "Header: '{0}' is not a valid type",
                        colNameAndType[1].Trim()));

                metadata.FieldNames[j] = colName;
                metadata.FieldTypes[j] = colType;
            }

            metadata.DefaultField = (short)metadata.FieldNames
                .Select(item => item.ToUpperInvariant()).ToList()
                .IndexOf(defaultColName.ToUpperInvariant());
            if (metadata.DefaultField < 0)
                ThrowInvalidDataStream("Header: Default field definition not found");
        }

        protected static object[] ReadItem(TextReader reader, Type[] dimTypes)
        {
            object[] item = new object[dimTypes.Length];
            string[] itemParts = SplitStringAndCheck(reader.ReadLine(), "\t", dimTypes.Length, StringSplitOptions.None);
            for (int j = 0; j < dimTypes.Length; j++)
            {
                item[j] = SqlUtility.ToObject(itemParts[j], dimTypes[j]);
            }
            return item;
        }

        protected static void WriteHeader(TextWriter writer, Metadata metadata)
        {
            writer.WriteLine(metadata.Desc ?? string.Empty);
            writer.WriteLine(string.Format("{0} {1}", metadata.FieldNames.Length, Header_FieldCount_Identifier));
            writer.WriteLine(string.Format("{0} {1}", metadata.ItemCount, Header_ItemCount_Identifier));
            writer.WriteLine(string.Format("{0}={1}", Header_DefaultDim_Identifier, metadata.FieldNames[metadata.DefaultField]));
            for (short j = 0; j < metadata.FieldNames.Length; j++)
            {
                writer.Write("{0}::{1}", metadata.FieldNames[j], metadata.FieldTypes[j].FullName);
                if (j < metadata.FieldNames.Length - 1)
                    writer.Write("\t");
                else
                    writer.WriteLine();
            }
        }

        protected static void WriteItem(TextWriter writer, object[] item)
        {
            for (short j = 0; j < item.Length; j++)
            {
                writer.Write(item[j].ToString());
                if (j < item.Length - 1)
                    writer.Write("\t");
                else
                    writer.WriteLine();
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
