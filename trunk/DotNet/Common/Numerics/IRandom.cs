using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Common.Numerics
{
    public interface IRandom
    {
        string Name { get; }
        void GetBytes(byte[] b);
        bool Bool();
        double Double();
        double Double(double min, double max);
        int Int32();
        int Int32(int min, int max);
        uint UInt32();
        uint UInt32(uint min, uint max);
        long Int64();
        long Int64(long min, long max);
        ulong UInt64();
        ulong UInt64(ulong min, ulong max);
    }
}
