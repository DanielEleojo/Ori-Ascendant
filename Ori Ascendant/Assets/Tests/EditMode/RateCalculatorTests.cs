using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: composition of the GAMEPLAY §2.1 rate formula, including the
    /// councilBonusModifier wrap (Osun) and the retirement-neutrality property
    /// the wrap exists to protect.
    /// </summary>
    public class RateCalculatorTests
    {
        [Test]
        public void NeutralEverything_ReturnsBaseRate()
        {
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 1.0, 0.0, 0.0);
            Assert.AreEqual(BigNumber.One, rate);
        }

        [Test]
        public void StageAndPath_MultiplyThrough()
        {
            // Stage 6 (×1250) on Sango (×2): 1 × 1250 × 2 = 2500/s.
            var rate = RateCalculator.ComputeRate(1.0, 1250.0, 2.0, 1.0, 0.0, 0.0);
            Assert.AreEqual(BigNumber.FromDouble(2500.0), rate);
        }

        [Test]
        public void OneAscendedAncestor_Gives1Point25Factor()
        {
            // GAMEPLAY §2.4 gen-2 example: stage 5 (×320), no path, one ascended
            // ancestor (W×1.0 = 0.25): 320 × 1.25 = 400/s.
            var rate = RateCalculator.ComputeRate(1.0, 320.0, 1.0, 1.0, 0.0, 0.25);
            Assert.AreEqual(BigNumber.FromDouble(400.0), rate);
        }

        [Test]
        public void FallenAncestor_Gives1Point10Factor()
        {
            // W × 0.4 = 0.10 → factor 1.10.
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 1.0, 0.0, 0.10);
            Assert.AreEqual(BigNumber.FromDouble(1.10), rate);
        }

        [Test]
        public void OsunWrap_DoublesPermanentAndActiveTogether()
        {
            // Osun (councilBonusModifier 2): permanent 0.25 + active 0.25 →
            // 1 + 2 × 0.5 = 2.0.
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 2.0, 0.25, 0.25);
            Assert.AreEqual(BigNumber.FromDouble(2.0), rate);
        }

        [TestCase(1.0)]
        [TestCase(2.0)] // Osun — the case the joint wrap exists for
        public void Retirement_IsAseNeutral_UnderAnyCouncilModifier(double councilModifier)
        {
            // Retiring moves W×bonus from the active sum into permanentAseBonus.
            // Because the modifier wraps BOTH terms, the rate must not change.
            const double w = 0.25;
            const double retiringBonus = w * 1.0; // an ascended ancestor retires

            var before = RateCalculator.ComputeRate(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: 0.0,
                activeCouncilSum: retiringBonus + 0.35);
            var after = RateCalculator.ComputeRate(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: retiringBonus,
                activeCouncilSum: 0.35);

            Assert.AreEqual(before, after,
                $"retirement changed the rate at councilModifier={councilModifier}");
        }

        [Test]
        public void FullCouncil_FiveAscended_Gives2Point25()
        {
            // 5 × (0.25 × 1.0) = 1.25 → factor 2.25 (GAMEPLAY long-run table).
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 1.0, 0.0, 1.25);
            Assert.AreEqual(BigNumber.FromDouble(2.25), rate);
        }

        // ---- Phase B: the gathered-input (RateInputs) overload ----

        [Test]
        public void RateInputs_Overload_MatchesPositional()
        {
            // The gathered-input overload must be a faithful delegate of the positional form,
            // so funnelling RecalculateRate through RateInputs cannot drift the economy.
            var inputs = new RateInputs(1.0, 320.0, 2.0, 2.0, 0.25, 0.35);
            var viaStruct = RateCalculator.ComputeRate(in inputs);
            var viaPositional = RateCalculator.ComputeRate(1.0, 320.0, 2.0, 2.0, 0.25, 0.35);
            Assert.AreEqual(viaPositional, viaStruct);
        }

        [TestCase(1.0)]
        [TestCase(2.0)] // Osun — the case the joint wrap exists for
        public void RateInputs_Retirement_IsAseNeutral_UnderAnyCouncilModifier(double councilModifier)
        {
            // The same neutrality property, now pinned at the RateInputs boundary: retiring
            // moves W×bonus from the active sum into permanentAseBonus, and the joint wrap
            // must leave the rate unchanged.
            const double retiringBonus = 0.25 * 1.0;

            var before = new RateInputs(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: 0.0, activeCouncilSum: retiringBonus + 0.35);
            var after = new RateInputs(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: retiringBonus, activeCouncilSum: 0.35);

            Assert.AreEqual(RateCalculator.ComputeRate(in before), RateCalculator.ComputeRate(in after),
                $"retirement changed the rate at councilModifier={councilModifier}");
        }

        // ---- Renown: the 7th additive term, outside the council wrap (issue #35) ----

        [Test]
        public void RenownBonus_AddsToRate()
        {
            // renownBonus 0.25 with everything else neutral → factor 1.25.
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 1.0, 0.0, 0.0, 0.25);
            Assert.AreEqual(BigNumber.FromDouble(1.25), rate);
        }

        [Test]
        public void RenownBonus_SitsOutsideCouncilWrap()
        {
            // Under Osun (councilModifier 2) renown is NOT doubled: 1 + 2×0 + 0.5 = 1.5,
            // not 2.0. This is what keeps renown path-agnostic.
            var rate = RateCalculator.ComputeRate(1.0, 1.0, 1.0, 2.0, 0.0, 0.0, 0.5);
            Assert.AreEqual(BigNumber.FromDouble(1.5), rate);
        }

        [TestCase(1.0)]
        [TestCase(2.0)] // Osun — the case the joint wrap exists for
        public void Retirement_IsAseNeutral_WithRenownPresent(double councilModifier)
        {
            // Retirement neutrality must still hold once a renown term is present.
            const double retiringBonus = 0.25 * 1.0;
            const double renownBonus = 0.3;

            var before = RateCalculator.ComputeRate(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: 0.0, activeCouncilSum: retiringBonus + 0.35, renownBonus: renownBonus);
            var after = RateCalculator.ComputeRate(1.0, 80.0, 1.0, councilModifier,
                permanentAseBonus: retiringBonus, activeCouncilSum: 0.35, renownBonus: renownBonus);

            Assert.AreEqual(before, after,
                $"renown present must not break retirement neutrality at councilModifier={councilModifier}");
        }

        [Test]
        public void RenownBonus_FlowsThrough_RateInputs()
        {
            var inputs = new RateInputs(1.0, 1.0, 1.0, 2.0, 0.0, 0.0, renownBonus: 0.5);
            Assert.AreEqual(
                RateCalculator.ComputeRate(1.0, 1.0, 1.0, 2.0, 0.0, 0.0, 0.5),
                RateCalculator.ComputeRate(in inputs));
        }
    }
}
