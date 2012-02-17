using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using MDo.Common.IO;

namespace SevenZip.Compression.LZMA
{
    public abstract class LzmaCoder
    {
        public const int MaxLzmaDictionarySize = 1 << 30;      // 1024M
        public const int MinLzmaDictionarySize = 1 << 20;      //    1M
        public const int DefaultLzmaDictionarySize = 1 << 25;  //   32M

        public const int MaxLzmaWordSize = 273;
        public const int MinLzmaWordSize = 5;
        public const int DefaultLzmaWordSize = 256;

        public const int LzmaPropertiesLengthInBytes = 5;

#if NATIVE64
        protected const string NativeLzmaLibraryPath = @"lib\LZMA64.dll";
#else
        protected const string NativeLzmaLibraryPath = @"lib\LZMA32.dll";
#endif

        public abstract bool LzmaHeaderManagedInternally { get; }
        public abstract void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize);

        protected static void CheckNativeReturnCode(int code)
        {
            switch (code)
            {
                case (int)NativeLzmaReturnCode.Normal:
                    break;

                case (int)NativeLzmaReturnCode.DataError:
                    throw new DataErrorException();

                case (int)NativeLzmaReturnCode.MemAllocError:
                    throw new OutOfMemoryException();

                case (int)NativeLzmaReturnCode.UnsupportedOps:
                    throw new InvalidOperationException();

                case (int)NativeLzmaReturnCode.ParamError:
                    throw new InvalidParamException();

                case (int)NativeLzmaReturnCode.InputEofError:
                    throw new InternalBufferOverflowException("Input ended unexpectedly.");

                case (int)NativeLzmaReturnCode.OutputEofError:
                    throw new InternalBufferOverflowException("Output buffer too small.");

                case (int)NativeLzmaReturnCode.ThreadingError:
                    throw new ApplicationException();

                default:
                    throw new Exception(string.Format("Native LZMA returned code {0:X}.", code));
            }
        }

        internal enum NativeLzmaReturnCode : int
        {
            Normal = 0,
            DataError = 1,
            MemAllocError = 2,
            UnsupportedOps = 4,
            ParamError = 5,
            InputEofError = 6,
            OutputEofError = 7,
            ThreadingError = 12,
        }
    }

    public abstract class LzmaEncoder : LzmaCoder
    {
        protected static readonly IDictionary<CoderPropID, object> LzmaProperties = new Dictionary<CoderPropID, object>()
        {
            { CoderPropID.DictionarySize,   DefaultLzmaDictionarySize   },
            { CoderPropID.PosStateBits,     2                           },
            { CoderPropID.LitContextBits,   3                           },
            { CoderPropID.LitPosBits,       0                           },
            { CoderPropID.Algorithm,        2                           },
            { CoderPropID.NumFastBytes,     DefaultLzmaWordSize         },
            { CoderPropID.MatchFinder,      "bt4"                       },
            { CoderPropID.EndMarker,        false                       },
        };

        public abstract void SetLzmaProperties(IDictionary<CoderPropID, object> lzmaProperties);
        public abstract void WriteLzmaProperties(Stream outStream);
    }

    public class ManagedLzmaEncoder : LzmaEncoder
    {
        private readonly Encoder _lzmaEncoder = new Encoder();

        public ManagedLzmaEncoder(int dictionarySize = DefaultLzmaDictionarySize, int wordSize = DefaultLzmaWordSize)
        {
            if (dictionarySize < MinLzmaDictionarySize || dictionarySize > MaxLzmaDictionarySize)
            {
                throw new ArgumentOutOfRangeException(
                    "dictionarySize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinLzmaDictionarySize,
                        MaxLzmaDictionarySize));
            }

            if (wordSize < MinLzmaWordSize || wordSize > MaxLzmaWordSize)
            {
                throw new ArgumentOutOfRangeException(
                    "wordSize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinLzmaWordSize,
                        MaxLzmaWordSize));
            }

            var lzmaProperties = new Dictionary<CoderPropID, object>(LzmaProperties);
            lzmaProperties[CoderPropID.DictionarySize] = dictionarySize;
            lzmaProperties[CoderPropID.NumFastBytes] = wordSize;
            this.SetLzmaProperties(lzmaProperties);
        }

        public override bool LzmaHeaderManagedInternally
        {
            get { return false; }
        }

        public override void SetLzmaProperties(IDictionary<CoderPropID, object> lzmaProperties)
        {
            _lzmaEncoder.SetCoderProperties(lzmaProperties.Keys.ToArray(), lzmaProperties.Values.ToArray());
        }

        public override void WriteLzmaProperties(Stream outStream)
        {
            _lzmaEncoder.WriteCoderProperties(outStream);
        }

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
            ManagedLzmaCoderProgress lzmaCoderProgress = new ManagedLzmaCoderProgress();
            using (MemoryStream inStream = new MemoryStream(input),
                                outStream = new MemoryStream())
            {
                _lzmaEncoder.Code(inStream, outStream, -1, -1, lzmaCoderProgress);
                output = outStream.ToArray();
            }
            inSize = lzmaCoderProgress.In;
            outSize = lzmaCoderProgress.Out;
        }

        internal class ManagedLzmaCoderProgress : ICodeProgress
        {
            public long In { get; private set; }
            public long Out { get; private set; }

            public void SetProgress(long inSize, long outSize)
            {
                this.In = inSize;
                this.Out = outSize;
            }
        }
    }

    public class NativeLzmaEncoder : LzmaEncoder
    {
        protected const int NativeLzmaCompressionLevel = 6;
        protected const int NativeLzmaNumThreads = 2;

        protected IDictionary<CoderPropID, object> _lzmaProperties;

        public NativeLzmaEncoder(int dictionarySize = DefaultLzmaDictionarySize, int wordSize = DefaultLzmaWordSize)
        {
            if (dictionarySize < MinLzmaDictionarySize || dictionarySize > MaxLzmaDictionarySize)
            {
                throw new ArgumentOutOfRangeException(
                    "dictionarySize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinLzmaDictionarySize,
                        MaxLzmaDictionarySize));
            }

            if (wordSize < MinLzmaWordSize || wordSize > MaxLzmaWordSize)
            {
                throw new ArgumentOutOfRangeException(
                    "wordSize",
                    string.Format(
                        "Range: [{0} => {1}]",
                        MinLzmaWordSize,
                        MaxLzmaWordSize));
            }

            var lzmaProperties = new Dictionary<CoderPropID, object>(LzmaProperties);
            lzmaProperties[CoderPropID.DictionarySize] = dictionarySize;
            lzmaProperties[CoderPropID.NumFastBytes] = wordSize;
            this.SetLzmaProperties(lzmaProperties);
        }

        public override bool LzmaHeaderManagedInternally
        {
            get { return false; }
        }

        public override void SetLzmaProperties(IDictionary<CoderPropID, object> lzmaProperties)
        {
            _lzmaProperties = lzmaProperties;
        }

        public override void WriteLzmaProperties(Stream outStream)
        {
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(_lzmaProperties.Keys.ToArray(), _lzmaProperties.Values.ToArray());
            encoder.WriteCoderProperties(outStream);
        }

        [DllImport(NativeLzmaLibraryPath, SetLastError = true)]
#if NATIVE64
        protected static extern int LzmaCompress(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref ulong destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ulong srcLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] outProps,
            ref ulong outPropsSize,  /* *outPropsSize must be = 5 */
            int level,              /* 0 <= level <= 9, default = 5 */
            uint dictSize,          /* default = (1 << 24) */
            int lc,                 /* 0 <= lc <= 8, default = 3  */
            int lp,                 /* 0 <= lp <= 4, default = 0  */
            int pb,                 /* 0 <= pb <= 4, default = 2  */
            int fb,                 /* 5 <= fb <= 273, default = 32 */
            int numThreads          /* 1 or 2, default = 2 */
        );
#else
        protected static extern int LzmaCompress(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref uint destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            uint srcLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] outProps,
            ref uint outPropsSize,  /* *outPropsSize must be = 5 */
            int level,              /* 0 <= level <= 9, default = 5 */
            uint dictSize,          /* default = (1 << 24) */
            int lc,                 /* 0 <= lc <= 8, default = 3  */
            int lp,                 /* 0 <= lp <= 4, default = 0  */
            int pb,                 /* 0 <= pb <= 4, default = 2  */
            int fb,                 /* 5 <= fb <= 273, default = 32 */
            int numThreads          /* 1 or 2, default = 2 */
        );
#endif

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
#if NATIVE64
            ulong bufSize = (ulong)GetBufferAllocationSize(input.LongLength);
            byte[] buffer = new byte[bufSize];
            ulong lzmaPropsLen = (ulong)LzmaPropertiesLengthInBytes;
            byte[] lzmaProps = new byte[LzmaPropertiesLengthInBytes];
            int result = LzmaCompress(
                buffer, ref bufSize, input, (ulong)input.LongLength,
                lzmaProps, ref lzmaPropsLen,
                NativeLzmaCompressionLevel,
                (uint)((int)_lzmaProperties[CoderPropID.DictionarySize]),
                (int)_lzmaProperties[CoderPropID.LitContextBits],
                (int)_lzmaProperties[CoderPropID.LitPosBits],
                (int)_lzmaProperties[CoderPropID.PosStateBits],
                (int)_lzmaProperties[CoderPropID.NumFastBytes],
                NativeLzmaNumThreads);
#else
            uint bufSize = (uint)GetBufferAllocationSize(input.LongLength);
            byte[] buffer = new byte[bufSize];
            uint lzmaPropsLen = (uint)LzmaPropertiesLengthInBytes;
            byte[] lzmaProps = new byte[LzmaPropertiesLengthInBytes];
            int result = LzmaCompress(
                buffer, ref bufSize, input, (uint)input.Length,
                lzmaProps, ref lzmaPropsLen,
                NativeLzmaCompressionLevel,
                (uint)((int)_lzmaProperties[CoderPropID.DictionarySize]),
                (int)_lzmaProperties[CoderPropID.LitContextBits],
                (int)_lzmaProperties[CoderPropID.LitPosBits],
                (int)_lzmaProperties[CoderPropID.PosStateBits],
                (int)_lzmaProperties[CoderPropID.NumFastBytes],
                NativeLzmaNumThreads);
#endif
            CheckNativeReturnCode(result);
            outSize = (long)bufSize;
            output = new byte[outSize];
            Array.Copy(buffer, output, outSize);
        }

        protected static long GetBufferAllocationSize(long inputLength)
        {
            if (inputLength <= 0)
                return 0;
            else
                return ((inputLength / 20 + 1) * 21);
        }
    }

    public class NativeLzmaEncoderWithBcjFilter : NativeLzmaEncoder
    {
        protected const int NativeLzmaBcjFilterMode = 2;

        public NativeLzmaEncoderWithBcjFilter(int dictionarySize = DefaultLzmaDictionarySize, int wordSize = DefaultLzmaWordSize)
            : base(dictionarySize, wordSize)
        { }

        public override bool LzmaHeaderManagedInternally
        {
            get { return true; }
        }

        [DllImport(NativeLzmaLibraryPath, SetLastError = true)]
#if NATIVE64
        protected static extern int Lzma86_EncodeWithOptions(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref ulong destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ulong srcLen,
            int level,       /* 0 <= level <= 9, default = 5 */
            uint dictSize,   /* default = (1 << 24) */
            int lc,          /* 0 <= lc <= 8, default = 3  */
            int lp,          /* 0 <= lp <= 4, default = 0  */
            int pb,          /* 0 <= pb <= 4, default = 2  */
            int fb,          /* 5 <= fb <= 273, default = 32 */
            int numThreads,  /* 1 or 2, default = 2 */
            int filterMode
        );
#else
        protected static extern int Lzma86_EncodeWithOptions(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref uint destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            uint srcLen,
            int level,       /* 0 <= level <= 9, default = 5 */
            uint dictSize,   /* default = (1 << 24) */
            int lc,          /* 0 <= lc <= 8, default = 3  */
            int lp,          /* 0 <= lp <= 4, default = 0  */
            int pb,          /* 0 <= pb <= 4, default = 2  */
            int fb,          /* 5 <= fb <= 273, default = 32 */
            int numThreads,  /* 1 or 2, default = 2 */
            int filterMode
        );
#endif

        public static void Encode(Stream inStream, Stream outStream,
            int dictionarySize, int lc, int lp, int pb, int fb)
        {
#if NATIVE64
            byte[] input = inStream.ToByteArray();
            ulong bufSize = (ulong)GetBufferAllocationSize(input.LongLength);
            byte[] buffer = new byte[bufSize];
            int result = Lzma86_EncodeWithOptions(
                buffer, ref bufSize, input, (ulong)input.Length,
                NativeLzmaCompressionLevel,
                (uint)dictionarySize,
                lc,
                lp,
                pb,
                fb,
                NativeLzmaNumThreads,
                NativeLzmaBcjFilterMode);
#else
            byte[] input = inStream.ToByteArray();
            uint bufSize = (uint)GetBufferAllocationSize(input.LongLength);
            byte[] buffer = new byte[bufSize];
            int result = Lzma86_EncodeWithOptions(
                buffer, ref bufSize, input, (uint)input.Length,
                NativeLzmaCompressionLevel,
                (uint)dictionarySize,
                lc,
                lp,
                pb,
                fb,
                NativeLzmaNumThreads,
                NativeLzmaBcjFilterMode);
#endif
            CheckNativeReturnCode(result);
            outStream.Write(buffer, 0, (int)bufSize);
        }

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
            using (MemoryStream inStream = new MemoryStream(input),
                                outStream = new MemoryStream())
            {
                Encode(inStream, outStream,
                    (int)_lzmaProperties[CoderPropID.DictionarySize],
                    (int)_lzmaProperties[CoderPropID.LitContextBits],
                    (int)_lzmaProperties[CoderPropID.LitPosBits],
                    (int)_lzmaProperties[CoderPropID.PosStateBits],
                    (int)_lzmaProperties[CoderPropID.NumFastBytes]);
                output = outStream.ToArray();
                outSize = output.LongLength;
            }
        }
    }

    public abstract class LzmaDecoder : LzmaCoder
    {
        public abstract void SetLzmaProperties(byte[] properties);

        public static byte[] ReadLzmaProperties(Stream inStream)
        {
            byte[] lzmaProperties = new byte[LzmaPropertiesLengthInBytes];
            if (inStream.Read(lzmaProperties, 0, LzmaPropertiesLengthInBytes) != LzmaPropertiesLengthInBytes)
                throw new InvalidDataException("Input stream is too short.");
            return lzmaProperties;
        }
    }

    public class ManagedLzmaDecoder : LzmaDecoder
    {
        private readonly Decoder _lzmaDecoder = new Decoder();

        public ManagedLzmaDecoder()
            : this(new byte[LzmaPropertiesLengthInBytes])
        { }

        public ManagedLzmaDecoder(byte[] lzmaProperties)
        {
            this.SetLzmaProperties(lzmaProperties);
        }

        public override bool  LzmaHeaderManagedInternally
        {
	        get { return false; }
        }

        public override void SetLzmaProperties(byte[] properties)
        {
            _lzmaDecoder.SetDecoderProperties(properties);
        }

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
            using (MemoryStream inStream = new MemoryStream(input),
                                outStream = new MemoryStream())
            {
                _lzmaDecoder.Code(inStream, outStream, 0, outSize, null);
                output = outStream.ToArray();
            }
        }
    }

    public class NativeLzmaDecoder : LzmaDecoder
    {
        protected byte[] _lzmaProperties;

        public NativeLzmaDecoder()
            : this(new byte[LzmaPropertiesLengthInBytes])
        { }

        public NativeLzmaDecoder(byte[] lzmaProperties)
        {
            this.SetLzmaProperties(lzmaProperties);
        }

        public override bool LzmaHeaderManagedInternally
        {
            get { return false; }
        }

        public override void SetLzmaProperties(byte[] lzmaProperties)
        {
            if (lzmaProperties.Length != LzmaPropertiesLengthInBytes)
                throw new ArgumentException("LZMA properties must be a byte[5].", "lzmaProperties");

            _lzmaProperties = lzmaProperties;
        }

        [DllImport(NativeLzmaLibraryPath, SetLastError = true)]
#if NATIVE64
        protected static extern int LzmaUncompress(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref ulong destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ref ulong srcLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] props,
            ulong propsSize
        );
#else
        protected static extern int LzmaUncompress(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref uint destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ref uint srcLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] props,
            uint propsSize
        );
#endif

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
#if NATIVE64
            ulong bufSize = (ulong)outSize;
            byte[] buffer = new byte[bufSize];
            if (input.Length > 0 && outSize > 0)
            {
                ulong srcLen = (ulong)input.LongLength;
                byte[] lzmaProps = new byte[LzmaPropertiesLengthInBytes];
                Array.Copy(_lzmaProperties, lzmaProps, LzmaPropertiesLengthInBytes);
                int result = LzmaUncompress(
                    buffer, ref bufSize, input, ref srcLen,
                    lzmaProps, (ulong)LzmaPropertiesLengthInBytes);
                CheckNativeReturnCode(result);
                inSize = (long)srcLen;
            }
#else
            uint bufSize = (uint)outSize;
            byte[] buffer = new byte[bufSize];
            if (input.Length > 0 && outSize > 0)
            {
                uint srcLen = (uint)input.Length;
                byte[] lzmaProps = new byte[LzmaPropertiesLengthInBytes];
                Array.Copy(_lzmaProperties, lzmaProps, LzmaPropertiesLengthInBytes);
                int result = LzmaUncompress(
                    buffer, ref bufSize, input, ref srcLen,
                    lzmaProps, (uint)LzmaPropertiesLengthInBytes);
                CheckNativeReturnCode(result);
                inSize = (long)srcLen;
            }
#endif
            outSize = (long)bufSize;
            output = new byte[outSize];
            Array.Copy(buffer, output, outSize);
        }
    }

    public class NativeLzmaDecoderWithBcjFilter : NativeLzmaDecoder
    {
        public NativeLzmaDecoderWithBcjFilter()
            : base()
        { }

        public NativeLzmaDecoderWithBcjFilter(byte[] lzmaProperties)
            : base(lzmaProperties)
        { }

        public override bool LzmaHeaderManagedInternally
        {
            get { return true; }
        }

        [DllImport(NativeLzmaLibraryPath, SetLastError = true)]
        protected static extern int Lzma86_GetUnpackSize(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] src,
            uint srcLen,
            out ulong unpackSize
        );

        [DllImport(NativeLzmaLibraryPath, SetLastError = true)]
#if NATIVE64
        protected static extern int Lzma86_Decode(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref ulong destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ref ulong srcLen
        );
#else
        protected static extern int Lzma86_Decode(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] dest,
            ref uint destLen,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] src,
            ref uint srcLen
        );
#endif

        public static void Decode(Stream inStream, Stream outStream)
        {
            byte[] input = inStream.ToByteArray();
            ulong unpackedSize;
            int result = Lzma86_GetUnpackSize(input, (uint)input.Length, out unpackedSize);
            CheckNativeReturnCode(result);
#if NATIVE64
            ulong bufSize = unpackedSize;
            byte[] buffer = new byte[bufSize];
            ulong srcLen = (ulong)input.LongLength;
#else
            uint bufSize = (uint)unpackedSize;
            byte[] buffer = new byte[bufSize];
            uint srcLen = (uint)input.Length;
#endif
            result = Lzma86_Decode(buffer, ref bufSize, input, ref srcLen);
            CheckNativeReturnCode(result);
            outStream.Write(buffer, 0, (int)bufSize);
        }

        public override void Code(byte[] input, out byte[] output, ref long inSize, ref long outSize)
        {
            using (MemoryStream inStream = new MemoryStream(input),
                                outStream = new MemoryStream())
            {
                Decode(inStream, outStream);
                output = outStream.ToArray();
                outSize = output.LongLength;
            }
        }
    }
}
