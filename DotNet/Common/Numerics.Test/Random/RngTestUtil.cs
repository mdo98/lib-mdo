using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MDo.Common.Numerics.Random.Test
{
    public static class RngTestUtil
    {
        private static readonly RNGCryptoServiceProvider CryptoRng = new RNGCryptoServiceProvider();

        public static int RandomInt()
        {
            byte[] b = new byte[4];
            CryptoRng.GetBytes(b);
            return BitConverter.ToInt32(b, 0);
        }
    }
}
