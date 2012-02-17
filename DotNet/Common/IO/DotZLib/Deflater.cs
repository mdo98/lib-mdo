//
// © Copyright Henrik Ravn 2004
//
// Use, modification and distribution are subject to the Boost Software License, Version 1.0.
// (See accompanying file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Zlib.DotZLib
{

    /// <summary>
    /// Implements a data compressor, using the deflate algorithm in the ZLib dll
    /// </summary>
	public sealed class Deflater : CodecBase
	{
        #region Dll imports
        [DllImport(Info.NativeZlibPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int deflateInit_(ref ZStream sz, int level, string vs, int size);

        [DllImport(Info.NativeZlibPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int deflate(ref ZStream sz, int flush);

        [DllImport(Info.NativeZlibPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int deflateReset(ref ZStream sz);

        [DllImport(Info.NativeZlibPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int deflateEnd(ref ZStream sz);
        #endregion

        /// <summary>
        /// Constructs an new instance of the <c>Deflater</c>
        /// </summary>
        /// <param name="level">The compression level to use for this <c>Deflater</c></param>
		public Deflater(CompressLevel level) : base()
		{
            int retval = deflateInit_(ref _zstream, (int)level, Info.Version, Marshal.SizeOf(_zstream));
            if (retval != 0)
                throw new ZLibException(retval, "Could not initialize deflater");

            resetOutput();
		}

        /// <summary>
        /// Adds more data to the codec to be processed.
        /// </summary>
        /// <param name="data">Byte array containing the data to be added to the codec</param>
        /// <param name="offset">The index of the first byte to add from <c>data</c></param>
        /// <param name="count">The number of bytes to add</param>
        /// <remarks>Adding data may, or may not, raise the <c>DataAvailable</c> event</remarks>
        public override void Add(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();
            if ((offset+count) > data.Length) throw new ArgumentException();

            int total = count;
            int inputIndex = offset;
            int err = 0;

            while (err >= 0 && inputIndex < total)
            {
                copyInput(data, inputIndex, Math.Min(total - inputIndex, kBufferSize));
                while (err >= 0 && _zstream.avail_in > 0)
                {
                    err = deflate(ref _zstream, (int)FlushTypes.None);
                    if (err == 0)
                        while (_zstream.avail_out == 0)
                        {
                            OnDataAvailable();
                            err = deflate(ref _zstream, (int)FlushTypes.None);
                        }
                    inputIndex += (int)_zstream.total_in;
                }
            }
            setChecksum( _zstream.adler );
        }


        /// <summary>
        /// Finishes up any pending data that needs to be processed and handled.
        /// </summary>
        public override void Finish()
        {
            int err;
            do
            {
                err = deflate(ref _zstream, (int)FlushTypes.Finish);
                OnDataAvailable();
            }
            while (err == 0);
            setChecksum( _zstream.adler );
            deflateReset(ref _zstream);
            resetOutput();
        }

        /// <summary>
        /// Closes the internal zlib deflate stream
        /// </summary>
        protected override void CleanUp() { deflateEnd(ref _zstream); }

    }
}
