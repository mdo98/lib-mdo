using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ionic.BZip2;
using Ionic.Zlib;

namespace MDo.Common.IO
{
    [Flags]
    public enum AlgorithmImplementation : int
    {
        Managed = 0,
        Native  = 1,
    }

    public enum CompressionAlgorithm : int
    {
        None        = 0,
        Deflate     = 0x02,
        DeflateN    = Deflate   | AlgorithmImplementation.Native,
        Zlib        = 0x04,
        BZip2       = 0x08,
        /*
        Lzma        = 0x0A,
        LzmaN       = Lzma      | AlgorithmImplementation.Native,
        */
        Default     = DeflateN,
    }

    public class DataEncodeStream : DataStream
    {
        public DataEncodeStream(Stream outStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            switch (compressionAlgorithm)
            {
                case CompressionAlgorithm.Deflate:
                    _stream = new Ionic.Zlib.DeflateStream(outStream, CompressionMode.Compress, CompressionLevel.BestCompression, true);
                    break;

                case CompressionAlgorithm.DeflateN:
                    _stream = new Zlib.DotZLib.DeflateEncodeStream(outStream);
                    break;

                case CompressionAlgorithm.Zlib:
                    _stream = new ZlibStream(outStream, CompressionMode.Compress, CompressionLevel.BestCompression, true);
                    break;

                case CompressionAlgorithm.BZip2:
                    _stream = new BZip2OutputStream(outStream, 9, true);
                    break;

                case CompressionAlgorithm.None:
                    _stream = outStream;
                    _leaveOpen = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("compressionAlgorithm");
            }
        }
    }

    public class DataDecodeStream : DataStream
    {
        public DataDecodeStream(Stream inStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.Default)
        {
            switch (compressionAlgorithm)
            {
                case CompressionAlgorithm.Deflate:
                    _stream = new Ionic.Zlib.DeflateStream(inStream, CompressionMode.Decompress, CompressionLevel.BestCompression, true);
                    break;

                case CompressionAlgorithm.DeflateN:
                    _stream = new Zlib.DotZLib.DeflateDecodeStream(inStream);
                    break;

                case CompressionAlgorithm.Zlib:
                    _stream = new ZlibStream(inStream, CompressionMode.Decompress, CompressionLevel.BestCompression, true);
                    break;

                case CompressionAlgorithm.BZip2:
                    _stream = new BZip2InputStream(inStream, true);
                    break;

                case CompressionAlgorithm.None:
                    _stream = inStream;
                    _leaveOpen = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("compressionAlgorithm");
            }
        }
    }

    public abstract class DataStream : Stream
    {
        protected Stream _stream;
        protected bool _leaveOpen = false;

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position;  }
            set { _stream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (_leaveOpen == false)
                _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
