﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Data.IO
{
    public class Metadata
    {
        private const int DefaultPageSize = 10000;

        internal Metadata(string folderName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentNullException("folderName");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("fileName");

            this.FolderName = folderName;
            this.FileName = fileName;

            this.PageSize = DefaultPageSize;
        }

        public string   FolderName      { get; private set;  }
        public string   FileName        { get; private set;  }
        public string[] FieldNames      { get; internal set; }
        public Type[]   FieldTypes      { get; internal set; }
        public short    DefaultField    { get; internal set; }
        public long     ItemCount       { get; internal set; }
        public string   Desc            { get; internal set; }

        public int      PageSize        { get; internal set; }

        public bool SchemaEquals(Metadata other)
        {
            if (null == other)
                throw new ArgumentNullException("other");

            bool schemaEqual = false;
            if (this.FieldNames.Length == other.FieldNames.Length)
            {
                var indexes = Enumerable.Range(0, this.FieldNames.Length);
                schemaEqual = indexes.All(j => this.FieldNames[j] == other.FieldNames[j])
                           && indexes.All(j => this.FieldTypes[j] == other.FieldTypes[j]);
            }
            return schemaEqual;
        }

        public void CopySchema(Metadata other)
        {
            this.FieldNames = other.FieldNames;
            this.FieldTypes = other.FieldTypes;
        }
    }
}
