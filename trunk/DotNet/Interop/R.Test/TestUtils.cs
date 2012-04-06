using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using MDo.Common.Numerics.Random;

namespace MDo.Interop.R.Test
{
    public static class TestUtils
    {
        public static RandomNumberGenerator RNG = new MT19937Rng();

        public static void ThrowInvalidDataStream()
        {
            throw new InvalidDataException("Input stream does not have the expected format.");
        }

        public const string DataDir = "data";

        public static string[] GetDataFiles(string ns, string prefix)
        {
            string testDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Directory.GetFiles(Path.Combine(testDir, DataDir, ns), string.Format("{0}*.txt", prefix));
        }

        public static TimeSpan Time(Action action)
        {
            DateTime start = DateTime.Now;
            action();
            DateTime end = DateTime.Now;
            return (end - start);
        }
    }
}
