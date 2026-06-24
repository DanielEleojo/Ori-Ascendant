using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #39: pure presenter mapping lineage renown → marketplace standing.
    /// MarketplaceStandingPresenter is stateless (no MonoBehaviour, no scene), so all
    /// paths are coverable in EditMode without any host setup.
    /// </summary>
    public class MarketplaceStandingPresenterTests
    {
        // ---- zero renown → last place ----

        [Test]
        public void ZeroRenown_Rank_IsLastPlace()
        {
            var standing = MarketplaceStandingPresenter.Map(0.0);
            Assert.AreEqual(100, standing.Rank, "zero renown must place the house at the bottom (rank 100)");
        }

        [Test]
        public void ZeroRenown_Line_ContainsRenownValue()
        {
            var standing = MarketplaceStandingPresenter.Map(0.0);
            StringAssert.Contains("Renown 0.00", standing.Line);
        }

        [Test]
        public void ZeroRenown_Line_Contains100th()
        {
            var standing = MarketplaceStandingPresenter.Map(0.0);
            StringAssert.Contains("100th", standing.Line);
        }

        [Test]
        public void ZeroRenown_Line_ContainsInTheMarketplace()
        {
            var standing = MarketplaceStandingPresenter.Map(0.0);
            StringAssert.Contains("in the marketplace", standing.Line);
        }

        // ---- monotonic climb ----

        [Test]
        public void MoreRenown_ProducesLowerRank()
        {
            int rankLow  = MarketplaceStandingPresenter.Map(0.05).Rank;
            int rankHigh = MarketplaceStandingPresenter.Map(0.30).Rank;
            Assert.Less(rankHigh, rankLow,
                "higher renown must produce a smaller (better) rank number");
        }

        [Test]
        public void Renown005_Rank_Is95()
        {
            Assert.AreEqual(95, MarketplaceStandingPresenter.Map(0.05).Rank);
        }

        [Test]
        public void Renown030_Rank_Is70()
        {
            Assert.AreEqual(70, MarketplaceStandingPresenter.Map(0.30).Rank);
        }

        // ---- rank floored at 1 ----

        [Test]
        public void VeryHighRenown_Rank_IsOne()
        {
            Assert.AreEqual(1, MarketplaceStandingPresenter.Map(5.0).Rank,
                "rank must never fall below 1 regardless of how much renown has been earned");
        }

        // ---- line format ----

        [Test]
        public void Renown030_Line_ContainsRenownValue()
        {
            var standing = MarketplaceStandingPresenter.Map(0.30);
            StringAssert.Contains("Renown 0.30", standing.Line);
        }

        [Test]
        public void Renown030_Line_Contains70th()
        {
            var standing = MarketplaceStandingPresenter.Map(0.30);
            StringAssert.Contains("70th", standing.Line);
        }

        // ---- determinism / no stored state ----

        [Test]
        public void SameRenown_CalledTwice_ProducesEqualRank()
        {
            int rank1 = MarketplaceStandingPresenter.Map(0.42).Rank;
            int rank2 = MarketplaceStandingPresenter.Map(0.42).Rank;
            Assert.AreEqual(rank1, rank2, "presenter must be deterministic — no stored roster");
        }

        [Test]
        public void SameRenown_CalledTwice_ProducesEqualLine()
        {
            string line1 = MarketplaceStandingPresenter.Map(0.42).Line;
            string line2 = MarketplaceStandingPresenter.Map(0.42).Line;
            Assert.AreEqual(line1, line2, "presenter must be deterministic — no stored roster");
        }

        // ---- rank 1 at 0.99 renown ----

        [Test]
        public void Renown099_Rank_IsOne()
        {
            Assert.AreEqual(1, MarketplaceStandingPresenter.Map(0.99).Rank,
                "0.99 renown climbs 99 ranks: 100 - 99 = 1");
        }

        [Test]
        public void Renown099_Line_Contains1st()
        {
            StringAssert.Contains("1st", MarketplaceStandingPresenter.Map(0.99).Line);
        }

        // ---- progress bar fraction ----

        [Test]
        public void ProgressFraction_AtZeroRenown_IsZero()
        {
            double renown = 0.0;
            const double RenownPerRank = 0.01;
            float fraction = (float)(renown % RenownPerRank / RenownPerRank);
            Assert.AreEqual(0f, fraction, 1e-6f);
        }

        [Test]
        public void ProgressFraction_AtHalfwayRenown_IsHalf()
        {
            double renown = 0.005;
            const double RenownPerRank = 0.01;
            float fraction = (float)(renown % RenownPerRank / RenownPerRank);
            Assert.AreEqual(0.5f, fraction, 1e-5f);
        }

        [Test]
        public void ProgressFraction_AtExactRankBoundary_IsZero()
        {
            double renown = 0.01;
            const double RenownPerRank = 0.01;
            float fraction = (float)(renown % RenownPerRank / RenownPerRank);
            Assert.AreEqual(0f, fraction, 1e-6f,
                "at an exact rank boundary the bar rolls over to 0 for the next rank");
        }

        // ---- ordinal suffixes ----

        [TestCase(1,   "1st")]
        [TestCase(2,   "2nd")]
        [TestCase(3,   "3rd")]
        [TestCase(4,   "4th")]
        [TestCase(11,  "11th")]
        [TestCase(12,  "12th")]
        [TestCase(13,  "13th")]
        [TestCase(21,  "21st")]
        [TestCase(22,  "22nd")]
        [TestCase(23,  "23rd")]
        [TestCase(100, "100th")]
        public void Ordinal_ReturnsCorrectSuffix(int n, string expected)
        {
            Assert.AreEqual(expected, MarketplaceStandingPresenter.Ordinal(n));
        }
    }
}
