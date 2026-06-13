using System;
using OriAscendant.Core;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure threshold logic for stage advancement and Tribulation eligibility
    /// (GAMEPLAY §2.2, §4.3). Thresholds are CUMULATIVE Àṣẹ — never spent — so
    /// banked overnight Àṣẹ can fund several advances in a row (multi-advance),
    /// and banked Àṣẹ is never zeroed by advancing (offline forgiveness).
    /// Plain C# core; CultivationSystem is the MonoBehaviour host.
    /// </summary>
    public sealed class StageProgression
    {
        private readonly BigNumber[] _advanceThresholds;
        private readonly BigNumber _tribulationThreshold;

        public int StageCount { get; }
        public int FinalStageIndex => StageCount - 1;

        /// <param name="advanceThresholds">Cumulative Àṣẹ to advance OUT of each
        /// stage, indexed by stage. The final stage's entry is unused — the
        /// Tribulation threshold gates it instead.</param>
        public StageProgression(BigNumber[] advanceThresholds, BigNumber tribulationThreshold)
        {
            if (advanceThresholds == null || advanceThresholds.Length < 2)
                throw new ArgumentException("at least 2 stages required", nameof(advanceThresholds));

            _advanceThresholds = advanceThresholds;
            _tribulationThreshold = tribulationThreshold;
            StageCount = advanceThresholds.Length;
        }

        public bool CanAdvance(int stage, BigNumber ase) =>
            stage >= 0 && stage < FinalStageIndex && ase >= _advanceThresholds[stage];

        /// <summary>The cumulative target the progress bar fills toward at this stage
        /// (the Tribulation threshold on the final stage).</summary>
        public BigNumber TargetFor(int stage) =>
            stage >= FinalStageIndex ? _tribulationThreshold : _advanceThresholds[stage];

        public bool IsTribulationEligible(int stage, BigNumber ase) =>
            stage == FinalStageIndex && ase >= _tribulationThreshold;
    }
}
