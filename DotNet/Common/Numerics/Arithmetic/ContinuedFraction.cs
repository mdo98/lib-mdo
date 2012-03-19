using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Arithmetic
{
    public class ContinuedFraction<TNumeric> : IConvertible
        where TNumeric : IConvertible
    {
        #region Constructors

        public ContinuedFraction(Func<uint, bool> subtractOps, Func<uint, TNumeric> numerators, Func<uint, TNumeric> denominators)
        {
            this.SubtractOps = subtractOps;
            this.Numerators = numerators;
            this.Denominators = denominators;
        }

        #endregion Constructors


        #region Properties

        public Func<uint, bool> SubtractOps     { get; private set; }
        public Func<uint, TNumeric> Numerators { get; private set; }
        public Func<uint, TNumeric> Denominators { get; private set; }

        #endregion Properties


        private const double LENTZ_TINY = 1.0E-60, LENTZ_EPS = 1.0E-12;

        private double Eval_Lentz(uint numTerms = uint.MaxValue)
        {
            double f = this.Denominators(0U).ToDouble(null);
            if (numTerms > 0U)
            {
                if (f == 0.0)
                    f = LENTZ_TINY;
                double C = f, D = 0.0;
                for (uint i = 1; i <= numTerms; i++)
                {
                    D = this.Denominators(i).ToDouble(null) + (this.SubtractOps(i) ? -1.0 : 1.0) * this.Numerators(i).ToDouble(null) * D;
                    if (D == 0.0)
                        D = LENTZ_TINY;
                    C = this.Denominators(i).ToDouble(null) + (this.SubtractOps(i) ? -1.0 : 1.0) * this.Numerators(i).ToDouble(null) / C;
                    if (C == 0.0)
                        C = LENTZ_TINY;
                    D = 1.0 / D;
                    double delta = C * D;
                    f *= delta;
                    if (Math.Abs(delta - 1.0) < LENTZ_EPS)
                        break;
                }
            }
            if (this.SubtractOps(0U))
            {
                f = 0.0 - f;
            }
            return f;
        }


        #region IConvertible

        public decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal)this.Eval_Lentz();
        }

        public double ToDouble(IFormatProvider provider)
        {
            return this.Eval_Lentz();
        }

        public short ToInt16(IFormatProvider provider)
        {
            return (short)this.Eval_Lentz(0U);
        }

        public int ToInt32(IFormatProvider provider)
        {
            return (int)this.Eval_Lentz(0U);
        }

        public long ToInt64(IFormatProvider provider)
        {
            return (long)this.Eval_Lentz(0U);
        }

        public float ToSingle(IFormatProvider provider)
        {
            return (float)this.Eval_Lentz();
        }

        public string ToString(IFormatProvider provider)
        {
            return this.Eval_Lentz().ToString(provider);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(double))
                return this.ToDouble(provider);
            else if (conversionType == typeof(decimal))
                return this.ToDecimal(provider);
            else if (conversionType == typeof(float))
                return this.ToSingle(provider);
            else if (conversionType == typeof(int))
                return this.ToInt32(provider);
            else if (conversionType == typeof(uint))
                return this.ToUInt32(provider);
            else if (conversionType == typeof(long))
                return this.ToInt64(provider);
            else if (conversionType == typeof(ulong))
                return this.ToUInt64(provider);
            else if (conversionType == typeof(short))
                return this.ToInt16(provider);
            else if (conversionType == typeof(ushort))
                return this.ToUInt16(provider);
            else if (conversionType == typeof(string))
                return this.ToString(provider);
            else
                throw new InvalidCastException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return (ushort)this.Eval_Lentz(0U);
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return (uint)this.Eval_Lentz(0U);
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return (ulong)this.Eval_Lentz(0U);
        }

        #region Invalid casts & miscellaneous

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        #endregion Invalid casts & miscellaneous

        #endregion IConvertible
    }
}
