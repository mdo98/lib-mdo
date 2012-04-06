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
        private const double SampleToUnitDoubleMultiplier = 5.42101086242752217E-20;
        
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
        /// e.g. linear congruential RNGs, return samples whose LSBs have very short cycles.</remarks>
        /// <param name="numBits">The desired length in bits of the pseudorandom sample.</param>
        /// <returns>A pseudorandom sample with the given number of bits.</returns>
        private ulong SampleBits(int numBits)
        {
            return (this.Sample() & (~((~0UL) << numBits)));
        }
#else
        private const double SampleToUnitDoubleMultiplier = 2.32830643653869629E-10;

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
        /// e.g. linear congruential RNGs, return samples whose LSBs have very short cycles.</remarks>
        /// <param name="numBits">The desired length in bits of the pseudorandom sample.</param>
        /// <returns>A pseudorandom sample with the given number of bits.</returns>
        private uint SampleBits(int numBits)
        {
            return (this.Sample() & (~((~0U) << numBits)));
        }
#endif

        protected static ulong ToUInt64(uint sample1, uint sample2)
        {
            return (((ulong)sample1) << 32) + (ulong)sample2;
        }

        protected static long ToInt64(uint sample1, uint sample2)
        {
            ulong uLongSample = ToUInt64(sample1, sample2);
            // Basically, halving the domain
            const ulong longLimit = 1UL << 63;
            if (uLongSample >= longLimit)
                return (long)(uLongSample - longLimit);
            else
                return (long)uLongSample;
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
            if (range <= 0.0)
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
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (int)(this.SampleToUnitDouble() * range));
        }

        public uint UInt32(uint min, uint max)
        {
            double range = max - min;
            if (range <= 0.0)
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
                    b[i] = (byte)(sample & 0xFFUL);
                    sample = (sample >> 8);
                    i++;
                }
                while ((i < b.Length) && (((uint)i & 0x7U) != 0));
            }
        }

        public uint UInt32()
        {
            return (uint)this.SampleBits(32);
        }

        public long Int64()
        {
            return (long)this.SampleBits(63);
        }

        public ulong UInt64()
        {
            return this.Sample();
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

        public uint UInt32()
        {
            return this.Sample();
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
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (long)(this.SampleToUnitDouble() * range));
        }

        public ulong UInt64(ulong min, ulong max)
        {
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (ulong)(this.SampleToUnitDouble() * range));
        }

        #endregion IRandom
    }
}
