using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for the micro-feedback motion helpers (issue #24):
    /// press-dip, number-flash, and tap-pulse. No scene, no MonoBehaviour —
    /// all functions are pure math, headlessly verifiable on Linux.
    /// </summary>
    public class MicroFeedbackMotionTests
    {
        // ---- PressDipScale ----

        [Test]
        public void PressDipScale_ReduceMotion_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.PressDipScale(0f, 0.12f, true), 0.001f,
                "ReduceMotion must silence press-dip (no scale motion)");

        [Test]
        public void PressDipScale_AtStart_ReturnsDipValue() =>
            Assert.AreEqual(0.96f, MotionHelper.PressDipScale(0f, 0.12f, false), 0.001f,
                "Scale at elapsed=0 must equal the dip target (0.96)");

        [Test]
        public void PressDipScale_AtEnd_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.PressDipScale(0.12f, 0.12f, false), 0.001f,
                "Scale must fully recover (1.0) by end of duration");

        [Test]
        public void PressDipScale_ZeroDuration_SnapsToOne() =>
            Assert.AreEqual(1f, MotionHelper.PressDipScale(0f, 0f, false), 0.001f,
                "Zero duration must snap directly to 1.0");

        [Test]
        public void PressDipScale_Mid_BetweenDipAndOne()
        {
            float mid = MotionHelper.PressDipScale(0.06f, 0.12f, false);
            Assert.Greater(mid, 0.96f, "Mid-recovery must exceed dip value");
            Assert.Less(mid, 1f, "Mid-recovery must not yet reach 1.0");
        }

        // ---- FlashAlpha ----

        [Test]
        public void FlashAlpha_AtStart_NearZero() =>
            Assert.AreEqual(0f, MotionHelper.FlashAlpha(0f, 0.5f, false), 0.001f,
                "Flash alpha must be zero at the start");

        [Test]
        public void FlashAlpha_AtMidpoint_PeaksNearOne() =>
            Assert.AreEqual(1f, MotionHelper.FlashAlpha(0.25f, 0.5f, false), 0.001f,
                "Flash alpha must peak at ~1.0 at half-duration");

        [Test]
        public void FlashAlpha_AtEnd_NearZero() =>
            Assert.AreEqual(0f, MotionHelper.FlashAlpha(0.5f, 0.5f, false), 0.001f,
                "Flash alpha must return to zero at duration end");

        [Test]
        public void FlashAlpha_ReduceMotion_LowerThanFull()
        {
            float full = MotionHelper.FlashAlpha(0.25f, 0.5f, false);
            float soft = MotionHelper.FlashAlpha(0.25f, 0.5f, true);
            Assert.Less(soft, full,
                "ReduceMotion must soften (reduce) flash alpha, not eliminate it entirely");
        }

        [Test]
        public void FlashAlpha_ReduceMotion_Peak_BelowHalf() =>
            Assert.Less(MotionHelper.FlashAlpha(0.25f, 0.5f, true), 0.5f,
                "ReduceMotion flash peak must be noticeably softer than full intensity");

        [Test]
        public void FlashAlpha_ZeroDuration_ReturnsZero() =>
            Assert.AreEqual(0f, MotionHelper.FlashAlpha(0.5f, 0f, false), 0.001f,
                "Zero duration must return zero immediately");

        [Test]
        public void FlashAlpha_AlwaysInZeroToOne()
        {
            for (float t = 0f; t <= 0.5f; t += 0.05f)
            {
                float v = MotionHelper.FlashAlpha(t, 0.5f, false);
                Assert.GreaterOrEqual(v, 0f, $"FlashAlpha below 0 at t={t}");
                Assert.LessOrEqual(v, 1.001f, $"FlashAlpha above 1 at t={t}");
            }
        }

        // ---- TapPulseScale ----

        [Test]
        public void TapPulseScale_ReduceMotion_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.TapPulseScale(0.125f, 0.25f, 0.04f, true), 0.001f,
                "ReduceMotion must silence tap pulse (no scale motion)");

        [Test]
        public void TapPulseScale_AtStart_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.TapPulseScale(0f, 0.25f, 0.04f, false), 0.001f,
                "Scale at elapsed=0 must be 1.0 (no immediate jump)");

        [Test]
        public void TapPulseScale_AtMidpoint_PeaksAtOnePlusAmplitude() =>
            Assert.AreEqual(1.04f, MotionHelper.TapPulseScale(0.125f, 0.25f, 0.04f, false), 0.001f,
                "Scale at half-duration must reach 1.0 + amplitude (the peak)");

        [Test]
        public void TapPulseScale_AtEnd_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.TapPulseScale(0.25f, 0.25f, 0.04f, false), 0.001f,
                "Scale must return to 1.0 by end of duration");

        [Test]
        public void TapPulseScale_ZeroDuration_ReturnsOne() =>
            Assert.AreEqual(1f, MotionHelper.TapPulseScale(0f, 0f, 0.04f, false), 0.001f,
                "Zero duration must snap to 1.0");

        [Test]
        public void TapPulseScale_NeverBelowOne()
        {
            for (float t = 0f; t <= 0.25f; t += 0.025f)
            {
                float s = MotionHelper.TapPulseScale(t, 0.25f, 0.04f, false);
                Assert.GreaterOrEqual(s, 1f - 0.001f,
                    $"TapPulseScale must never dip below 1.0 (at t={t})");
            }
        }
    }
}
