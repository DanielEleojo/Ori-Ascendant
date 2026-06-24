namespace OriAscendant.Systems
{
    /// <summary>
    /// The seven inputs to the production-rate formula (GAMEPLAY §2.1), gathered into one
    /// value so the rate's dependencies — and the Àṣẹ-neutrality of council retirement —
    /// are explicit in one place instead of scattered across four systems. Assembled by
    /// <see cref="AseGenerationSystem.RecalculateRate"/> (the sole writer of the cached
    /// rate) and consumed by <see cref="RateCalculator.ComputeRate(in RateInputs)"/>.
    ///
    /// Invariant: <see cref="CouncilBonusModifier"/> (Osun ×2) wraps BOTH lineage terms
    /// (<see cref="PermanentAseBonus"/> + <see cref="ActiveCouncilSum"/>) together; that
    /// joint wrap is what keeps retirement Àṣẹ-neutral on every path. The property is pinned
    /// by RateCalculatorTests against this struct.
    ///
    /// The 7th term, <see cref="RenownBonus"/>, rides the lineage factor (so it IS scaled by
    /// stage/path, like the council bonuses) but sits OUTSIDE the councilBonusModifier wrap, so
    /// Osun's ×2 never amplifies it and retirement neutrality is undisturbed (issue #35).
    /// </summary>
    public readonly struct RateInputs
    {
        /// <summary>Global base production (GameplayConfig.baseRate).</summary>
        public readonly double BaseRate;

        /// <summary>Current cultivation stage multiplier (1.0 at stage 0).</summary>
        public readonly double StageProductionMultiplier;

        /// <summary>Chosen path's online multiplier (1.0 until a path is chosen).</summary>
        public readonly double PathOnlineMultiplier;

        /// <summary>Path's council-bonus modifier — the joint wrap over both lineage terms.</summary>
        public readonly double CouncilBonusModifier;

        /// <summary>Baked retired-ancestor bonus (SaveData.lineage.permanentAseBonus).</summary>
        public readonly double PermanentAseBonus;

        /// <summary>Active council contribution, already weighted: Σ(W × bonusMultiplier).</summary>
        public readonly double ActiveCouncilSum;

        /// <summary>Renown's CAPPED contribution to the rate — the 7th additive term, inside
        /// the lineage factor but OUTSIDE the councilBonusModifier wrap (Osun-neutral) (issue #35).</summary>
        public readonly double RenownBonus;

        public RateInputs(
            double baseRate,
            double stageProductionMultiplier,
            double pathOnlineMultiplier,
            double councilBonusModifier,
            double permanentAseBonus,
            double activeCouncilSum,
            double renownBonus = 0.0)
        {
            BaseRate = baseRate;
            StageProductionMultiplier = stageProductionMultiplier;
            PathOnlineMultiplier = pathOnlineMultiplier;
            CouncilBonusModifier = councilBonusModifier;
            PermanentAseBonus = permanentAseBonus;
            ActiveCouncilSum = activeCouncilSum;
            RenownBonus = renownBonus;
        }
    }
}
