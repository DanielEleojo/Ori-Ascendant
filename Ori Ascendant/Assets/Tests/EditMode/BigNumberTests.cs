using NUnit.Framework;
using Newtonsoft.Json;
using OriAscendant.Core;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// EditMode suite for <see cref="BigNumber"/>. BigNumber.cs is off-limits to
    /// change without these tests, so the suite covers normalization, the four
    /// operators across exponents, precision behaviour at scale, comparison,
    /// every ToString tier, and the JSON serialization round-trip (TECH §8 risk).
    /// </summary>
    public class BigNumberTests
    {
        // ---- normalization ----

        [Test]
        public void Normalizes_To_Engineering_Form()
        {
            var n = new BigNumber(1500.0, 0);
            Assert.AreEqual(1.5, n.Mantissa, 1e-9);
            Assert.AreEqual(3, n.Exponent);
        }

        [Test]
        public void Normalizes_SubUnit_Value()
        {
            var n = new BigNumber(0.5, 0);
            Assert.AreEqual(500.0, n.Mantissa, 1e-9);
            Assert.AreEqual(-3, n.Exponent);
        }

        [Test]
        public void Folds_NonMultipleOf3_Exponent()
        {
            var n = new BigNumber(1.0, 10); // 1e10 == 10 * 1e9
            Assert.AreEqual(10.0, n.Mantissa, 1e-9);
            Assert.AreEqual(9, n.Exponent);
        }

        [Test]
        public void Zero_Is_Canonical()
        {
            var n = new BigNumber(0.0, 5);
            Assert.IsTrue(n.IsZero);
            Assert.AreEqual(0.0, n.Mantissa);
            Assert.AreEqual(0, n.Exponent);
        }

        [Test]
        public void Default_Value_Is_Zero()
        {
            BigNumber n = default;
            Assert.IsTrue(n.IsZero);
        }

        // ---- arithmetic ----

        [Test]
        public void Adds_Same_Exponent()
        {
            var r = new BigNumber(5, 3) + new BigNumber(5, 3); // 5e3 + 5e3 = 1e4
            Assert.AreEqual(new BigNumber(10, 3), r);
        }

        [Test]
        public void Add_Preserves_Large_Magnitude_Within_Precision()
        {
            var big = new BigNumber(1, 30);
            var r = big + new BigNumber(1, 0); // tiny addend lost at scale (expected)
            Assert.AreEqual(big, r);
        }

        [Test]
        public void Add_Carries_To_Next_Magnitude()
        {
            var r = new BigNumber(999, 0) + new BigNumber(1, 0); // 1000 -> 1K
            Assert.AreEqual(new BigNumber(1, 3), r);
        }

        [Test]
        public void Subtracts_To_Zero()
        {
            var r = new BigNumber(5, 6) - new BigNumber(5, 6);
            Assert.IsTrue(r.IsZero);
        }

        [Test]
        public void Subtracts_To_Negative()
        {
            var r = new BigNumber(1, 0) - new BigNumber(2, 0);
            Assert.AreEqual(new BigNumber(-1, 0), r);
        }

        [Test]
        public void Multiplies_Across_Exponents()
        {
            var r = new BigNumber(2, 3) * new BigNumber(3, 6); // 2e3 * 3e6 = 6e9
            Assert.AreEqual(new BigNumber(6, 9), r);
        }

        [Test]
        public void Multiply_Renormalizes_Mantissa()
        {
            var r = new BigNumber(999, 0) * new BigNumber(999, 0); // 998001
            Assert.AreEqual(3, r.Exponent);
            Assert.AreEqual(998.001, r.Mantissa, 1e-6);
        }

        [Test]
        public void Divides_Across_Exponents()
        {
            var r = new BigNumber(1, 6) / new BigNumber(1, 3); // 1e3
            Assert.AreEqual(new BigNumber(1, 3), r);
        }

        [Test]
        public void Divides_To_SubUnit()
        {
            var r = new BigNumber(1, 0) / new BigNumber(8, 0); // 0.125
            Assert.AreEqual(new BigNumber(125, -3), r);
        }

        [Test]
        public void Divide_By_Zero_Throws()
        {
            Assert.Throws<System.DivideByZeroException>(() =>
            {
                var _ = new BigNumber(1, 0) / BigNumber.Zero;
            });
        }

        [Test]
        public void Scalar_Multiply_Matches_BigNumber_Multiply()
        {
            var rate = new BigNumber(2, 0);
            var r = rate * 3600.0; // 2/s for one hour = 7200
            Assert.AreEqual(new BigNumber(7.2, 3), r);
        }

        // ---- comparison ----

        [Test]
        public void Compares_By_Magnitude()
        {
            Assert.IsTrue(new BigNumber(1, 6) > new BigNumber(1, 3));
            Assert.IsTrue(new BigNumber(1, 3) < new BigNumber(1, 6));
        }

        [Test]
        public void Compares_Negatives_Correctly()
        {
            Assert.IsTrue(new BigNumber(-1, 6) < new BigNumber(-1, 3));
        }

        [Test]
        public void Equality_Operators()
        {
            Assert.IsTrue(new BigNumber(5, 3) == new BigNumber(5000, 0));
            Assert.IsTrue(new BigNumber(5, 3) != new BigNumber(6, 3));
        }

        // ---- formatting ----

        [Test]
        public void Formats_Plain_K_M_B_T()
        {
            Assert.AreEqual("1", new BigNumber(1, 0).ToString());
            Assert.AreEqual("1.5K", new BigNumber(1.5, 3).ToString());
            Assert.AreEqual("523.4M", new BigNumber(523.4, 6).ToString());
            Assert.AreEqual("6B", new BigNumber(6, 9).ToString());
        }

        [Test]
        public void Formats_Scientific_Beyond_T()
        {
            Assert.AreEqual("1.00e18", new BigNumber(1, 18).ToString());
            Assert.AreEqual("5.23e17", new BigNumber(523.4, 15).ToString());
            Assert.AreEqual("1.00e300", new BigNumber(1, 300).ToString());
        }

        [Test]
        public void Formats_SubUnit_And_Negative_And_Zero()
        {
            Assert.AreEqual("0.125", new BigNumber(125, -3).ToString());
            Assert.AreEqual("-1", new BigNumber(-1, 0).ToString());
            Assert.AreEqual("0", BigNumber.Zero.ToString());
        }

        // ---- serialization round-trip (TECH §8 risk) ----

        private class SaveDto
        {
            public double Mantissa;
            public int Exponent;
        }

        [TestCase(10.0, 9)]    // 1e10
        [TestCase(100.0, 48)]  // 1e50
        [TestCase(10.0, 99)]   // 1e100
        [TestCase(100.0, 198)] // 1e200
        [TestCase(1.0, 300)]   // 1e300
        public void Survives_Json_Round_Trip(double mantissa, int exponent)
        {
            var original = new BigNumber(mantissa, exponent);
            var dto = new SaveDto { Mantissa = original.Mantissa, Exponent = original.Exponent };

            string json = JsonConvert.SerializeObject(dto);
            var restored = JsonConvert.DeserializeObject<SaveDto>(json);
            var roundTripped = new BigNumber(restored.Mantissa, restored.Exponent);

            Assert.AreEqual(original, roundTripped);
        }
    }
}
