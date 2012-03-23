using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    /// <summary>
    /// Wraps System.Random and implements IRandom interface to test its randomness properties.
    /// </summary>
    public sealed class SystemRandom : System.Random, IRandom
    {
        private readonly int Seed;

        public SystemRandom() : base() { }
        public SystemRandom(int seed) : base(seed) { Seed = seed; }

        public override string ToString()
        {
            return this.Name;
        }

        #region IRandom

        public string Name
        {
            get
            {
                return string.Format(
                    "{0} (Seed = {1})",
                    this.GetType().Name,
                    this.Seed);
            }
        }

        public void GetBytes(byte[] b)
        {
            base.NextBytes(b);
        }

        public bool Bool()
        {
            return (base.NextDouble() >= 0.5);
        }

        public double Double()
        {
            return base.NextDouble();
        }

        public double Double(double min, double max)
        {
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + base.NextDouble() * range);
        }

        public int Int32()
        {
            return base.Next();
        }

        public int Int32(int min, int max)
        {
            return base.Next(min, max);
        }

        public uint UInt32()
        {
            return this.UInt32(0, uint.MaxValue);
        }

        public uint UInt32(uint min, uint max)
        {
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (uint)(base.NextDouble() * range));
        }

        public long Int64()
        {
            return this.Int64(0, long.MaxValue);
        }

        public long Int64(long min, long max)
        {
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (long)(base.NextDouble() * range));
        }

        public ulong UInt64()
        {
            return this.UInt64(0, ulong.MaxValue);
        }

        public ulong UInt64(ulong min, ulong max)
        {
            double range = max - min;
            if (range <= 0.0)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (ulong)(base.NextDouble() * range));
        }

        #endregion IRandom
    }
}
