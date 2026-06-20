using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for VesselFillRatio (issue #25 / PRD W2).
    /// No scene, no MonoBehaviour — fill ratio is a pure math function.
    /// </summary>
    public class VesselFillRatioTests
    {
        private const int TotalStages = 6; // MVP stage count

        // ---- Boundary values ----

        [Test]
        public void Stage0_ZeroProgress_IsZero()
        {
            float fill = VesselFillRatio.Compute(0, 0.0, TotalStages);
            Assert.AreEqual(0f, fill, 0.001f,
                "Stage 0 with 0 progress should yield fill=0 (empty vessel)");
        }

        [Test]
        public void FinalStage_FullProgress_IsOne()
        {
            float fill = VesselFillRatio.Compute(TotalStages - 1, 1.0, TotalStages);
            Assert.AreEqual(1f, fill, 0.001f,
                "Final stage at 100% progress should yield fill=1 (brimming)");
        }

        // ---- Monotonic across stage boundaries ----

        [Test]
        public void StageBoundary_AdvancingDoesNotDrop()
        {
            // At the end of stage 2, fill = (2 + 1.0) / 6 = 0.5
            float atStage2Full = VesselFillRatio.Compute(2, 1.0, TotalStages);
            // At the start of stage 3, fill = (3 + 0.0) / 6 = 0.5
            float atStage3Empty = VesselFillRatio.Compute(3, 0.0, TotalStages);
            Assert.AreEqual(atStage2Full, atStage3Empty, 0.001f,
                "Fill must not drop when advancing to a new stage (monotonic boundary)");
        }

        [Test]
        public void AllStageBoundaries_AreMonotonic()
        {
            for (int s = 0; s < TotalStages - 1; s++)
            {
                float endOfStage = VesselFillRatio.Compute(s, 1.0, TotalStages);
                float startOfNext = VesselFillRatio.Compute(s + 1, 0.0, TotalStages);
                Assert.AreEqual(endOfStage, startOfNext, 0.001f,
                    $"Fill must be continuous at boundary between stage {s} and {s + 1}");
            }
        }

        // ---- Mid-stage values ----

        [Test]
        public void Stage3_ZeroProgress_IsHalf()
        {
            float fill = VesselFillRatio.Compute(3, 0.0, TotalStages);
            Assert.AreEqual(0.5f, fill, 0.001f,
                "Stage 3 at 0% should be exactly half-full");
        }

        [Test]
        public void Stage3_HalfProgress_IsBetweenHalfAndTwoThirds()
        {
            float fill = VesselFillRatio.Compute(3, 0.5, TotalStages);
            Assert.AreEqual(3.5f / TotalStages, fill, 0.001f);
        }

        // ---- Clamping ----

        [Test]
        public void ProgressFraction_AboveOne_ClampedToOne()
        {
            float clamped = VesselFillRatio.Compute(1, 2.5, TotalStages);
            float expected = VesselFillRatio.Compute(1, 1.0, TotalStages);
            Assert.AreEqual(expected, clamped, 0.001f,
                "progressFraction > 1 must be clamped to 1");
        }

        [Test]
        public void ProgressFraction_Negative_ClampedToZero()
        {
            float clamped = VesselFillRatio.Compute(2, -0.5, TotalStages);
            float expected = VesselFillRatio.Compute(2, 0.0, TotalStages);
            Assert.AreEqual(expected, clamped, 0.001f,
                "Negative progressFraction must be clamped to 0");
        }

        // ---- Defensive guards ----

        [Test]
        public void ZeroTotalStages_ReturnsZero()
        {
            float fill = VesselFillRatio.Compute(3, 0.5, 0);
            Assert.AreEqual(0f, fill, 0.001f,
                "Zero totalStages must return 0 (defensive)");
        }

        [Test]
        public void NegativeTotalStages_ReturnsZero()
        {
            float fill = VesselFillRatio.Compute(0, 0.0, -1);
            Assert.AreEqual(0f, fill, 0.001f);
        }

        // ---- Monotonically increasing within a stage ----

        [Test]
        public void WithinStage_MonotonicallyIncreasing()
        {
            float prev = VesselFillRatio.Compute(2, 0.0, TotalStages);
            for (int i = 1; i <= 10; i++)
            {
                float cur = VesselFillRatio.Compute(2, i / 10.0, TotalStages);
                Assert.GreaterOrEqual(cur, prev - 0.001f,
                    $"Fill must not decrease within a stage at fraction={i / 10.0}");
                prev = cur;
            }
        }
    }
}
