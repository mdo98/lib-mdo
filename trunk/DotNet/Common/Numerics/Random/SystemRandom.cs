using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public sealed class SystemRandom : System.Random, IRandom
    {
        private readonly int Seed;

        public SystemRandom() : this(Environment.TickCount) { }
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
            return (base.NextDouble() >= 0.5D);
        }

        public double Double()
        {
            return base.NextDouble();
        }

        public double Double(double min, double max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + base.NextDouble() * range);
        }

        public int Int()
        {
            return base.Next();
        }

        public int Int(int min, int max)
        {
            return base.Next(min, max);
        }

        public uint UInt()
        {
            return this.UInt(0, uint.MaxValue);
        }

        public uint UInt(uint min, uint max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (uint)Math.Floor(base.NextDouble() * range));
        }

#if !X86
        public decimal Decimal()
        {
            return (decimal)base.NextDouble();
        }

        public decimal Decimal(decimal min, decimal max)
        {
            decimal range = max - min;
            if (range < 0.0M)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (decimal)base.NextDouble() * range);
        }

        public long Long()
        {
            return this.Long(0, long.MaxValue);
        }

        public long Long(long min, long max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (long)Math.Floor(base.NextDouble() * range));
        }

        public ulong ULong()
        {
            return this.ULong(0, ulong.MaxValue);
        }

        public ulong ULong(ulong min, ulong max)
        {
            double range = max - min;
            if (range < 0.0D)
                throw new ArgumentOutOfRangeException("max - min");
            return (min + (ulong)Math.Floor(base.NextDouble() * range));
        }
#endif

        #endregion IRandom
    }
}
