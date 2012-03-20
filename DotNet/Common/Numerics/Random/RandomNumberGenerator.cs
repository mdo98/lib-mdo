using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public abstract class RandomNumberGenerator : IRandom
    {
#if X86
        private static readonly uint[] MASKS = new uint[32]; 
#else
        private static readonly ulong[] MASKS = new ulong[64];
#endif

        static RandomNumberGenerator()
        {
            for (int i = 0; i < MASKS.Length; i++)
            {
#if X86
                MASKS[i] = ~((~0U) << i);
#else
                MASKS[i] = ~((~0UL) << i);
#endif
            }
        }


        #region Abstract Members

#if !X86
        protected abstract ulong InternalSample();
#else
        protected abstract uint InternalSample();
#endif

        #endregion Abstract Members


        #region Internal Operations

        private readonly object SyncRoot = new object();
        private const decimal SampleToUnitDecimalMultiplier = decimal.One / ((decimal)ulong.MaxValue + decimal.One);
        private const double SampleToUnitDoubleMultiplier = (double)SampleToUnitDecimalMultiplier;

#if !X86
        internal ulong Sample()
        {
            ulong val;
            lock (SyncRoot)
            {
                val = this.InternalSample();
            }
            return val;
        }

        /// <summary>
        /// Returns a pseudorandom sample with a given number of bits.
        /// </summary>
        /// <remarks>This method returns the least significant bits (LSBs) of a pseudorandom sample.
        /// Modern RNGs return samples with random LSBs; however many RNGs now known to be defective,
        /// e.g. linear congruential RNGs, return samples whose LSBs have very short cycles.  If you
        /// implement a defective RNG, you should override this method.</remarks>
        /// <param name="numBits">The desired length in bits of the pseudorandom sample.</param>
        /// <returns>A pseudorandom sample with the given number of bits.</returns>
        protected virtual ulong SampleBits(int numBits)
        {
            return (this.Sample() & MASKS[numBits]);
        }

        private decimal SampleToUnitDecimal()
        {
            return (SampleToUnitDecimalMultiplier * (decimal)this.InternalSample());
        }
#else
        internal uint Sample()
        {
            uint val;
            lock (SyncRoot)
            {
                val = this.InternalSample();
            }
            return val;
        }

        /// <summary>
        /// Returns a pseudorandom sample with a given number of bits.
        /// </summary>
        /// <remarks>This method returns the least significant bits (LSBs) of a pseudorandom sample.
        /// Modern RNGs return samples with random LSBs; however many RNGs now known to be defective,
        /// e.g. linear congruential RNGs, return samples whose LSBs have very short cycles.  If you
        /// implement a defective RNG, you should override this method.</remarks>
        /// <param name="numBits">The desired length in bits of the pseudorandom sample.</param>
        /// <returns>A pseudorandom sample with the given number of bits.</returns>
        protected virtual uint SampleBits(int numBits)
        {
            return (this.Sample() & MASKS[numBits]);
        }

        private decimal SampleToUnitDecimal()
        {
            return (SampleToUnitDecimalMultiplier * (decimal)this.UInt64());
        }
#endif

        protected static ulong ToUInt64(uint sample1, uint sample2)
        {
            return (((ulong)sample1) << 32) + (ulong)sample2;
        }

        protected static long ToInt64(uint sample1, uint sample2)
        {
            const ulong longMaxValue = long.MaxValue;
            ulong uLongSample = ToUInt64(sample1, sample2);
            if (uLongSample > longMaxValue)
                return (long)(uLongSample - (longMaxValue + 1UL));
            else
                return ((long)uLongSample + long.MinValue);
        }

        private double SampleToUnitDouble()
        {
            return (SampleToUnitDoubleMultiplier * (double)this.InternalSample());
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

        protected static uint[] GetSeedArray(int length, uint seed)
        {
            uint[] seeds = new uint[length];
            seeds[0] = seed;
            for (int i = 1; i < length; i++)
            {
                unchecked { seeds[i] = 1664525U * (seeds[i - 1] ^ (seeds[i - 1] >> 30)) + (uint)i; }
            }
            return seeds;
        }

        protected static ulong[] GetSeedArray(int length, ulong seed)
        {
            ulong[] seeds = new ulong[length];
            seeds[0] = seed;
            for (int i = 1; i < length; i++)
            {
                unchecked { seeds[i] = 6364136223846793005UL * (seeds[i - 1] ^ (seeds[i - 1] >> 60)) + (ulong)i; }
            }
            return seeds;
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

        public bool Bool()
        {
            return (this.SampleBits(1) == 0);
        }

        public double Double()
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

        public int Int32()
        {
            return (int)this.SampleBits(31);
        }

        public int Int32(int min, int max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (int)(this.SampleToUnitDouble() * range));
        }

        public uint UInt32()
        {
            return (uint)this.SampleBits(32);
        }

        public uint UInt32(uint min, uint max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (uint)(this.SampleToUnitDouble() * range));
        }

#if !X86
        public void GetBytes(byte[] b)
        {
            if (null == b)
                throw new ArgumentNullException("b");

            int i = 0;
            while (i < b.Length)
            {
                ulong sample = this.Sample();
                do
                {
                    b[i] = (byte)(sample & 0xFFU);
                    sample = (sample >> 8);
                    i++;
                }
                while ((i < b.Length) && (((uint)i & 0x7U) != 0));
            }
        }

        public long Int64()
        {
            return (long)this.SampleBits(63);
        }

        public ulong UInt64()
        {
            return this.SampleBits(64);
        }
#else
        public void GetBytes(byte[] b)
        {
            if (null == b)
                throw new ArgumentNullException("b");

            int i = 0;
            while (i < b.Length)
            {
                uint sample = this.Sample();
                do
                {
                    b[i] = (byte)(sample & 0xFFU);
                    sample = (sample >> 8);
                    i++;
                }
                while ((i < b.Length) && (((uint)i & 0x3U) != 0));
            }
        }

        public long Int64()
        {
            return ToInt64(this.Sample(), this.Sample());
        }

        public ulong UInt64()
        {
            return ToUInt64(this.Sample(), this.Sample());
        }
#endif

        public long Int64(long min, long max)
        {
            decimal range = max - min;
            if (range < 0.0M)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (long)(this.SampleToUnitDecimal() * range));
        }

        public ulong UInt64(ulong min, ulong max)
        {
            decimal range = max - min;
            if (range < 0.0M)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (ulong)(this.SampleToUnitDecimal() * range));
        }

        public decimal Decimal()
        {
            return this.SampleToUnitDecimal();
        }

        public decimal Decimal(decimal min, decimal max)
        {
            decimal range = max - min;
            if (range < 0.0M)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + this.SampleToUnitDecimal() * range);
        }

        #endregion IRandom
    }
}
