using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate B: the pure threshold core, table-driven from GAMEPLAY §2.2.
    /// Thresholds are cumulative — Àṣẹ is never spent.
    /// </summary>
    public class StageProgressionTests
    {
        private static StageProgression MakeTable() => new StageProgression(
            new[]
            {
                BigNumber.FromDouble(100),
                BigNumber.FromDouble(1500),
                BigNumber.FromDouble(5500),
                BigNumber.FromDouble(100000),
                BigNumber.FromDouble(750000),
                BigNumber.Zero, // final stage — tribulation-gated
            },
            new BigNumber(25.0, 6)); // 25,000,000

        [TestCase(0, 99, false)]
        [TestCase(0, 100, true)]
        [TestCase(1, 1499, false)]
        [TestCase(1, 1500, true)]
        [TestCase(2, 5499, false)]
        [TestCase(2, 5500, true)]
        [TestCase(3, 99999, false)]
        [TestCase(3, 100000, true)]
        [TestCase(4, 749000, false)]
        [TestCase(4, 750000, true)]
        public void CanAdvance_FollowsTheLockedTable(int stage, double ase, bool expected)
        {
            Assert.AreEqual(expected, MakeTable().CanAdvance(stage, BigNumber.FromDouble(ase)));
        }

        [Test]
        public void FinalStage_NeverAdvances_TribulationGatesIt()
        {
            var table = MakeTable();
            Assert.IsFalse(table.CanAdvance(5, new BigNumber(999.0, 9)));
        }

        [Test]
        public void MultiAdvance_BankedAseFundsSeveralStages()
        {
            // An overnight bank of 10,000 Àṣẹ clears thresholds 100/1500/5500 in a
            // row but not 100,000 — exactly three affordable advances.
            var table = MakeTable();
            var banked = BigNumber.FromDouble(10000);

            int stage = 0;
            int advances = 0;
            while (table.CanAdvance(stage, banked))
            {
                stage++;
                advances++;
            }

            Assert.AreEqual(3, advances);
            Assert.AreEqual(3, stage);
        }

        [TestCase(24_999_000, false)]
        [TestCase(25_000_000, true)]
        [TestCase(54_000_000, true)] // full 8h overnight bank at stage-6 rate
        public void TribulationEligibility_GatesAt25M(double ase, bool expected)
        {
            Assert.AreEqual(expected, MakeTable().IsTribulationEligible(5, BigNumber.FromDouble(ase)));
        }

        [Test]
        public void TribulationEligibility_RequiresFinalStage()
        {
            // Plenty of Àṣẹ but mid-climb — never eligible (one capstone per generation).
            Assert.IsFalse(MakeTable().IsTribulationEligible(4, new BigNumber(25.0, 6)));
        }

        [Test]
        public void TargetFor_ReportsAdvanceThresholds_AndTribulationAtPeak()
        {
            var table = MakeTable();
            Assert.AreEqual(BigNumber.FromDouble(100), table.TargetFor(0));
            Assert.AreEqual(BigNumber.FromDouble(750000), table.TargetFor(4));
            Assert.AreEqual(new BigNumber(25.0, 6), table.TargetFor(5), "final stage fills toward the tribulation gate");
        }
    }
}
