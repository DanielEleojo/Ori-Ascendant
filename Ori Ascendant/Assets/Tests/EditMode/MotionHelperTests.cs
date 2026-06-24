using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for MotionHelper (issue #20 / ADR-0005).
    /// No scene, no MonoBehaviour — easing and reduce-motion gating are pure math.
    /// </summary>
    public class MotionHelperTests
    {
        // ---- EaseOut ----

        [Test]
        public void EaseOut_Zero_ReturnsZero() =>
            Assert.AreEqual(0f, MotionHelper.EaseOut(0f), 0.001f);

        [Test]
        public void EaseOut_One_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.EaseOut(1f), 0.001f);

        [Test]
        public void EaseOut_Mid_FasterThanLinear() =>
            Assert.Greater(MotionHelper.EaseOut(0.5f), 0.5f,
                "Ease-out must be faster than linear at midpoint (fast start, soft landing)");

        [Test]
        public void EaseOut_Negative_ClampsToZero() =>
            Assert.AreEqual(0f, MotionHelper.EaseOut(-1f), 0.001f);

        [Test]
        public void EaseOut_OverOne_ClampsToOne() =>
            Assert.AreEqual(1f, MotionHelper.EaseOut(2f), 0.001f);

        [Test]
        public void EaseOut_IsMonotonicallyIncreasing()
        {
            float prev = MotionHelper.EaseOut(0f);
            for (float t = 0.1f; t <= 1f; t += 0.1f)
            {
                float cur = MotionHelper.EaseOut(t);
                Assert.GreaterOrEqual(cur, prev - 0.001f,
                    $"EaseOut must not decrease at t={t}");
                prev = cur;
            }
        }

        // ---- Tween ----

        [Test]
        public void Tween_ReduceMotion_SnapsToTarget()
        {
            float result = MotionHelper.Tween(0f, 1f, 0f, 2f, reduceMotion: true);
            Assert.AreEqual(1f, result, 0.001f,
                "Reduce Motion: tween must snap to target immediately");
        }

        [Test]
        public void Tween_ReduceMotion_PartialElapsed_StillSnaps()
        {
            float result = MotionHelper.Tween(0f, 100f, 0.5f, 1f, reduceMotion: true);
            Assert.AreEqual(100f, result, 0.001f);
        }

        [Test]
        public void Tween_ZeroDuration_SnapsToTarget()
        {
            float result = MotionHelper.Tween(0f, 1f, 0f, 0f, reduceMotion: false);
            Assert.AreEqual(1f, result, 0.001f,
                "Zero duration must snap to target even without Reduce Motion");
        }

        [Test]
        public void Tween_AtStart_ReturnsFrom()
        {
            float result = MotionHelper.Tween(5f, 10f, 0f, 1f, reduceMotion: false);
            Assert.AreEqual(5f, result, 0.001f);
        }

        [Test]
        public void Tween_AtEnd_ReturnsTo()
        {
            float result = MotionHelper.Tween(5f, 10f, 1f, 1f, reduceMotion: false);
            Assert.AreEqual(10f, result, 0.001f);
        }

        [Test]
        public void Tween_Mid_EasedOut_FasterThanLinear()
        {
            // Ease-out: at half duration the value should be past the linear midpoint.
            float result = MotionHelper.Tween(0f, 100f, 0.5f, 1f, reduceMotion: false);
            Assert.Greater(result, 50f,
                "Ease-out at t=0.5 must exceed the linear midpoint (75 expected for quadratic)");
        }

        // ---- BreathingSine ----

        [Test]
        public void BreathingSine_ReduceMotion_ReturnsZero()
        {
            float v = MotionHelper.BreathingSine(1f, 4f, reduceMotion: true);
            Assert.AreEqual(0f, v, 0.001f,
                "BreathingSine must be silent when Reduce Motion is on");
        }

        [Test]
        public void BreathingSine_ReduceMotion_AlwaysZeroAcrossTime()
        {
            for (float t = 0f; t < 10f; t += 0.3f)
                Assert.AreEqual(0f, MotionHelper.BreathingSine(t, 4f, reduceMotion: true), 0.001f,
                    $"BreathingSine must be 0 at t={t} when Reduce Motion is on");
        }

        [Test]
        public void BreathingSine_WithinPlusMinusOne()
        {
            for (float t = 0f; t < 10f; t += 0.1f)
            {
                float v = MotionHelper.BreathingSine(t, 4f, reduceMotion: false);
                Assert.GreaterOrEqual(v, -1.001f, $"BreathingSine below -1 at t={t}");
                Assert.LessOrEqual(v, 1.001f, $"BreathingSine above +1 at t={t}");
            }
        }

        [Test]
        public void BreathingSine_PeakAndTrough_AtQuarterAndThreeQuarterPeriod()
        {
            // sin(2π * 0.25) = +1 at quarter period; sin(2π * 0.75) = -1 at 3/4 period.
            const float period = 4f;
            float peak = MotionHelper.BreathingSine(period * 0.25f, period, reduceMotion: false);
            float trough = MotionHelper.BreathingSine(period * 0.75f, period, reduceMotion: false);
            Assert.AreEqual(1f, peak, 0.001f, "Quarter-period should be the peak (+1)");
            Assert.AreEqual(-1f, trough, 0.001f, "Three-quarter period should be the trough (-1)");
        }

        [Test]
        public void BreathingSine_Oscillates_SignsFlipAcrossHalfPeriod()
        {
            const float period = 4f;
            float atQuarter = MotionHelper.BreathingSine(1f, period, reduceMotion: false);
            float atThreeQuarter = MotionHelper.BreathingSine(3f, period, reduceMotion: false);
            Assert.Greater(atQuarter, 0f, "Quarter period should be positive");
            Assert.Less(atThreeQuarter, 0f, "Three-quarter period should be negative");
        }
    }
}
