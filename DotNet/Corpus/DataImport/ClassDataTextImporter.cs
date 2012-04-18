using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MDo.Data.Corpus.DataImport
{
    public class ClassDataTextImporter : IClassDataImporter
    {
        #region Constants

        protected const string Extension = ".rc-class";

        #endregion Constants


        #region Fields

        private string _baseDir;

        #endregion Fields


        public ClassDataTextImporter(string baseDir)
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


        #region IClassDataImporter

        public void Import(string className, string variantName, IClassDataManager destStore)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentNullException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            ClassMetadata metadata = this.GetMetadata(className, variantName);
            if (destStore.VariantExists(className, variantName))
            {
                ClassMetadata destMetadata = destStore.GetMetadata(className, variantName);

                // Compare metadata
                bool metaEqual = false;
                if (metadata.DimensionNames.Length == destMetadata.DimensionNames.Length)
                {
                    var indexes = Enumerable.Range(0, metadata.DimensionNames.Length);
                    metaEqual = indexes.All(j => metadata.DimensionNames[j] == destMetadata.DimensionNames[j])
                        && indexes.All(j => metadata.DimensionTypes[j] == destMetadata.DimensionTypes[j]);
                }

                if (metaEqual)
                {
                    destStore.ClearItems(className, variantName);
                }
                else
                {
                    destStore.RemoveVariant(className, variantName);
                    destStore.AddVariant(metadata);
                }
            }
            else
            {
                destStore.AddVariant(metadata);
            }

            for (int i = 0; i < metadata.Count; i++)
            {
                destStore.AddItem(metadata, this.GetItem(className, variantName, i));
            }
        }

        #endregion IClassDataImporter


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
                    case "dims":
                        numCols = num;
                        break;

                    case "items":
                        metadata.Count = num;
                        break;

                    default:
                        ThrowInvalidDataStream();
                        break;
                }
                if (numCols > 0 && metadata.Count > 0)
                {
                    metadata.DimensionNames = new string[numCols];
                    metadata.DimensionTypes = new Type[numCols];
                    break;
                }
            }

            string[] colMetadata = SplitStringAndCheck(reader.ReadLine(), "\t", numCols);
            for (int j = 0; j < numCols; j++)
            {
                string[] colNameAndType = SplitStringAndCheck(colMetadata[j], "::", 2);
                metadata.DimensionNames[j] = colNameAndType[0];
                metadata.DimensionTypes[j] = Type.GetType(colNameAndType[1]);
            }
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
                ThrowInvalidDataStream();

            string[] parts = line.Split(new string[] { splitStr }, splitOptions);
            if (parts.Length != numParts)
                ThrowInvalidDataStream();

            return parts;
        }

        private static void ThrowInvalidDataStream()
        {
            throw new InvalidDataException("Input stream does not have the expected format.");
        }

        #endregion Internal Ops
    }
}
