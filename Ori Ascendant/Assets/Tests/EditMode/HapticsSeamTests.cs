using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OriAscendant.Audio;
using OriAscendant.Core;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Spy implementation — records every call so tests can verify the routing
    /// table without a device.
    /// </summary>
    internal sealed class SpyHaptics : IHapticFeedback
    {
        public readonly List<ImpactStyle> ImpactCalls = new List<ImpactStyle>();
        public readonly List<NotificationStyle> NotifyCalls = new List<NotificationStyle>();
        public int SelectCount;

        public void Impact(ImpactStyle style) => ImpactCalls.Add(style);
        public void Notify(NotificationStyle style) => NotifyCalls.Add(style);
        public void Select() => SelectCount++;
    }

    /// <summary>
    /// Gate: Taptic-Engine seam + event routing (issue #21).
    /// All tests run in the editor (no device required) via the spy.
    /// </summary>
    public class HapticsSeamTests
    {
        // ---- seam shape ----

        [Test]
        public void NullHaptics_AllMethods_NeverThrow()
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

        [Test]
        public void SpyHaptics_RecordsImpact()
        {
            var spy = new SpyHaptics();
            spy.Impact(ImpactStyle.Medium);
            Assert.AreEqual(1, spy.ImpactCalls.Count);
            Assert.AreEqual(ImpactStyle.Medium, spy.ImpactCalls[0]);
        }

        [Test]
        public void SpyHaptics_RecordsNotify()
        {
            var spy = new SpyHaptics();
            spy.Notify(NotificationStyle.Success);
            Assert.AreEqual(1, spy.NotifyCalls.Count);
            Assert.AreEqual(NotificationStyle.Success, spy.NotifyCalls[0]);
        }

        [Test]
        public void SpyHaptics_RecordsSelect()
        {
            var spy = new SpyHaptics();
            spy.Select();
            Assert.AreEqual(1, spy.SelectCount);
        }

        // ---- event → haptic routing (HapticRouter) ----

        [Test]
        public void ChannelTap_TriggersSelect()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteChanneled(spy);
            Assert.AreEqual(1, spy.SelectCount, "channel tap must fire Select()");
            Assert.AreEqual(0, spy.ImpactCalls.Count);
            Assert.AreEqual(0, spy.NotifyCalls.Count);
        }

        [Test]
        public void StageAdvance_TriggersImpactMedium()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteStageAdvanced(spy);
            Assert.AreEqual(1, spy.ImpactCalls.Count);
            Assert.AreEqual(ImpactStyle.Medium, spy.ImpactCalls[0], "stage advance must fire Impact(Medium)");
        }

        [Test]
        public void Ascended_TriggersNotifySuccess()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteTribulationComplete(spy, didAscend: true);
            Assert.AreEqual(1, spy.NotifyCalls.Count);
            Assert.AreEqual(NotificationStyle.Success, spy.NotifyCalls[0], "Ascended must fire Notify(Success)");
            Assert.AreEqual(0, spy.ImpactCalls.Count);
        }

        [Test]
        public void Fall_TriggersImpactLight_NeverError()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteTribulationComplete(spy, didAscend: false);
            // ART_BIBLE 3.2: a fall is not failure — must NOT use Notify(Warning/Error)
            Assert.AreEqual(0, spy.NotifyCalls.Count, "Fall must never fire a notification haptic (ART_BIBLE 3.2)");
            Assert.AreEqual(1, spy.ImpactCalls.Count);
            Assert.AreEqual(ImpactStyle.Light, spy.ImpactCalls[0], "Fall must fire Impact(Light) — soft, not harsh");
        }

        [Test]
        public void AncestorStarIgnite_TriggersImpactLight()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteAncestorStarIgnite(spy);
            Assert.AreEqual(1, spy.ImpactCalls.Count);
            Assert.AreEqual(ImpactStyle.Light, spy.ImpactCalls[0], "star ignite must fire a warm Impact(Light)");
            Assert.AreEqual(0, spy.NotifyCalls.Count);
        }

        [Test]
        public void ContestWin_TriggersNotifySuccess()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteContestResolved(spy, didWin: true);
            Assert.AreEqual(1, spy.NotifyCalls.Count);
            Assert.AreEqual(NotificationStyle.Success, spy.NotifyCalls[0], "contest win must fire Notify(Success)");
            Assert.AreEqual(0, spy.ImpactCalls.Count);
        }

        [Test]
        public void ContestLoss_TriggersImpactLight_NeverWarning()
        {
            // Mirrors the Fall rule (ART_BIBLE 3.2): a loss lands soft — never Notify(Warning).
            var spy = new SpyHaptics();
            HapticRouter.RouteContestResolved(spy, didWin: false);
            Assert.AreEqual(0, spy.NotifyCalls.Count, "contest loss must never fire a notification haptic");
            Assert.AreEqual(1, spy.ImpactCalls.Count);
            Assert.AreEqual(ImpactStyle.Light, spy.ImpactCalls[0], "contest loss must fire Impact(Light) — soft, not harsh");
        }

        [Test]
        public void TribulationComplete_Fall_ImpactNotNotification()
        {
            // Regression guard for ART_BIBLE 3.2: ensure no future refactor
            // accidentally swaps Impact(Light) for Notify(Warning).
            var spy = new SpyHaptics();
            HapticRouter.RouteTribulationComplete(spy, didAscend: false);
            foreach (var n in spy.NotifyCalls)
                Assert.AreNotEqual(NotificationStyle.Warning, n,
                    "Fall must never use the Warning notification haptic");
        }

        // ---- issue #6: ButtonPressDip press-down fires Select haptic ----

        [Test]
        public void ButtonPressDip_OnPointerDown_FiresSelectHaptic()
        {
            ServiceLocator.Clear();
            var host = new GameObject("DipHapticsTest");
            try
            {
                var audio = host.AddComponent<AudioManager>();

                // Inject spy directly into AudioManager._haptics.
                var spy = new SpyHaptics();
                typeof(AudioManager)
                    .GetField("_haptics", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(audio, spy);

                // Ensure AudioManager is registered (belt-and-suspenders for EditMode Awake timing).
                ServiceLocator.Register(audio);

                var dip = host.AddComponent<ButtonPressDip>();

                // Also inject _audio directly in case ServiceLocator wiring raced in EditMode.
                typeof(ButtonPressDip)
                    .GetField("_audio", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(dip, audio);

                dip.OnPointerDown(null);

                Assert.AreEqual(1, spy.SelectCount,
                    "ButtonPressDip press-down must fire Select haptic via AudioManager.PlaySelect() (#6)");
                Assert.AreEqual(0, spy.ImpactCalls.Count,
                    "press-down must not fire an Impact haptic");
            }
            finally
            {
                Object.DestroyImmediate(host);
                ServiceLocator.Clear();
            }
        }
    }
}
