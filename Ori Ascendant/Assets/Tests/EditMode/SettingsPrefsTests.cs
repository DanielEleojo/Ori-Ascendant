using NUnit.Framework;
using OriAscendant.Audio;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate: Settings toggles persistence + haptic gating (issue #31).
    /// Headless-safe: no MonoBehaviour, no scene.
    /// </summary>
    public class SettingsPrefsTests
    {
        private const string HapticsKey = "ori_haptics_enabled";
        private const string ReduceMotionKey = "ReduceMotion";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(HapticsKey);
            PlayerPrefs.DeleteKey(ReduceMotionKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(HapticsKey);
            PlayerPrefs.DeleteKey(ReduceMotionKey);
        }

        // ---- HapticPrefs ----

        [Test]
        public void HapticPrefs_DefaultsTrue()
        {
            Assert.IsTrue(HapticPrefs.HapticsEnabled,
                "Haptics should be on by default (no PlayerPrefs key set)");
        }

        [Test]
        public void HapticPrefs_SetFalse_Persists()
        {
            HapticPrefs.HapticsEnabled = false;
            Assert.IsFalse(HapticPrefs.HapticsEnabled,
                "Haptics should remain off after being set to false");
        }

        [Test]
        public void HapticPrefs_SetTrue_Persists()
        {
            HapticPrefs.HapticsEnabled = false;
            HapticPrefs.HapticsEnabled = true;
            Assert.IsTrue(HapticPrefs.HapticsEnabled,
                "Haptics should be on after being re-enabled");
        }

        // ---- MotionPrefs ----

        [Test]
        public void MotionPrefs_DefaultsFalse()
        {
            Assert.IsFalse(MotionPrefs.ReduceMotionEnabled,
                "Reduce Motion should be off by default");
        }

        [Test]
        public void MotionPrefs_SetTrue_Persists()
        {
            MotionPrefs.ReduceMotionEnabled = true;
            Assert.IsTrue(MotionPrefs.ReduceMotionEnabled,
                "Reduce Motion should remain on after being set to true");
        }

        [Test]
        public void MotionPrefs_SetFalse_Persists()
        {
            MotionPrefs.ReduceMotionEnabled = true;
            MotionPrefs.ReduceMotionEnabled = false;
            Assert.IsFalse(MotionPrefs.ReduceMotionEnabled,
                "Reduce Motion should be off after being re-disabled");
        }

        // ---- GatedHaptics ----

        [Test]
        public void GatedHaptics_WhenEnabled_ForwardsImpact()
        {
            HapticPrefs.HapticsEnabled = true;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Impact(ImpactStyle.Medium);
            Assert.AreEqual(1, spy.ImpactCalls.Count,
                "GatedHaptics must forward Impact when haptics are enabled");
            Assert.AreEqual(ImpactStyle.Medium, spy.ImpactCalls[0]);
        }

        [Test]
        public void GatedHaptics_WhenEnabled_ForwardsNotify()
        {
            HapticPrefs.HapticsEnabled = true;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Notify(NotificationStyle.Success);
            Assert.AreEqual(1, spy.NotifyCalls.Count,
                "GatedHaptics must forward Notify when haptics are enabled");
        }

        [Test]
        public void GatedHaptics_WhenEnabled_ForwardsSelect()
        {
            HapticPrefs.HapticsEnabled = true;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Select();
            Assert.AreEqual(1, spy.SelectCount,
                "GatedHaptics must forward Select when haptics are enabled");
        }

        [Test]
        public void GatedHaptics_WhenDisabled_SuppressesImpact()
        {
            HapticPrefs.HapticsEnabled = false;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Impact(ImpactStyle.Heavy);
            Assert.AreEqual(0, spy.ImpactCalls.Count,
                "GatedHaptics must suppress Impact when haptics are disabled");
        }

        [Test]
        public void GatedHaptics_WhenDisabled_SuppressesNotify()
        {
            HapticPrefs.HapticsEnabled = false;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Notify(NotificationStyle.Success);
            Assert.AreEqual(0, spy.NotifyCalls.Count,
                "GatedHaptics must suppress Notify when haptics are disabled");
        }

        [Test]
        public void GatedHaptics_WhenDisabled_SuppressesSelect()
        {
            HapticPrefs.HapticsEnabled = false;
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);
            gated.Select();
            Assert.AreEqual(0, spy.SelectCount,
                "GatedHaptics must suppress Select when haptics are disabled");
        }

        [Test]
        public void GatedHaptics_Toggle_RespondsAtCallTime()
        {
            var spy = new SpyHaptics();
            var gated = new GatedHaptics(spy);

            HapticPrefs.HapticsEnabled = true;
            gated.Impact(ImpactStyle.Light);

            HapticPrefs.HapticsEnabled = false;
            gated.Impact(ImpactStyle.Light);

            Assert.AreEqual(1, spy.ImpactCalls.Count,
                "GatedHaptics must stop forwarding immediately after the toggle is disabled");
        }
    }
}
