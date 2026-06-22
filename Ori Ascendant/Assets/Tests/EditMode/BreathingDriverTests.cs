using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Behavior coverage for the hero idle-breathing clock (ADR-0005). Pure struct,
    /// no scene — the breathing math that used to be inline in MainScreenSkin.TickBreathing.
    /// </summary>
    public class BreathingDriverTests
    {
        [Test]
        public void ReduceMotion_FreezesScaleAndBrightness()
        {
            var d = new BreathingDriver();
            var (scale, bright) = d.Tick(0.1f, 1.0f, reduceMotion: true);
            Assert.AreEqual(1.0f, scale, 1e-5f, "Reduce Motion must freeze the breathing scale");
            Assert.AreEqual(1.0f, bright, 1e-5f, "Reduce Motion must freeze the breathing brightness");
        }

        [Test]
        public void TapPulse_MultipliesIntoScale_WhenBreathingSilenced()
        {
            // With breathing silenced (Reduce Motion), scale must equal the tap-pulse passed in.
            var d = new BreathingDriver();
            var (scale, _) = d.Tick(0.016f, 1.04f, reduceMotion: true);
            Assert.AreEqual(1.04f, scale, 1e-5f);
        }

        [Test]
        public void QuarterPeriod_PeaksAtAmplitude()
        {
            // sin peaks at t = period/4 → scale = 1+ScaleAmp, bright = 1+BrightAmp.
            var d = new BreathingDriver();
            var (scale, bright) = d.Tick(BreathingDriver.PeriodSeconds / 4f, 1.0f, reduceMotion: false);
            Assert.AreEqual(1f + BreathingDriver.ScaleAmp, scale, 1e-4f);
            Assert.AreEqual(1f + BreathingDriver.BrightAmp, bright, 1e-4f);
        }

        [Test]
        public void Scale_StaysWithinAmplitudeBand_OverManyFrames()
        {
            var d = new BreathingDriver();
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < 600; i++)
            {
                var (scale, _) = d.Tick(0.016f, 1.0f, reduceMotion: false);
                if (scale < min) min = scale;
                if (scale > max) max = scale;
            }
            Assert.GreaterOrEqual(min, 1f - BreathingDriver.ScaleAmp - 1e-3f, "Scale must not dip below the band");
            Assert.LessOrEqual(max, 1f + BreathingDriver.ScaleAmp + 1e-3f, "Scale must not exceed the band");
        }
    }
}
