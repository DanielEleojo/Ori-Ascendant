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
            // Equal-power law: both tracks sit at sin(45°) ≈ 0.7071 so the summed
            // POWER (v²) holds at 1.0 — a linear blend sags ~-3dB at this midpoint.
            Assert.AreEqual(0.70710678f, fade.IncomingVolume, 1e-5);
            Assert.AreEqual(fade.IncomingVolume, fade.OutgoingVolume, 1e-5);
            Assert.AreEqual(1f,
                fade.IncomingVolume * fade.IncomingVolume +
                fade.OutgoingVolume * fade.OutgoingVolume, 1e-5);
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
            Assert.DoesNotThrow(() =>
            {
                h.Impact(ImpactStyle.Light);
                h.Impact(ImpactStyle.Medium);
                h.Impact(ImpactStyle.Heavy);
                h.Notify(NotificationStyle.Success);
                h.Notify(NotificationStyle.Warning);
                h.Select();
            });
        }
    }
}
