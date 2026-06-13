using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: the pure offline-progress math — clamp behavior, fresh-install
    /// guard, path offline modifiers, and Apply's mutation contract.
    /// </summary>
    public class OfflineProgressCalculatorTests
    {
        private const long Now = 1_781_136_000; // fixed "now" (Unix UTC)

        private static readonly BigNumber Rate10 = new BigNumber(10.0, 0); // 10 Àṣẹ/s

        [Test]
        public void ZeroElapsed_EarnsNothing()
        {
            var r = OfflineProgressCalculator.Compute(Now, Now, Rate10, 1.0);

            Assert.IsFalse(r.IsFirstLaunch);
            Assert.AreEqual(0, r.CountedSeconds);
            Assert.IsTrue(r.Earned.IsZero);
        }

        [Test]
        public void FutureTimestamp_ClampsToZero_NeverNegative()
        {
            // Clock skew / cloud merge can put lastSave in the future. Àṣẹ must
            // never regress (locked business rule).
            var r = OfflineProgressCalculator.Compute(Now + 9999, Now, Rate10, 1.0);

            Assert.AreEqual(0, r.CountedSeconds);
            Assert.IsTrue(r.Earned.IsZero);
        }

        [Test]
        public void ExactCap_Counts28800Seconds()
        {
            var r = OfflineProgressCalculator.Compute(Now - 28800, Now, Rate10, 1.0);

            Assert.AreEqual(28800, r.CountedSeconds);
            Assert.AreEqual(new BigNumber(288.0, 3), r.Earned); // 10 × 28800 = 288,000
        }

        [Test]
        public void BeyondCap_StillCounts28800()
        {
            var r = OfflineProgressCalculator.Compute(Now - 100_000, Now, Rate10, 1.0);

            Assert.AreEqual(28800, r.CountedSeconds);
            Assert.AreEqual(new BigNumber(288.0, 3), r.Earned);
        }

        [Test]
        public void FreshInstall_NoGain_NoFree8Hours()
        {
            // lastSaveTimestamp == 0 means "never saved" — without this guard a
            // brand-new player would bank a free 8 hours on first launch.
            var r = OfflineProgressCalculator.Compute(0, Now, Rate10, 1.0);

            Assert.IsTrue(r.IsFirstLaunch);
            Assert.AreEqual(0, r.CountedSeconds);
            Assert.IsTrue(r.Earned.IsZero);
        }

        [TestCase(1.5, 15_000.0)] // Ane: ×1.5 offline
        [TestCase(0.5, 5_000.0)]  // Sango: ×0.5 (net offline ×1.0 vs its ×2 online)
        public void OfflineRateModifier_ScalesEarnings_NotTime(double modifier, double expected)
        {
            var r = OfflineProgressCalculator.Compute(Now - 1000, Now, Rate10, modifier);

            Assert.AreEqual(1000, r.CountedSeconds, "modifier must never change counted TIME");
            Assert.AreEqual(BigNumber.FromDouble(expected), r.Earned);
        }

        [Test]
        public void Apply_CreditsAse_AndStampsTimestamp()
        {
            var save = new SaveData { lastSaveTimestamp = Now - 600 };
            save.SetAse(new BigNumber(500.0, 0));
            save.SetAsePerSecond(Rate10);

            var r = OfflineProgressCalculator.Apply(save, Now, 1.0);

            Assert.AreEqual(600, r.CountedSeconds);
            Assert.AreEqual(BigNumber.FromDouble(6_500.0), save.GetAse()); // 500 + 10×600
            Assert.AreEqual(Now, save.lastSaveTimestamp);
        }

        [Test]
        public void Apply_FirstLaunch_StampsBothTimestamps_AndKeepsZeroAse()
        {
            var save = new SaveData();

            var r = OfflineProgressCalculator.Apply(save, Now, 1.0);

            Assert.IsTrue(r.IsFirstLaunch);
            Assert.IsTrue(save.GetAse().IsZero);
            Assert.AreEqual(Now, save.lastSaveTimestamp);
            Assert.AreEqual(Now, save.generationStartTimestamp, "first launch begins generation 1");
        }

        [Test]
        public void Apply_RaisesEvent_WithEarnedAndSeconds()
        {
            var save = new SaveData { lastSaveTimestamp = Now - 120 };
            save.SetAsePerSecond(Rate10);

            BigNumber eventEarned = BigNumber.Zero;
            long eventSeconds = -1;
            void Handler(BigNumber earned, long seconds)
            {
                eventEarned = earned;
                eventSeconds = seconds;
            }

            OfflineProgressCalculator.OnOfflineProgressApplied += Handler;
            try
            {
                OfflineProgressCalculator.Apply(save, Now, 1.0);
            }
            finally
            {
                OfflineProgressCalculator.OnOfflineProgressApplied -= Handler;
            }

            Assert.AreEqual(120, eventSeconds);
            Assert.AreEqual(BigNumber.FromDouble(1200.0), eventEarned);
        }
    }
}
