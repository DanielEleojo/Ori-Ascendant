using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: the pure offline-progress math — clamp behavior, fresh-install
    /// guard, path offline modifiers, and the two intent-named save mutations
    /// (first-launch init vs resume accrual; issue #17 / PRD #13 ⑥).
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

        // ---- Split mutation API (issue #17): InitializeFirstLaunch vs ApplyAccrual ----

        [Test]
        public void InitializeFirstLaunch_StampsBothTimestamps_AndCreditsZeroAse()
        {
            // A fresh install MUST NOT credit any Àṣẹ — both timestamps stamp to now.
            var save = new SaveData();
            BigNumber preAse = save.GetAse();

            OfflineProgressCalculator.InitializeFirstLaunch(save, Now);

            Assert.AreEqual(preAse, save.GetAse(), "first launch credits zero Àṣẹ");
            Assert.IsTrue(save.GetAse().IsZero);
            Assert.AreEqual(Now, save.lastSaveTimestamp);
            Assert.AreEqual(Now, save.generationStartTimestamp, "first launch begins generation 1");
        }

        [Test]
        public void InitializeFirstLaunch_DoesNotRaiseEvent()
        {
            // First launch is not an "offline progress applied" moment — the
            // Welcome Back UI must not flash on a brand-new install.
            var save = new SaveData();

            bool raised = false;
            void Handler(BigNumber _, long __) { raised = true; }

            OfflineProgressCalculator.OnOfflineProgressApplied += Handler;
            try
            {
                OfflineProgressCalculator.InitializeFirstLaunch(save, Now);
            }
            finally
            {
                OfflineProgressCalculator.OnOfflineProgressApplied -= Handler;
            }

            Assert.IsFalse(raised, "first-launch init must not fire the offline-progress event");
        }

        [Test]
        public void ApplyAccrual_CreditsAse_AndStampsLastSave()
        {
            var save = new SaveData { lastSaveTimestamp = Now - 600, generationStartTimestamp = Now - 5000 };
            save.SetAse(new BigNumber(500.0, 0));
            save.SetAsePerSecond(Rate10);

            var r = OfflineProgressCalculator.ApplyAccrual(save, Now, 1.0);

            Assert.IsFalse(r.IsFirstLaunch);
            Assert.AreEqual(600, r.CountedSeconds);
            Assert.AreEqual(BigNumber.FromDouble(6_500.0), save.GetAse()); // 500 + 10×600
            Assert.AreEqual(Now, save.lastSaveTimestamp);
            Assert.AreEqual(Now - 5000, save.generationStartTimestamp,
                "accrual must never touch generationStartTimestamp");
        }

        [Test]
        public void ApplyAccrual_Idempotent_DoesNotReinitGenerationTimestamp()
        {
            // Acceptance: applying accrual twice does not re-initialize the
            // generation timestamp (the only way to set it is first-launch or
            // a Tribulation resolve — never accrual).
            long genStart = Now - 12345;
            var save = new SaveData { lastSaveTimestamp = Now - 100, generationStartTimestamp = genStart };
            save.SetAsePerSecond(Rate10);

            OfflineProgressCalculator.ApplyAccrual(save, Now, 1.0);
            Assert.AreEqual(genStart, save.generationStartTimestamp, "first accrual must not stamp generation");

            // Simulate a later resume — accrual again with a fresh elapsed window.
            OfflineProgressCalculator.ApplyAccrual(save, Now + 200, 1.0);

            Assert.AreEqual(genStart, save.generationStartTimestamp, "second accrual must not stamp generation");
            Assert.AreEqual(Now + 200, save.lastSaveTimestamp);
        }

        [Test]
        public void ApplyAccrual_RaisesEvent_WithEarnedAndSeconds()
        {
            var save = new SaveData { lastSaveTimestamp = Now - 120, generationStartTimestamp = Now - 1000 };
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
                OfflineProgressCalculator.ApplyAccrual(save, Now, 1.0);
            }
            finally
            {
                OfflineProgressCalculator.OnOfflineProgressApplied -= Handler;
            }

            Assert.AreEqual(120, eventSeconds);
            Assert.AreEqual(BigNumber.FromDouble(1200.0), eventEarned);
        }

        [Test]
        public void ApplyAccrual_OnFreshSave_IsNoOp_AndDoesNotInitialize()
        {
            // Defense-in-depth: if the caller routes a fresh save through
            // accrual by mistake, accrual must NOT secretly initialize the
            // timestamps (that is first-launch's job). It credits zero and
            // leaves the save alone for the lifecycle owner to notice.
            var save = new SaveData(); // lastSaveTimestamp == 0

            var r = OfflineProgressCalculator.ApplyAccrual(save, Now, 1.0);

            Assert.IsTrue(r.IsFirstLaunch, "Compute reports first-launch on lastSave==0");
            Assert.IsTrue(save.GetAse().IsZero);
            Assert.AreEqual(0, save.lastSaveTimestamp, "accrual must not stamp on a fresh save");
            Assert.AreEqual(0, save.generationStartTimestamp);
        }
    }
}
