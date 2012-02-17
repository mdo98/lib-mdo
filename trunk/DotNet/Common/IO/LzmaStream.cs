using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MDo.Common.IO;

namespace SevenZip.Compression.LZMA
{
    public abstract class LzmaStream : Stream
    {
        public const int MaxBufferSize      = 1 << 30;  // 1048576K
        public const int MinBufferSize      = 1 << 18;  //     256K
        public const int DefaultBufferSize  = 1 << 24;  //   16384K

        private const int NumProcessedBytesLength = sizeof(long);

        protected long _numProcessedBytes = 0;
        protected bool _disposed = false;

        protected readonly MemoryStream _buffer = new MemoryStream(DefaultBufferSize);

        protected void DisposedCheck()
        {
            if (_disposed)
                throw new ObjectDisposedException(this.GetType().FullName);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _buffer.Close();
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public static long Encode(LzmaEncoder encoder, Stream inStream, Stream outStream)
        {
            byte[] input = inStream.ToByteArray();
            byte[] output;
            long inSize = input.LongLength, outSize = -1;
            encoder.Code(input, out output, ref inSize, ref outSize);
            (new MemoryStream(output)).Transfer(outStream);
            return outSize;
        }

        public static long Decode(LzmaDecoder decoder, Stream inStream, Stream outStream, long expectedOutputLength)
        {
            byte[] input = inStream.ToByteArray();
            byte[] output;
            long inSize = 0, outSize = expectedOutputLength;
            decoder.Code(input, out output, ref inSize, ref outSize);
            (new MemoryStream(output)).Transfer(outStream);
            return outSize;
        }

        protected static long ReadNumProcessedBytes(Stream inStream, bool restoreStreamPosition)
        {
            long position = inStream.Position;
            byte[] numProcessedBytesArray = new byte[NumProcessedBytesLength];
            inStream.Position = LzmaCoder.LzmaPropertiesLengthInBytes;
            inStream.Read(numProcessedBytesArray, 0, NumProcessedBytesLength);
            long numProcessedBytes = BitConverter.ToInt64(numProcessedBytesArray, 0);
            if (restoreStreamPosition)
                inStream.Position = position;
            return numProcessedBytes;
        }

        protected static void WriteNumProcessedBytes(long numProcessedBytes, Stream outStream, bool restoreStreamPosition)
        {
            long position = outStream.Position;
            byte[] numProcessedBytesArray = BitConverter.GetBytes(numProcessedBytes);
            outStream.Position = LzmaCoder.LzmaPropertiesLengthInBytes;
            outStream.Write(numProcessedBytesArray, 0, NumProcessedBytesLength);
            if (restoreStreamPosition)
                outStream.Position = position;
        }

        protected static void Init(LzmaEncoder encoder, Stream outStream, long srcLen)
        {
            if (!encoder.LzmaHeaderManagedInternally)
            {
                encoder.WriteLzmaProperties(outStream);
                WriteNumProcessedBytes(srcLen, outStream, false);
            }
        }

        protected static long Init(LzmaDecoder decoder, Stream inStream)
        {
            long unpackedLen = 0;
            if (!decoder.LzmaHeaderManagedInternally)
            {
                decoder.SetLzmaProperties(LzmaDecoder.ReadLzmaProperties(inStream));
                unpackedLen = ReadNumProcessedBytes(inStream, false);
            }
            return unpackedLen;
        }

        public static long Compress(Stream inStream, Stream outStream, LzmaEncoder encoder, int dictionarySize = LzmaCoder.DefaultLzmaDictionarySize, int wordSize = LzmaCoder.DefaultLzmaWordSize)
        {
            Init(encoder, outStream, inStream.Length);
            return Encode(encoder, inStream, outStream);
        }

        public static long Decompress(Stream inStream, Stream outStream, LzmaDecoder decoder)
        {
            long numProcessedBytes = Init(decoder, inStream);
            return Decode(decoder, inStream, outStream, numProcessedBytes);
        }
    }

    public class LzmaEncodeStream : LzmaStream
    {
        private Stream _output;
        private readonly LzmaEncoder _lzmaEncoder;


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LzmaEncodeStream class.
        /// </summary>
        /// <param name="outStream">An output stream which supports writing.</param>
        /// <param name="encoder"></param>
        /// <param name="bufferSize"></param>
        public LzmaEncodeStream(Stream outStream, LzmaEncoder encoder, int bufferSize = DefaultBufferSize)
        {
            if (!outStream.CanWrite)
            {
                throw new ArgumentException("The specified stream cannot write.", "outStream");
            }
            _output = outStream;

            if (bufferSize < MinBufferSize || bufferSize > MaxBufferSize)
            {
                throw new ArgumentOutOfRangeException(
                    "bufferSize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinBufferSize,
                        MaxBufferSize));
            }
            _buffer.Capacity = bufferSize;

            _lzmaEncoder = encoder;
            Init(_lzmaEncoder, _output, _numProcessedBytes);
        }

        #endregion Constructors


        #region InternalOps

        private void WriteChunk()
        {
            _buffer.SetLength(_buffer.Position);
            _buffer.Position = 0L;
            Encode(_lzmaEncoder, _buffer, _output);
            _numProcessedBytes += _buffer.Length;
            _buffer.Position = 0L;
        }

        #endregion InternalOps


        #region Stream

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get { DisposedCheck(); return _output.Length; }
        }

        /// <summary>
        /// Gets or sets the position within the stream.
        /// </summary>
        public override long Position
        {
            get { DisposedCheck(); return _output.Position; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Releases all resources used by LzmaEncodeStream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                this.Flush();
                _output = null;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be compressed and written.
        /// </summary>
        public override void Flush()
        {
            DisposedCheck();
            this.WriteChunk();
            if (!_lzmaEncoder.LzmaHeaderManagedInternally)
                WriteNumProcessedBytes(_numProcessedBytes, _output, false);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and compresses it if necessary.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to read from the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            int dataLength = Math.Min(buffer.Length - offset, count);
            int length;
            while (dataLength >= (length = _buffer.Capacity - (int)_buffer.Position))
            {
                _buffer.Write(buffer, offset, length);
                this.WriteChunk();
                offset += length;
                dataLength -= length;
            }
            _buffer.Write(buffer, offset, dataLength);
        }

        #endregion Stream
    }

    public class LzmaDecodeStream : LzmaStream
    {
        private readonly LzmaDecoder _lzmaDecoder;
        private Stream _input;


        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LzmaDecodeStream class.
        /// </summary>
        /// <param name="lzmaEncodedStream">A compressed stream.</param>
        /// <param name="decoder"></param>
        /// <param name="bufferSize"></param>
        public LzmaDecodeStream(Stream lzmaEncodedStream, LzmaDecoder decoder, int bufferSize = DefaultBufferSize)
        {
            if (!lzmaEncodedStream.CanRead)
            {
                throw new ArgumentException("The specified stream cannot read.", "lzmaEncodedStream");
            }
            _input = lzmaEncodedStream;

            if (bufferSize < MinBufferSize || bufferSize > MaxBufferSize)
            {
                throw new ArgumentOutOfRangeException(
                    "bufferSize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinBufferSize,
                        MaxBufferSize));
            }
            _buffer.Capacity = bufferSize;

            _lzmaDecoder = decoder;
            _numProcessedBytes = Init(_lzmaDecoder, _input);

            this.ReadChunk();
        }

        #endregion Constructors


        #region InternalOps

        private void ReadChunk()
        {
            _buffer.Position = 0L;
            long numProcessedBytes;
            if (_lzmaDecoder.LzmaHeaderManagedInternally)
            {
                numProcessedBytes = 0L;
                if (_input.Position < _input.Length)
                {
                    numProcessedBytes = Decode(_lzmaDecoder, _input, _buffer, numProcessedBytes);
                }
            }
            else
            {
                numProcessedBytes = Math.Min(_numProcessedBytes, (long)_buffer.Capacity);
                numProcessedBytes = Decode(_lzmaDecoder, _input, _buffer, numProcessedBytes);
                _numProcessedBytes -= numProcessedBytes;
            }
            _buffer.Position = 0L;
            _buffer.SetLength(numProcessedBytes);
        }

        #endregion InternalOps


        #region Stream

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get { DisposedCheck(); return _input.Length; }
        }

        /// <summary>
        /// Gets or sets the position within the stream.
        /// </summary>
        public override long Position
        {
            get { DisposedCheck(); return _input.Position; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Releases all resources used by LzmaDecodeStream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _input = null;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Flush() { }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and decompresses data if necessary.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>        
        public override int Read(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            int dataLength = Math.Min(buffer.Length - offset, count);
            int length;
            int numBytesRead = 0;
            while (dataLength >= (length = (int)(_buffer.Length - _buffer.Position)))
            {
                if (length <= 0)
                    break;
                length = _buffer.Read(buffer, offset, length);
                numBytesRead += length;
                offset += length;
                dataLength -= length;
                this.ReadChunk();
            }
            numBytesRead += _buffer.Read(buffer, offset, Math.Min(length, dataLength));
            return numBytesRead;
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>       
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion Stream
    }
}
