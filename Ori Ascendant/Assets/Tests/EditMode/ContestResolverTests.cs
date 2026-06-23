using NUnit.Framework;
using OriAscendant.Data;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ContestResolver (issue #37): StanceCompare triangle, ComputeOdds
    /// tilting and clamping, and Resolve roll + stake math. All doubles asserted within 1e-12.
    /// </summary>
    public class ContestResolverTests
    {
        private const double Tol = 1e-12;
        private ContestConfig _config;

        [SetUp]
        public void SetUp() => _config = EditModeTestHelpers.MakeContestConfig();

        // ── StanceCompare ─────────────────────────────────────────────────────────

        [Test]
        public void StanceCompare_Tie_ReturnsZero()
        {
            Assert.AreEqual(0, ContestResolver.StanceCompare(Stance.Strike, Stance.Strike));
            Assert.AreEqual(0, ContestResolver.StanceCompare(Stance.Endure, Stance.Endure));
            Assert.AreEqual(0, ContestResolver.StanceCompare(Stance.Flow,   Stance.Flow));
        }

        [Test]
        public void StanceCompare_StrikeBeatsEndure()
        {
            Assert.AreEqual( 1, ContestResolver.StanceCompare(Stance.Strike, Stance.Endure));
            Assert.AreEqual(-1, ContestResolver.StanceCompare(Stance.Endure, Stance.Strike));
        }

        [Test]
        public void StanceCompare_EndureBeatsFlow()
        {
            Assert.AreEqual( 1, ContestResolver.StanceCompare(Stance.Endure, Stance.Flow));
            Assert.AreEqual(-1, ContestResolver.StanceCompare(Stance.Flow,   Stance.Endure));
        }

        [Test]
        public void StanceCompare_FlowBeatsStrike()
        {
            Assert.AreEqual( 1, ContestResolver.StanceCompare(Stance.Flow,   Stance.Strike));
            Assert.AreEqual(-1, ContestResolver.StanceCompare(Stance.Strike, Stance.Flow));
        }

        // ── ComputeOdds ───────────────────────────────────────────────────────────

        [Test]
        public void ComputeOdds_EvenStanceAndPower_Returns0Point5()
        {
            // tie stance, ratio 1.0 → 0.5 + 0 + 0.30*(1-1) = 0.5
            double odds = ContestResolver.ComputeOdds(Stance.Strike, Stance.Strike, 1.0, _config);
            Assert.AreEqual(0.5, odds, Tol);
        }

        [Test]
        public void ComputeOdds_PlayerWinsStance_TiltsUp()
        {
            // Strike vs Endure (+1 tilt), ratio 1.0 → 0.5 + 0.20 = 0.70
            double odds = ContestResolver.ComputeOdds(Stance.Strike, Stance.Endure, 1.0, _config);
            Assert.AreEqual(0.70, odds, Tol);
        }

        [Test]
        public void ComputeOdds_PlayerLosesStance_TiltsDown()
        {
            // Endure vs Strike (-1 tilt), ratio 1.0 → 0.5 - 0.20 = 0.30
            double odds = ContestResolver.ComputeOdds(Stance.Endure, Stance.Strike, 1.0, _config);
            Assert.AreEqual(0.30, odds, Tol);
        }

        [Test]
        public void ComputeOdds_WeakerHouse_TiltsUp()
        {
            // tie stance, ratio 0.5 → 0.5 + 0.30*(1-0.5) = 0.5 + 0.15 = 0.65
            double odds = ContestResolver.ComputeOdds(Stance.Strike, Stance.Strike, 0.5, _config);
            Assert.AreEqual(0.65, odds, Tol);
        }

        [Test]
        public void ComputeOdds_StrongerHouse_TiltsDown()
        {
            // tie stance, ratio 1.5 → 0.5 + 0.30*(1-1.5) = 0.5 - 0.15 = 0.35
            double odds = ContestResolver.ComputeOdds(Stance.Strike, Stance.Strike, 1.5, _config);
            Assert.AreEqual(0.35, odds, Tol);
        }

        [Test]
        public void ComputeOdds_LargePlayerAdvantage_ClampsToMax()
        {
            // odds would exceed 0.90 — clamp at oddsMax
            // tie stance, ratio 0.0 → 0.5 + 0.30*1.0 = 0.80; add stance win: 0.80 + 0.20 = 1.0 → clamped 0.90
            double odds = ContestResolver.ComputeOdds(Stance.Strike, Stance.Endure, 0.0, _config);
            Assert.AreEqual(0.90, odds, Tol);
        }

        [Test]
        public void ComputeOdds_LargePlayerDisadvantage_ClampsToMin()
        {
            // odds would fall below 0.10 — clamp at oddsMin
            // player loses stance: -0.20, ratio 2.0 → 0.5 - 0.20 + 0.30*(1-2.0) = 0.5 - 0.20 - 0.30 = 0.0 → clamped 0.10
            double odds = ContestResolver.ComputeOdds(Stance.Endure, Stance.Strike, 2.0, _config);
            Assert.AreEqual(0.10, odds, Tol);
        }

        // ── Resolve ───────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_RollBelowOdds_Won_PositiveDelta()
        {
            // odds=0.70, roll=0.5 (0.5 < 0.7 → won)
            // stake = 0.10 * (1 - 0.70) = 0.03; delta = +0.03
            var outcome = ContestResolver.Resolve(0.70, _config, new FakeRandom(0.5));
            Assert.IsTrue(outcome.Won);
            Assert.AreEqual(0.03, outcome.RenownDelta, Tol);
            Assert.AreEqual(0.70, outcome.Odds, Tol);
        }

        [Test]
        public void Resolve_RollAtOrAboveOdds_Lost_NegativeSoftenedDelta()
        {
            // odds=0.70, roll=0.95 (0.95 >= 0.7 → lost)
            // stake = 0.10 * 0.30 = 0.03; delta = -0.03 * 0.5 = -0.015
            var outcome = ContestResolver.Resolve(0.70, _config, new FakeRandom(0.95));
            Assert.IsFalse(outcome.Won);
            Assert.AreEqual(-0.015, outcome.RenownDelta, Tol);
            Assert.AreEqual(0.70, outcome.Odds, Tol);
        }

        [Test]
        public void Resolve_OddsEchoedInOutcome()
        {
            var outcome = ContestResolver.Resolve(0.30, _config, new FakeRandom(0.5));
            Assert.AreEqual(0.30, outcome.Odds, Tol);
        }

        [Test]
        public void Resolve_LongerOddsWin_LargerStakeThanShortOddsWin()
        {
            // odds 0.10 (long-shot): stake = 0.10 * 0.90 = 0.09; roll 0.0 → won
            var longShot = ContestResolver.Resolve(0.10, _config, new FakeRandom(0.0));
            // odds 0.90 (near-sure): stake = 0.10 * 0.10 = 0.01; roll 0.0 → won
            var nearSure = ContestResolver.Resolve(0.90, _config, new FakeRandom(0.0));

            Assert.IsTrue(longShot.Won);
            Assert.IsTrue(nearSure.Won);
            Assert.AreEqual(0.09, longShot.RenownDelta, Tol);
            Assert.AreEqual(0.01, nearSure.RenownDelta, Tol);
            Assert.Greater(longShot.RenownDelta, nearSure.RenownDelta);
        }
    }
}
