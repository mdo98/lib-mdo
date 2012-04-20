using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MDo.Data.Corpus
{
    public class TextClassDataReader : IClassDataProvider
    {
        #region Constants

        protected const string Extension = ".rc-class";
        protected const string Header_DimensionCount_Identifier = "dims";
        protected const string Header_ItemCount_Identifier      = "items";
        protected const string Header_DefaultDim_Identifier     = "defaultdim";

        #endregion Constants


        #region Fields

        private string _baseDir;

        #endregion Fields


        public TextClassDataReader(string baseDir)
        {
            this.BaseDir = baseDir;
        }


        #region Properties

        public string BaseDir
        {
            get
            {
                return _baseDir;
            }

            set
            {
                if (null == value)
                    throw new ArgumentNullException("this.BaseDir");
                if (!Directory.Exists(value))
                    throw new DirectoryNotFoundException(string.Format("Folder '{0}' doesn't exist.", value));
                _baseDir = value;
            }
        }

        #endregion Properties


        #region IClassDataProvider
        
        public string[] ListClasses()
        {
            ISet<string> classes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] files = Directory.GetFiles(this.BaseDir, string.Format("*{0}", Extension));
            foreach (string file in files)
            {
                string[] pathParts = file.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                string className = SplitStringAndCheck(pathParts[pathParts.Length-1], ".", 3)[0];
                classes.Add(className);
            }
            return classes.ToArray();
        }

        public string[] ListVariants(string className)
        {
            ISet<string> variants = new SortedSet<string>();
            string[] files = Directory.GetFiles(this.BaseDir, string.Format("{0}.*{1}", className, Extension));
            foreach (string file in files)
            {
                string[] pathParts = file.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                string variant = SplitStringAndCheck(pathParts[pathParts.Length-1], ".", 3)[1];
                variants.Add(variant);
            }
            return variants.ToArray();
        }

        public virtual ClassMetadata GetMetadata(string className, string variantName)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            ClassMetadata metadata = new ClassMetadata(className, variantName);

            // Get metadata for variant
            string fileName = Path.Combine(this.BaseDir, string.Format("{0}.{1}{2}", className, variantName, Extension));
            using (Stream inStream = File.OpenRead(fileName))
            {
                using (TextReader reader = new StreamReader(inStream))
                {
                    ReadHeader(reader, metadata);
                }
            }

            // List all variants
            metadata.VariantNames = this.ListVariants(className);

            return metadata;
        }

        public virtual object[] GetItem(string className, string variantName, int indx)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            if (indx < 0)
                throw new ArgumentOutOfRangeException("indx");

            variantName = ClassMetadata.FixVariantName(variantName);

            string fileName = Path.Combine(this.BaseDir, string.Format("{0}.{1}{2}", className, variantName, Extension));
            object[] item;
            using (Stream inStream = File.OpenRead(fileName))
            {
                using (TextReader reader = new StreamReader(inStream))
                {
                    ClassMetadata metadata = new ClassMetadata(className, variantName);
                    ReadHeader(reader, metadata);

                    for (int i = 0; i < indx; i++)
                        reader.ReadLine();

                    item = ReadItem(reader, metadata.DimensionTypes);
                }
            }
            return item;
        }

        #endregion IClassDataProvider


        #region Internal Ops

        protected static void ReadHeader(TextReader reader, ClassMetadata metadata)
        {
            metadata.Desc = reader.ReadLine();

            int numCols = 0;
            while (true)
            {
                string[] parts = SplitStringAndCheck(reader.ReadLine(), " ", 2);
                int num = int.Parse(parts[0].Trim());
                switch (parts[1].Trim().ToLowerInvariant())
                {
                    case Header_DimensionCount_Identifier:
                        if (num < 0 || num > (int)short.MaxValue)
                            ThrowInvalidDataStream(string.Format(
                                "Header: '{0}' must be between [{1}->{2}]",
                                Header_DimensionCount_Identifier,
                                0,
                                short.MaxValue));
                        numCols = num;
                        break;

                    case Header_ItemCount_Identifier:
                        metadata.NumItems = num;
                        break;

                    default:
                        ThrowInvalidDataStream(string.Format(
                            "Header: '{0}'/'{1}' expected, found '{2}'",
                            Header_DimensionCount_Identifier,
                            Header_ItemCount_Identifier,
                            parts[1]));
                        break;
                }
                if (numCols > 0 && metadata.NumItems > 0)
                {
                    metadata.DimensionNames = new string[numCols];
                    metadata.DimensionTypes = new Type[numCols];
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
            for (int j = 0; j < numCols; j++)
            {
                string[] colNameAndType = SplitStringAndCheck(colMetadata[j], "::", 2);
                string colName = colNameAndType[0].Trim();
                if (colNames.Contains(colName))
                    ThrowInvalidDataStream(string.Format(
                        "Header: '{0}' is applied to more than one dimension",
                        colName));

                metadata.DimensionNames[j] = colName;
                metadata.DimensionTypes[j] = Type.GetType(colNameAndType[1].Trim());
            }

            metadata.DefaultDim = (short)metadata.DimensionNames
                .Select(item => item.ToUpperInvariant()).ToList()
                .IndexOf(defaultColName.ToUpperInvariant());
            if (metadata.DefaultDim < 0)
                ThrowInvalidDataStream("Header: Default dimension not found");
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

        #endregion Internal Ops
    }
}
