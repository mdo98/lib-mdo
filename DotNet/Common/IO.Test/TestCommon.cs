using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace MDo.Common.IO.Test
{
    public static class TestCommon
    {
        private static readonly string[] TestDataDirs = new string[]
        {
            @"data",
        };

        private static readonly string[] TestFileTypes = new string[]
        {
            @"*.csv",
        };

        public static ICollection<string> GetTestDataPaths()
        {
            ICollection<string> testFiles = new SortedSet<string>();
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (string dir in TestDataDirs.Select(item => Path.Combine(baseDir, item)))
            {
                foreach (string fileType in TestFileTypes)
                {
                    foreach (string file in Directory.GetFiles(dir, fileType))
                    {
                        testFiles.Add(file);
                    }
                }
            }
            return testFiles;
        }
    }
}
