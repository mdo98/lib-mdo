﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MDo.Common.IO
{
    public static partial class FS
    {
        #region Binary serialization

        public static void Serialize(this object obj, Stream outStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            using (DataStream dataStream = new DataEncodeStream(outStream, compressionAlgorithm))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(dataStream, obj);
            }
        }

        public static void Serialize(this object obj, string fileName, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            using (Stream outStream = OpenWrite(fileName))
            {
                obj.Serialize(outStream, compressionAlgorithm);
            }
        }

        public static T Deserialize<T>(Stream inStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            T obj = default(T);
            using (DataStream dataStream = new DataDecodeStream(inStream, compressionAlgorithm))
            {
                BinaryFormatter bf = new BinaryFormatter();
                obj = (T)bf.Deserialize(dataStream);
            }
            return obj;
        }

        public static T Deserialize<T>(string fileName, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            T obj = default(T);
            using (Stream inStream = OpenRead(fileName))
            {
                obj = Deserialize<T>(inStream, compressionAlgorithm);
            }
            return obj;
        }

        #endregion Binary serialization


        #region XML serialization

        public static string ToXml(this object obj, XmlWriterSettings xmlWriterSettings)
        {
            StringBuilder xmlBuffer = new StringBuilder();
            using (XmlWriter xmlWriter = XmlWriter.Create(xmlBuffer, xmlWriterSettings))
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                serializer.Serialize(xmlWriter, obj);
            }
            return xmlBuffer.ToString();
        }

        public static string ToCompactXml(this object obj)
        {
            return obj.ToXml(new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = false,
            });
        }

        public static string ToIndentedXml(this object obj)
        {
            return obj.ToXml(new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
        }

        public static void ToXmlStream(this object obj, Stream outStream, XmlWriterSettings xmlWriterSettings)
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(outStream, xmlWriterSettings))
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                serializer.Serialize(xmlWriter, obj);
            }
        }

        public static void ToXmlFile(this object obj, string xmlFilePath, XmlWriterSettings xmlWriterSettings)
        {
            using (Stream output = OpenWrite(xmlFilePath))
            {
                obj.ToXmlStream(output, xmlWriterSettings);
            }
        }

        public static void ToCompactXmlFile(this object obj, string xmlFilePath)
        {
            obj.ToXmlFile(xmlFilePath, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = false,
            });
        }

        public static void ToIndentedXmlFile(this object obj, string xmlFilePath)
        {
            obj.ToXmlFile(xmlFilePath, new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
            });
        }

        public static T FromXml<T>(TextReader xmlBuffer)
        {
            T obj = default(T);
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            obj = (T)serializer.Deserialize(xmlBuffer);
            return obj;
        }

        public static T FromXml<T>(string xml)
        {
            T obj = default(T);
            if (!string.IsNullOrWhiteSpace(xml))
            {
                using (TextReader serializationInput = new StringReader(xml))
                {
                    obj = FromXml<T>(serializationInput);
                }
            }
            return obj;
        }

        public static T FromXml<T>(Stream xmlStream)
        {
            T obj = default(T);
            using (TextReader serializationInput = new StreamReader(xmlStream))
            {
                obj = FromXml<T>(serializationInput);
            }
            return obj;
        }

        public static T FromXmlFile<T>(string xmlFilePath)
        {
            T obj = default(T);
            if (!string.IsNullOrWhiteSpace(xmlFilePath))
            {
                using (Stream input = OpenRead(xmlFilePath))
                {
                    obj = FromXml<T>(input);
                }
            }
            return obj;
        }

        #endregion XML serialization
    }
}
