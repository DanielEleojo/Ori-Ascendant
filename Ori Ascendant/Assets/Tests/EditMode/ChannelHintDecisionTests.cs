using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #18 (⑤b): lifetime derivation for the persisted channel-hint.
    /// ChannelHintDecision.Evaluate is a pure function — no host, no scene.
    /// </summary>
    public class ChannelHintDecisionTests
    {
        private const long Lifetime = 6L;

        [Test]
        public void NeverShown_ReturnsPending()
        {
            // shownAt == 0 means the save field was never written.
            Assert.AreEqual(ChannelHintState.Pending,
                ChannelHintDecision.Evaluate(shownAt: 0, nowUtc: 1_000_000, Lifetime));
        }

        [Test]
        public void ShownJustNow_ReturnsActive()
        {
            long shownAt = 1_000_000;
            Assert.AreEqual(ChannelHintState.Active,
                ChannelHintDecision.Evaluate(shownAt, nowUtc: shownAt, Lifetime));
        }

        [Test]
        public void ShownWithinLifetime_ReturnsActive()
        {
            long shownAt = 1_000_000;
            long now = shownAt + Lifetime - 1; // one second before expiry
            Assert.AreEqual(ChannelHintState.Active,
                ChannelHintDecision.Evaluate(shownAt, now, Lifetime));
        }

        [Test]
        public void ShownAtExactLifetimeBoundary_ReturnsExpired()
        {
            long shownAt = 1_000_000;
            long now = shownAt + Lifetime; // exactly at expiry
            Assert.AreEqual(ChannelHintState.Expired,
                ChannelHintDecision.Evaluate(shownAt, now, Lifetime));
        }

        [Test]
        public void ShownBeyondLifetime_ReturnsExpired()
        {
            long shownAt = 1_000_000;
            long now = shownAt + Lifetime + 60; // well past expiry
            Assert.AreEqual(ChannelHintState.Expired,
                ChannelHintDecision.Evaluate(shownAt, now, Lifetime));
        }

        [Test]
        public void ActiveWindow_IsExclusive_AtBoundary()
        {
            // Active range is [shownAt, shownAt + lifetime); boundary is Expired.
            long shownAt = 500;
            Assert.AreEqual(ChannelHintState.Active,
                ChannelHintDecision.Evaluate(shownAt, nowUtc: shownAt + Lifetime - 1, Lifetime));
            Assert.AreEqual(ChannelHintState.Expired,
                ChannelHintDecision.Evaluate(shownAt, nowUtc: shownAt + Lifetime, Lifetime));
        }

        [Test]
        public void NeverShown_IsAlwaysPending_RegardlessOfNow()
        {
            // shownAt == 0 is the sentinel — always Pending, whatever the clock.
            foreach (long now in new long[] { 0, 1, 1_000_000, long.MaxValue / 2 })
            {
                Assert.AreEqual(ChannelHintState.Pending,
                    ChannelHintDecision.Evaluate(shownAt: 0, now, Lifetime),
                    $"shownAt=0 must be Pending at now={now}");
            }
        }
    }
}
