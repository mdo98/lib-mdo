using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Data.Corpus.DataImport
{
    public class ClassDataTextCachedImporter : ClassDataTextImporter
    {
        #region Fields

        private string _className;
        private string _variantName;

        #endregion Fields


        public ClassDataTextCachedImporter(string baseDir, string className, string variantName) : base(baseDir)
        {
            this.ClassName = className;
            this.VariantName = ClassMetadata.FixVariantName(variantName);
        }


        #region Properties

        public string ClassName
        {
            get
            {
                return _className;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException("this.ClassName");
                _className = value;
            }
        }

        public string VariantName
        {
            get
            {
                return _variantName;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _variantName = ClassMetadata.DefaultVariantName;
                else
                    _variantName = value;
            }
        }

        public ClassMetadata CachedMetadata { get; private set; }
        public IList<object[]> CachedItems  { get; private set; }

        #endregion Properties


        public override ClassMetadata GetMetadata(string className, string variantName)
        {
            if (!className.Equals(this.ClassName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("className");

            variantName = ClassMetadata.FixVariantName(variantName);

            if (!variantName.Equals(this.VariantName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("variantName");

            if (this.CachedMetadata == null)
                this.CachedMetadata = base.GetMetadata(className, variantName);
            return this.CachedMetadata;
        }

        public override object[] GetItem(string className, string variantName, int indx)
        {
            if (!className.Equals(this.ClassName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("className");

            if (indx < 0)
                throw new ArgumentOutOfRangeException("indx");

            variantName = ClassMetadata.FixVariantName(variantName);

            if (!variantName.Equals(this.VariantName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("variantName");

            if (this.CachedItems == null)
            {
                this.CachedItems = new List<object[]>();

                string fileName = Path.Combine(this.BaseDir, string.Format("{0}.{1}{2}", this.ClassName, this.VariantName, Extension));
                using (Stream inStream = File.OpenRead(fileName))
                {
                    using (TextReader reader = new StreamReader(inStream))
                    {
                        ClassMetadata metadata = new ClassMetadata(className, variantName);
                        ReadHeader(reader, metadata);

                        for (int i = 0; i < metadata.Count; i++)
                        {
                            this.CachedItems.Add(ReadItem(reader, metadata.DimensionTypes));
                        }
                    }
                }
            }
            
            if (indx >= this.CachedItems.Count)
                throw new ArgumentOutOfRangeException("indx");

            return this.CachedItems[indx];
        }
    }
}
