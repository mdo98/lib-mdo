using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public abstract class RandomNumberGenerator : IRandom
    {
        #region Abstract Members

#if !X86
        protected abstract ulong InternalSample();
#else
        protected abstract uint InternalSample();
#endif

        #endregion Abstract Members


        #region Internal Operations

        private readonly object SyncRoot = new object();

#if !X86
        private const decimal SampleToUnitDecimalMultiplier = 1.0M / ((decimal)ulong.MaxValue + 1.0M);
        private const double SampleToUnitDoubleMultiplier = (double)SampleToUnitDecimalMultiplier;

        protected virtual ulong SampleBits(int numBits)
        {
            ulong mask = (1UL << numBits) - 1UL;
            return (this.Sample() & mask);
        }

        private decimal SampleToUnitDecimal()
        {
            return (SampleToUnitDecimalMultiplier * (decimal)this.InternalSample());
        }

        protected static ulong[] GetSeedArray(int length, ulong seed)
        {
            ulong[] seeds = new ulong[length];
            seeds[0] = seed;
            for (int i = 1; i < length; i++)
            {
                unchecked { seeds[i] = 6364136223846793005UL * (seeds[i - 1] ^ (seeds[i - 1] >> 60)); }
            }
            return seeds;
        }

        internal ulong Sample()
        {
            ulong val;
            lock (SyncRoot)
            {
                val = this.InternalSample();
            }
            return val;
        }
#else
        private const double SampleToUnitDoubleMultiplier = 1.0 / ((double)uint.MaxValue + 1.0D);

        protected virtual uint SampleBits(int numBits)
        {
            uint mask = (1U << numBits) - 1U;
            return (this.Sample() & mask);
        }

        internal uint Sample()
        {
            uint val;
            lock (SyncRoot)
            {
                val = this.InternalSample();
            }
            return val;
        }
#endif

        private double SampleToUnitDouble()
        {
            return (SampleToUnitDoubleMultiplier * (double)this.InternalSample());
        }

        protected static uint[] GetSeedArray(int length, uint seed)
        {
            uint[] seeds = new uint[length];
            seeds[0] = seed;
            for (int i = 1; i < length; i++)
            {
                unchecked { seeds[i] = 1664525U * (seeds[i - 1] ^ (seeds[i - 1] >> 30)); }
            }
            return seeds;
        }

        protected static byte[] GetSeed(int numBytes)
        {
            byte[] seed = new byte[numBytes];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(seed);
            }
            return seed;
        }

        public override string ToString()
        {
            return this.Name;
        }

        #endregion Internal Operations


        #region IRandom

        public virtual string Name
        {
            get { return this.GetType().Name; }
        }

        public virtual void GetBytes(byte[] b)
        {
            if (null == b)
                throw new ArgumentNullException("b");
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = (byte)this.SampleBits(8);
            }
        }

        public virtual bool Bool()
        {
            return (this.SampleBits(1) == 0);
        }

        public virtual double Double()
        {
            return this.SampleToUnitDouble();
        }

        public double Double(double min, double max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + this.SampleToUnitDouble() * range);
        }

        public virtual int Int()
        {
            return (int)this.SampleBits(31);
        }

        public virtual int Int(int min, int max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (int)Math.Floor(this.SampleToUnitDouble() * range));
        }

        public virtual uint UInt()
        {
            return (uint)this.SampleBits(32);
        }

        public virtual uint UInt(uint min, uint max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (uint)Math.Floor(this.SampleToUnitDouble() * range));
        }

#if !X86
        public virtual decimal Decimal()
        {
            return this.SampleToUnitDecimal();
        }

        public virtual decimal Decimal(decimal min, decimal max)
        {
            decimal range = max - min;
            if (range < 0.0M)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + this.SampleToUnitDecimal() * range);
        }

        public virtual long Long()
        {
            return (long)this.SampleBits(63);
        }

        public virtual long Long(long min, long max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (long)Math.Floor(this.SampleToUnitDouble() * range));
        }

        public virtual ulong ULong()
        {
            return this.SampleBits(64);
        }

        public virtual ulong ULong(ulong min, ulong max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (ulong)Math.Floor(this.SampleToUnitDouble() * range));
        }
#endif

        #endregion IRandom
    }
}
