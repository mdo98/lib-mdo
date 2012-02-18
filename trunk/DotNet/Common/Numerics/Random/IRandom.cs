using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics.Random
{
    public interface IRandom
    {
        string Name { get; }
        void GetBytes(byte[] b);
        bool Bool();
        double Double();
        double Double(double min, double max);
        int Int();
        int Int(int min, int max);
        uint UInt();
        uint UInt(uint min, uint max);
#if !X86
        decimal Decimal();
        decimal Decimal(decimal min, decimal max);
        long Long();
        long Long(long min, long max);
        ulong ULong();
        ulong ULong(ulong min, ulong max);
#endif
    }
}
