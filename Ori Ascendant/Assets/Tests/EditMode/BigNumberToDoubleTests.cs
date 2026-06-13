using NUnit.Framework;
using OriAscendant.Core;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Accompanying tests for BigNumber.ToDouble() (added Phase B for UI
    /// progress ratios) — the off-limits rule requires any BigNumber.cs change
    /// to ship with unit tests in the same change.
    /// </summary>
    public class BigNumberToDoubleTests
    {
        [Test]
        public void Zero_IsZero()
        {
            Assert.AreEqual(0.0, BigNumber.Zero.ToDouble());
        }

        [TestCase(1.5, 3, 1500.0)]
        [TestCase(100.0, 0, 100.0)]
        [TestCase(25.0, 6, 25_000_000.0)]
        [TestCase(750.0, 3, 750_000.0)]
        [TestCase(-2.5, 3, -2500.0)]
        public void KnownValues_ConvertExactly(double mantissa, int exponent, double expected)
        {
            Assert.AreEqual(expected, new BigNumber(mantissa, exponent).ToDouble(), 1e-9);
        }

        [TestCase(0.0)]
        [TestCase(1.0)]
        [TestCase(123456.789)]
        [TestCase(0.25)]
        [TestCase(-987654.321)]
        public void FromDouble_RoundTrips(double value)
        {
            double restored = BigNumber.FromDouble(value).ToDouble();
            Assert.AreEqual(value, restored, System.Math.Abs(value) * 1e-12 + 1e-12);
        }

        [Test]
        public void LargeMagnitude_WithinDoubleRange_IsApproximate()
        {
            var value = new BigNumber(1.0, 30); // 1e30
            Assert.AreEqual(1e30, value.ToDouble(), 1e30 * 1e-12);
        }

        [Test]
        public void BeyondDoubleRange_Saturates_NeverInfinity()
        {
            var huge = new BigNumber(5.0, 400);
            var hugeNegative = new BigNumber(-5.0, 400);

            Assert.AreEqual(double.MaxValue, huge.ToDouble());
            Assert.AreEqual(double.MinValue, hugeNegative.ToDouble());
            Assert.IsFalse(double.IsInfinity(huge.ToDouble()));
        }

        [Test]
        public void RatioUseCase_OneDecimalPercent()
        {
            // The stage-6 tribulation bar: 9,300,000 / 25,000,000 = 37.2%.
            var ase = new BigNumber(9.3, 6);
            var threshold = new BigNumber(25.0, 6);

            double percent = (ase / threshold).ToDouble() * 100.0;

            Assert.AreEqual(37.2, percent, 1e-6);
        }
    }
}
