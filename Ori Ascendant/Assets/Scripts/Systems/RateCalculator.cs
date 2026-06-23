using OriAscendant.Core;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure core of the production-rate formula (GAMEPLAY §2.1):
    ///
    ///   asePerSecond = baseRate × stageProductionMultiplier × pathOnlineMultiplier
    ///                × (1 + councilBonusModifier × (permanentAseBonus + activeCouncilSum) + renownBonus)
    ///
    /// The councilBonusModifier (Osun ×2) wraps the permanent AND active terms
    /// together — that joint wrap is what keeps council retirement Àṣẹ-neutral
    /// on every path. activeCouncilSum is the already-weighted Σ(W × bonusMultiplier).
    /// renownBonus (issue #35) is the CAPPED lineage-renown contribution. It rides the
    /// lineage factor like the council bonuses — so it IS scaled by stage/path (Sango's
    /// online ×2 amplifies it) — but it sits OUTSIDE the councilBonusModifier wrap, so
    /// Osun's ×2 never amplifies it and council retirement stays Àṣẹ-neutral.
    /// AseGenerationSystem is the only production caller (sole writer rule).
    /// </summary>
    public static class RateCalculator
    {
        public static BigNumber ComputeRate(
            double baseRate,
            double stageProductionMultiplier,
            double pathOnlineMultiplier,
            double councilBonusModifier,
            double permanentAseBonus,
            double activeCouncilSum,
            double renownBonus = 0.0)
        {
            double lineageFactor = 1.0 + councilBonusModifier * (permanentAseBonus + activeCouncilSum) + renownBonus;
            double rate = baseRate * stageProductionMultiplier * pathOnlineMultiplier * lineageFactor;
            return BigNumber.FromDouble(rate);
        }

        /// <summary>Gathered-input overload (Phase B): the explicit one-call form, so the
        /// caller assembles the seven inputs in one place (<see cref="RateInputs"/>). Delegates
        /// to the positional overload above, so both share byte-identical arithmetic.</summary>
        public static BigNumber ComputeRate(in RateInputs inputs) =>
            ComputeRate(
                inputs.BaseRate,
                inputs.StageProductionMultiplier,
                inputs.PathOnlineMultiplier,
                inputs.CouncilBonusModifier,
                inputs.PermanentAseBonus,
                inputs.ActiveCouncilSum,
                inputs.RenownBonus);
    }
}
