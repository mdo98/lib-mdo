using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zlib.DotZLib
{
    public abstract class DeflateStream : Stream
    {
        protected bool _disposed = false;
        protected void DisposedCheck()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().FullName);
        }
    }

    public class DeflateEncodeStream : DeflateStream
    {
        private Deflater _deflater;
        private Stream _output;

        public DeflateEncodeStream(Stream outStream, CompressLevel compressionLevel = CompressLevel.Best)
        {
            if (!outStream.CanWrite)
            {
                throw new ArgumentException("The specified stream cannot write.", "outStream");
            }
            _output = outStream;

            _deflater = new Deflater(compressionLevel);
            _deflater.DataAvailable += new DataAvailableHandler(Deflater_DataAvailable);
        }


        #region InternalOps

        private void Deflater_DataAvailable(byte[] data, int startIndex, int count)
        {
            _output.Write(data, startIndex, count);
        }

        #endregion InternalOps


        #region Stream

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                this.Flush();
                _deflater.Dispose();
                _deflater = null;
                _output = null;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            DisposedCheck();
            _deflater.Finish();
        }

        public override long Length
        {
            get { DisposedCheck(); return _output.Length; }
        }

        public override long Position
        {
            get { DisposedCheck(); return _output.Position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            _deflater.Add(buffer, offset, count);
        }

        #endregion Stream
    }

    public class DeflateDecodeStream : DeflateStream
    {
        private const int DefaultBufferSize = 65536;

        private Inflater _inflater;
        private Stream _input;
        private readonly MemoryStream _bufStream = new MemoryStream();
        private byte[] _buffer = new byte[DefaultBufferSize];

        public DeflateDecodeStream(Stream deflateEncodedStream)
        {
            if (!deflateEncodedStream.CanRead)
            {
                throw new ArgumentException("The specified stream cannot read.", "deflateEncodedStream");
            }
            _input = deflateEncodedStream;

            _inflater = new Inflater();
            _inflater.DataAvailable += new DataAvailableHandler(Inflater_DataAvailable);

            this.ReadToBuffer();
        }


        #region InternalOps

        private void Inflater_DataAvailable(byte[] data, int startIndex, int count)
        {
            long position = _bufStream.Position;
            _bufStream.Seek(0L, SeekOrigin.End);
            _bufStream.Write(data, startIndex, count);
            _bufStream.Position = position;
        }

        private void ReadToBuffer()
        {
            while (_bufStream.Length == _bufStream.Position)
            {
                int bufLength = _input.Read(_buffer, 0, _buffer.Length);
                if (bufLength > 0)
                {
                    _inflater.Add(_buffer, 0, bufLength);
                }
                else
                {
                    _inflater.Finish();
                    break;
                }
            }
        }

        #endregion InternalOps


        #region Stream

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _inflater.Dispose();
                _inflater = null;
                _bufStream.Close();
                _buffer = null;
                _input = null;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            // No-Ops
        }

        public override long Length
        {
            get { DisposedCheck(); return _input.Length; }
        }

        public override long Position
        {
            get { DisposedCheck(); return _input.Position; }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            int dataLength = Math.Min(buffer.Length - offset, count);
            int length;
            int numBytesRead = 0;
            while (dataLength >= (length = (int)(_bufStream.Length - _bufStream.Position)))
            {
                if (length <= 0)
                    break;
                length = _bufStream.Read(buffer, offset, length);
                numBytesRead += length;
                offset += length;
                dataLength -= length;
                _bufStream.Position = 0L;
                _bufStream.SetLength(0L);
                this.ReadToBuffer();
            }
            numBytesRead += _bufStream.Read(buffer, offset, Math.Min(length, dataLength));
            return numBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion Stream
    }
}
