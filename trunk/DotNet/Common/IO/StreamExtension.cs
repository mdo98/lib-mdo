using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MDo.Common.IO
{
    public static class StreamExtension
    {
        public const int DefaultBufferSize = 1 << 16;  // 64K

        public static void Transfer(this Stream inStream, Stream bufferStream, int bufferSize = DefaultBufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int length;
            while ((length = inStream.Read(buffer, 0, bufferSize)) > 0)
            {
                bufferStream.Write(buffer, 0, length);
            }
        }

        public static MemoryStream TransferToMemory(this Stream stream, int bufferSize = DefaultBufferSize)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.Transfer(memoryStream, bufferSize);
            return memoryStream;
        }

        public static byte[] ToByteArray(this Stream stream, int bufferSize = DefaultBufferSize)
        {
            byte[] buffer;
            using (MemoryStream bufStream = stream.TransferToMemory(bufferSize))
            {
                buffer = bufStream.ToArray();
            }
            return buffer;
        }
    }
}
