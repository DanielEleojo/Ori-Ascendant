using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for OverlayTransition (issue #23).
    /// No scene, no MonoBehaviour — the struct is pure math over elapsed time.
    /// </summary>
    public class OverlayTransitionTests
    {
        // ---- initial state ----

        [Test]
        public void NewTransition_IsFullyClosed()
        {
            var t = new OverlayTransition();
            Assert.IsTrue(t.IsFullyClosed, "A fresh OverlayTransition must start fully closed");
        }

        [Test]
        public void NewTransition_Tick_ReturnsZeroAlpha()
        {
            var t = new OverlayTransition();
            var (alpha, _) = t.Tick(0.1f, false);
            Assert.AreEqual(0f, alpha, 0.001f, "Closed transition must return alpha=0");
        }

        // ---- open animation ----

        [Test]
        public void Open_IsNotFullyClosed()
        {
            var t = new OverlayTransition();
            t.Open();
            Assert.IsFalse(t.IsFullyClosed, "Opening transition must not be fully closed");
        }

        [Test]
        public void Open_AtZeroDt_AlphaIsZero()
        {
            var t = new OverlayTransition();
            t.Open();
            var (alpha, _) = t.Tick(0f, false);
            Assert.AreEqual(0f, alpha, 0.001f, "At elapsed=0 the alpha must be 0");
        }

        [Test]
        public void Open_AfterFullDuration_AlphaIsOne()
        {
            var t = new OverlayTransition();
            t.Open();
            var (alpha, _) = t.Tick(OverlayTransition.Duration, false);
            Assert.AreEqual(1f, alpha, 0.001f, "After full duration alpha must reach 1");
        }

        [Test]
        public void Open_AfterFullDuration_ScaleIsOne()
        {
            var t = new OverlayTransition();
            t.Open();
            var (_, scale) = t.Tick(OverlayTransition.Duration, false);
            Assert.AreEqual(1f, scale, 0.001f, "After full duration scale must reach 1");
        }

        [Test]
        public void Open_AfterFullDuration_IsFullyOpenNotClosed()
        {
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false);
            Assert.IsFalse(t.IsFullyClosed, "After open completes the overlay is open, not closed");
        }

        [Test]
        public void Open_MidDuration_AlphaEasedOutFasterThanLinear()
        {
            var t = new OverlayTransition();
            t.Open();
            var (alpha, _) = t.Tick(OverlayTransition.Duration * 0.5f, false);
            Assert.Greater(alpha, 0.5f,
                "Ease-out at half duration must be past the linear midpoint");
        }

        [Test]
        public void Open_AtStart_ScaleIsScaleFrom()
        {
            var t = new OverlayTransition();
            t.Open();
            // At t=0, EaseOut(0) = 0, so scale = Lerp(ScaleFrom, 1, 0) = ScaleFrom
            var (_, scale) = t.Tick(0f, false);
            Assert.AreEqual(OverlayTransition.ScaleFrom, scale, 0.001f,
                "Scale at open start must be ScaleFrom");
        }

        // ---- reduce motion ----

        [Test]
        public void Open_ReduceMotion_ScaleAlwaysOne()
        {
            var t = new OverlayTransition();
            t.Open();
            var (_, scale) = t.Tick(0f, reduceMotion: true);
            Assert.AreEqual(1f, scale, 0.001f,
                "Reduce Motion: scale must stay at 1 (no scale tween)");
        }

        [Test]
        public void Open_ReduceMotion_AlphaStillTweens()
        {
            var t = new OverlayTransition();
            t.Open();
            // Alpha should still go from 0 → 1 even with reduce motion
            var (alphaStart, _) = t.Tick(0f, reduceMotion: true);
            Assert.AreEqual(0f, alphaStart, 0.001f, "Alpha starts at 0 with reduce motion");

            t.Open(); // reset
            var (alphaFull, _) = t.Tick(OverlayTransition.Duration, reduceMotion: true);
            Assert.AreEqual(1f, alphaFull, 0.001f, "Alpha reaches 1 after duration with reduce motion");
        }

        [Test]
        public void Close_ReduceMotion_ScaleAlwaysOne()
        {
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false); // complete open
            t.Close();
            var (_, scale) = t.Tick(0f, reduceMotion: true);
            Assert.AreEqual(1f, scale, 0.001f,
                "Reduce Motion: scale during close must stay at 1");
        }

        // ---- close animation ----

        [Test]
        public void Close_AfterOpen_StartsWithAlphaOne()
        {
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false);
            t.Close();
            var (alpha, _) = t.Tick(0f, false);
            Assert.AreEqual(1f, alpha, 0.001f, "At start of close alpha must still be 1");
        }

        [Test]
        public void Close_AfterFullDuration_AlphaIsZero()
        {
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false);
            t.Close();
            var (alpha, _) = t.Tick(OverlayTransition.Duration, false);
            Assert.AreEqual(0f, alpha, 0.001f, "After full close duration alpha must be 0");
        }

        [Test]
        public void Close_AfterFullDuration_IsFullyClosed()
        {
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false);
            t.Close();
            t.Tick(OverlayTransition.Duration, false);
            Assert.IsTrue(t.IsFullyClosed, "After close completes, IsFullyClosed must be true");
        }

        [Test]
        public void Close_AfterFullDuration_ScaleNeverExceedsOne()
        {
            // Scale at end of close is ScaleFrom (0.97), but IsFullyClosed is true.
            // Callers gate on IsFullyClosed before calling SetActive(false), so the
            // in-flight scale value when IsFullyClosed is true doesn't matter in
            // practice — but it must not be > 1.
            var t = new OverlayTransition();
            t.Open();
            t.Tick(OverlayTransition.Duration, false);
            t.Close();
            var (_, scale) = t.Tick(OverlayTransition.Duration, false);
            Assert.LessOrEqual(scale, 1f + 0.001f,
                "Scale at close completion must be ≤ 1");
        }

        [Test]
        public void ClosedTransition_CloseNoop_RemainsFullyClosed()
        {
            var t = new OverlayTransition();
            t.Close(); // calling Close on a brand-new closed transition must not throw
            Assert.IsTrue(t.IsFullyClosed);
        }

        // ---- alpha monotonicity ----

        [Test]
        public void Open_AlphaIsMonotonicallyIncreasing()
        {
            float prev = 0f;
            float step = OverlayTransition.Duration / 5f;
            for (float elapsed = step; elapsed <= OverlayTransition.Duration + 0.001f; elapsed += step)
            {
                var t = new OverlayTransition();
                t.Open();
                var (alpha, _) = t.Tick(elapsed, false);
                Assert.GreaterOrEqual(alpha, prev - 0.001f,
                    $"Alpha must not decrease during open at elapsed={elapsed}");
                prev = alpha;
            }
        }
    }
}
