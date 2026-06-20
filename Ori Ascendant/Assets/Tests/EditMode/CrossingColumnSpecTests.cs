using System.Reflection;
using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #33: tribulation overflow column as the Crossing gauge (PRD W2).
    /// Headless gate — CrossingColumnSpec is pure math, no scene, no MonoBehaviour.
    /// Tests pin: height/alpha mapping, activation predicate, apex alignment,
    /// and a reflection gate confirming MainScreenSkin wires the column field.
    /// </summary>
    public class CrossingColumnSpecTests
    {
        // ---- Column height mapping ----

        [Test]
        public void ColumnHeight_ZeroFraction_ReturnsZero() =>
            Assert.AreEqual(0f, CrossingColumnSpec.ColumnHeight(0.0), 0.001f,
                "Zero tribulation progress must produce zero column height");

        [Test]
        public void ColumnHeight_FullFraction_ReturnsMaxHeight() =>
            Assert.AreEqual(CrossingColumnSpec.MaxColumnHeight,
                CrossingColumnSpec.ColumnHeight(1.0), 0.001f,
                "Full tribulation progress must reach the apex (MaxColumnHeight)");

        [Test]
        public void ColumnHeight_HalfFraction_ReturnsHalfMax() =>
            Assert.AreEqual(CrossingColumnSpec.MaxColumnHeight * 0.5f,
                CrossingColumnSpec.ColumnHeight(0.5), 0.001f,
                "Column height must be linear — fraction 0.5 = half of MaxColumnHeight");

        [Test]
        public void ColumnHeight_NegativeFraction_ClampsToZero() =>
            Assert.AreEqual(0f, CrossingColumnSpec.ColumnHeight(-1.0), 0.001f,
                "Negative fraction must clamp to zero height");

        [Test]
        public void ColumnHeight_OverFraction_ClampsToMaxHeight() =>
            Assert.AreEqual(CrossingColumnSpec.MaxColumnHeight,
                CrossingColumnSpec.ColumnHeight(2.0), 0.001f,
                "Fraction above 1.0 must clamp to MaxColumnHeight");

        [Test]
        public void ColumnHeight_SmallFraction_IsPositive()
        {
            // A Sango 2-minute session at stage 6 yields ~1.2% progress (GAMEPLAY §2.4).
            // The column must produce a non-zero height so any progress is visible.
            float h = CrossingColumnSpec.ColumnHeight(0.012);
            Assert.Greater(h, 0f,
                "A 2-minute Sango session (~1.2% progress) must yield a non-zero column height");
        }

        // ---- Column alpha mapping ----

        [Test]
        public void ColumnAlpha_ZeroFraction_ReturnsZero() =>
            Assert.AreEqual(0f, CrossingColumnSpec.ColumnAlpha(0.0), 0.001f,
                "Column must be invisible at zero tribulation progress");

        [Test]
        public void ColumnAlpha_FullFraction_ReturnsOne() =>
            Assert.AreEqual(1f, CrossingColumnSpec.ColumnAlpha(1.0), 0.001f,
                "Column must reach full alpha at tribulation eligibility");

        [Test]
        public void ColumnAlpha_IsMonotonicBetweenZeroAndOne()
        {
            float a0 = CrossingColumnSpec.ColumnAlpha(0.3);
            float a1 = CrossingColumnSpec.ColumnAlpha(0.7);
            Assert.Greater(a1, a0,
                "Column alpha must increase monotonically with tribulation progress");
        }

        // ---- IsActive predicate ----

        [Test]
        public void IsActive_FinalStage_ReturnsTrue() =>
            Assert.IsTrue(CrossingColumnSpec.IsActive(5),
                "Column must be active at the final stage (index 5)");

        [Test]
        public void IsActive_Stage4_ReturnsFalse() =>
            Assert.IsFalse(CrossingColumnSpec.IsActive(4),
                "Column must be inactive before the final stage");

        [Test]
        public void IsActive_Stage0_ReturnsFalse() =>
            Assert.IsFalse(CrossingColumnSpec.IsActive(0),
                "Column must be inactive at the earliest stage");

        // ---- Apex alignment with tribulation eligibility ----

        [Test]
        public void ApexFraction_AlignsWithTribulationEligibility() =>
            Assert.AreEqual(1.0, CrossingColumnSpec.ApexFraction, 0.001,
                "ApexFraction must equal 1.0 — tribulation becomes eligible at full progress");

        [Test]
        public void ColumnHeightAtApex_IsMaxColumnHeight() =>
            Assert.AreEqual(CrossingColumnSpec.MaxColumnHeight,
                CrossingColumnSpec.ColumnHeight(CrossingColumnSpec.ApexFraction), 0.001f,
                "Column height at ApexFraction must equal MaxColumnHeight");

        [Test]
        public void MaxColumnHeight_IsPositive() =>
            Assert.Greater(CrossingColumnSpec.MaxColumnHeight, 0f,
                "MaxColumnHeight must be a positive pixel value");

        // ---- Reflection gate: MainScreenSkin must wire the column ----

        [Test]
        public void MainScreenSkin_Has_CrossingColumnField()
        {
            var field = typeof(MainScreenSkin)
                .GetField("_crossingColumn", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                "_crossingColumn must exist in MainScreenSkin — skin wires the overflow column (issue #33)");
        }
    }
}
