using NUnit.Framework;
using OriAscendant.Audio;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>Gate D: the pure audio cores (track selection + crossfade math).</summary>
    public class AudioCoreTests
    {
        [TestCase(-1, 0)] // path-less → default theme
        [TestCase(0, 1)]  // Ane
        [TestCase(1, 2)]  // Sango
        [TestCase(2, 3)]  // Osun
        [TestCase(99, 0)] // out of range → default
        public void ThemeIndex_MapsPathToSlot(int path, int expected)
        {
            Assert.AreEqual(expected, AudioTrackSelector.ThemeIndexForPath(path));
        }

        [Test]
        public void Crossfade_StartsAtZeroIncoming_FullOutgoing()
        {
            var fade = new AudioCrossfade();
            fade.Begin(1.0f);
            Assert.IsTrue(fade.IsFading);
            Assert.AreEqual(0f, fade.IncomingVolume, 1e-6);
            Assert.AreEqual(1f, fade.OutgoingVolume, 1e-6);
        }

        [Test]
        public void Crossfade_Midpoint_BlendsEvenly()
        {
            var fade = new AudioCrossfade();
            fade.Begin(1.0f);
            fade.Tick(0.5f);
            Assert.AreEqual(0.5f, fade.IncomingVolume, 1e-6);
            Assert.AreEqual(0.5f, fade.OutgoingVolume, 1e-6);
            Assert.IsTrue(fade.IsFading);
        }

        [Test]
        public void Crossfade_CompletesAtDuration()
        {
            var fade = new AudioCrossfade();
            fade.Begin(1.0f);
            fade.Tick(1.0f);
            Assert.AreEqual(1f, fade.IncomingVolume, 1e-6);
            Assert.AreEqual(0f, fade.OutgoingVolume, 1e-6);
            Assert.IsFalse(fade.IsFading);
        }

        [Test]
        public void Crossfade_Overshoot_ClampsAndStops()
        {
            var fade = new AudioCrossfade();
            fade.Begin(1.0f);
            fade.Tick(5.0f);
            Assert.AreEqual(1f, fade.IncomingVolume, 1e-6);
            Assert.IsFalse(fade.IsFading);
        }

        [Test]
        public void NullHaptics_NeverThrows()
        {
            IHapticFeedback h = new NullHaptics();
            Assert.DoesNotThrow(() => { h.Light(); h.Medium(); h.Heavy(); });
        }
    }
}
