using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MDo.Common.Numerics.Random.Test;

namespace MDo.Common.TestLoader
{
    [TestClass]
    public class Numerics_Test
    {
        [TestCategory("Numerics_Test"), TestMethod]
        public void TestRng()
        {
            RngTestMain.TestRngs();
        }
    }
}
