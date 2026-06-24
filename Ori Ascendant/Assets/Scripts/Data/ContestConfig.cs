using System;
using OriAscendant.Core;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One Àṣẹ milestone entry in a ContestConfig. When the player's accumulated Àṣẹ
    /// reaches this value, a rival House surfaces (issue #38).
    /// </summary>
    [Serializable]
    public class ContestMilestone
    {
        [Tooltip("Àṣẹ threshold (mantissa × 10^exponent) that surfaces a rival House.")]
        public double mantissa = 1.0;
        public int exponent = 3;

        public BigNumber Value => new BigNumber(mantissa, exponent);
    }

    /// <summary>
    /// Ọjà (the Marketplace) contest tuning — issue #37. ScriptableObject = config only.
    /// EVERY value here is a PLACEHOLDER pending the balance pass (issue #40); the contest
    /// SHAPE is locked, the numbers are not.
    /// </summary>
    [CreateAssetMenu(fileName = "ContestConfig", menuName = "Ori Ascendant/Contest Config")]
    public class ContestConfig : ScriptableObject
    {
        [Header("Odds shaping (placeholders — balance pass #40)")]
        [Tooltip("Odds shift when you win (or lose) the stance triangle.")]
        public double stanceTilt = 0.20;

        [Tooltip("Odds shift per unit of power advantage (1 - houseRatio).")]
        public double powerWeight = 0.30;

        [Tooltip("Lowest disclosed win chance — even a hopeless clash is never 0 (honest design).")]
        public double oddsMin = 0.10;

        [Tooltip("Highest disclosed win chance — even a sure thing can still fail.")]
        public double oddsMax = 0.90;

        [Header("Renown stake (risk-reward; placeholders — #40)")]
        [Tooltip("Base renown stake before odds scaling.")]
        public double stakeBase = 0.10;

        [Tooltip("Losses are softened by this factor relative to a win of the same stake (<1 = softer).")]
        public double lossSoftness = 0.5;

        [Header("House generation (placeholders — #40)")]
        [Tooltip("Min house power as a ratio of the player's current asePerSecond.")]
        public double housePowerMin = 0.75;

        [Tooltip("Max house power as a ratio of the player's current asePerSecond.")]
        public double housePowerMax = 1.25;

        [Header("Contest screen timing (issue #43)")]
        [Tooltip("Seconds the hold-to-confirm button must be held before committing a stance.")]
        public double holdToConfirmSeconds = 0.8;

        [Tooltip("Seconds the reveal beat is shown before advancing to the summary.")]
        public double revealSeconds = 2.0;

        [Header("Contest cadence — Àṣẹ milestones (placeholders, #40)")]
        [Tooltip("First Àṣẹ milestone (mantissa × 10^exponent). When first crossed this life, a contest surfaces.")]
        public double milestoneMantissa = 1.0;
        public int milestoneExponent = 3; // 1 000 Àṣẹ

        [Tooltip("Additional milestones beyond the first; each surfaces another contest when crossed.")]
        public ContestMilestone[] extraMilestones = new ContestMilestone[0];

        public BigNumber GetMilestone() => new BigNumber(milestoneMantissa, milestoneExponent);

        /// <summary>How many contest milestones are at or below the given Àṣẹ amount.</summary>
        public int CountMilestonesCrossed(BigNumber ase)
        {
            int count = ase >= GetMilestone() ? 1 : 0;
            if (extraMilestones != null)
                foreach (var m in extraMilestones)
                    if (ase >= m.Value) count++;
            return count;
        }
    }
}
