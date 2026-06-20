using System.Collections.Generic;
using NUnit.Framework;
using OriAscendant.Audio;

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
            HapticRouter.RouteTribulationComplete(didAscend: true, spy);
            Assert.AreEqual(1, spy.NotifyCalls.Count);
            Assert.AreEqual(NotificationStyle.Success, spy.NotifyCalls[0], "Ascended must fire Notify(Success)");
            Assert.AreEqual(0, spy.ImpactCalls.Count);
        }

        [Test]
        public void Fall_TriggersImpactLight_NeverError()
        {
            var spy = new SpyHaptics();
            HapticRouter.RouteTribulationComplete(didAscend: false, spy);
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
        public void TribulationComplete_Fall_ImpactNotNotification()
        {
            // Regression guard for ART_BIBLE 3.2: ensure no future refactor
            // accidentally swaps Impact(Light) for Notify(Warning).
            var spy = new SpyHaptics();
            HapticRouter.RouteTribulationComplete(didAscend: false, spy);
            foreach (var n in spy.NotifyCalls)
                Assert.AreNotEqual(NotificationStyle.Warning, n,
                    "Fall must never use the Warning notification haptic");
        }
    }
}
