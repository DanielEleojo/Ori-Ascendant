using OriAscendant.Core;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure core of the production-rate formula (GAMEPLAY §2.1):
    ///
    ///   asePerSecond = baseRate
    ///                × stageProductionMultiplier
    ///                × pathOnlineMultiplier            (1.0 until a path is chosen)
    ///                × (1 + councilBonusModifier × (permanentAseBonus + activeCouncilSum))
    ///
    /// The councilBonusModifier (Osun ×2) wraps the permanent AND active terms
    /// together — that joint wrap is what keeps council retirement Àṣẹ-neutral
    /// on every path. activeCouncilSum is the already-weighted Σ(W × bonusMultiplier).
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
            double activeCouncilSum)
        {
            double lineageFactor = 1.0 + councilBonusModifier * (permanentAseBonus + activeCouncilSum);
            double rate = baseRate * stageProductionMultiplier * pathOnlineMultiplier * lineageFactor;
            return BigNumber.FromDouble(rate);
        }
    }
}
