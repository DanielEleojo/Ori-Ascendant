using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Behavior coverage for the hero Àṣẹ-counter flash clock (issue #24). Pure struct —
    /// the change-detection + flash math that used to be inline in MainScreenSkin.TickMicroFeedback.
    /// </summary>
    public class AseFlashDriverTests
    {
        [Test]
        public void FirstObservedValue_DoesNotFlash()
        {
            var d = new AseFlashDriver();
            float alpha = d.Tick(0.016f, "100", reduceMotion: false);
            Assert.AreEqual(0f, alpha, 1e-4f, "The first counter value must prime the watcher without flashing");
        }

        [Test]
        public void UnchangedValue_StaysSettled()
        {
            var d = new AseFlashDriver();
            d.Tick(0.016f, "100", false);            // prime
            float alpha = d.Tick(0.016f, "100", false);
            Assert.AreEqual(0f, alpha, 1e-4f, "An unchanged counter value must not flash");
        }

        [Test]
        public void ValueChange_RisesTowardPeak()
        {
            var d = new AseFlashDriver();
            d.Tick(0.016f, "100", false);            // prime
            d.Tick(0.016f, "101", false);            // change → restart flash (alpha 0 at the instant)
            float mid = d.Tick(AseFlashDriver.Duration / 2f, "101", false); // advance to the arch peak
            Assert.Greater(mid, 0.5f, "A value change must produce a visible flash rising toward its peak");
        }

        [Test]
        public void ReduceMotion_SoftensFlashPeak_ButKeepsItVisible()
        {
            var full = new AseFlashDriver();
            full.Tick(0.016f, "100", false);
            full.Tick(0.016f, "101", false);
            float fullPeak = full.Tick(AseFlashDriver.Duration / 2f, "101", reduceMotion: false);

            var rm = new AseFlashDriver();
            rm.Tick(0.016f, "100", true);
            rm.Tick(0.016f, "101", true);
            float rmPeak = rm.Tick(AseFlashDriver.Duration / 2f, "101", reduceMotion: true);

            Assert.Less(rmPeak, fullPeak, "Reduce Motion must soften the flash peak");
            Assert.Greater(rmPeak, 0f, "Reduce Motion still permits an alpha flash (iOS RM allows alpha tweens)");
        }
    }
}
