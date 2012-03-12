using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MDo.Common.Numerics.Random.Test;
using MDo.Common.Numerics.Statistics.Test;

namespace MDo.Common.TestLoader
{
    [TestClass]
    public class Numerics_Test
    {
#if DEBUG
        [TestCategory("Numerics_Test"), TestMethod]
        public void Rng_ImplementationCheck()
        {
            RngTestMain.ImplementationCheck();
        }
#endif

        [TestCategory("Numerics_Test"), TestMethod]
        public void Rng_TestRngs()
        {
            RngTestMain.TestRngs();
        }

        [TestCategory("Numerics_Test"), TestMethod]
        public void Rng_GenerateSamplesForDieHard()
        {
            RngTestMain.GenerateSamplesForDieHard();
        }

        [TestCategory("Numerics_Test"), TestMethod]
        public void Statistics_KolmogorovSmirnov_CdfOneSided_KnuthAsymptoteFormula()
        {
            StatisticsTest.KolmogorovSmirnov_CdfOneSided_KnuthAsymptoteFormula();
        }

        [TestCategory("Numerics_Test"), TestMethod]
        public void Statistics_KolmogorovSmirnov_Cdf_DurbinMatrix()
        {
            StatisticsTest.KolmogorovSmirnov_Cdf_DurbinMatrix();
        }

        [TestCategory("Numerics_Test"), TestMethod]
        public void Statistics_KolmogorovSmirnov_Cdf()
        {
            StatisticsTest.KolmogorovSmirnov_Cdf();
        }
    }
}
