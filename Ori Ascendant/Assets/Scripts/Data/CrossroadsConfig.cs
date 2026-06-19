using System;
using System.Collections.Generic;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One choosable option in a crossroads dilemma. virtueIndex matches an index
    /// in OriConfig.virtues; the player's Ori's option is always in the set
    /// (Dynasty PRD Phase 1, slice 2a). Config only — never mutated at runtime.
    /// </summary>
    [Serializable]
    public class CrossroadsOption
    {
        [Tooltip("Index into OriConfig.virtues; -1 means virtue-neutral (no Ori owns this option).")]
        public int virtueIndex = -1;

        [Tooltip("Display text for this option shown on the crossroads card.")]
        [TextArea]
        public string optionText;
    }

    /// <summary>
    /// One dilemma in the seed deck. id is the persistent identity stored in
    /// SaveData (deeds + pending state). Placeholder copy pre-§7.10.
    /// </summary>
    [Serializable]
    public class CrossroadsCard
    {
        [Tooltip("Unique string id persisted in SaveData.pendingCrossroadsId and DeedData.")]
        public string id;

        [Tooltip("The dilemma presented to the player.")]
        [TextArea]
        public string prompt;

        [Tooltip("Choosable options; the player's Ori's option must always be among them.")]
        public CrossroadsOption[] options;
    }

    /// <summary>
    /// One Àṣẹ milestone entry in a CrossroadsConfig. When the player's accumulated Àṣẹ
    /// reaches this value, an additional crossroads is queued (slice 2b).
    /// </summary>
    [Serializable]
    public class CrossroadsMilestone
    {
        [Tooltip("Àṣẹ threshold (mantissa × 10^exponent) that triggers one queued crossroads.")]
        public double mantissa = 1.0;
        public int exponent = 3;

        public BigNumber Value => new BigNumber(mantissa, exponent);
    }

    /// <summary>
    /// Seed deck of crossroads dilemmas and the Àṣẹ milestones at which they fire
    /// (Dynasty PRD Phase 1, slices 2a/2b). Final deck content lands after the §7.10
    /// native-speaker review; the field shape is the contract.
    ///
    /// The first milestone is defined by milestoneMantissa/milestoneExponent (backward
    /// compat). Additional milestones are listed in extraMilestones. All milestones are
    /// evaluated together: when the player's Àṣẹ surpasses more milestones than there
    /// are already-triggered crossroads this life, the extras queue up.
    /// </summary>
    [CreateAssetMenu(fileName = "CrossroadsConfig", menuName = "Ori Ascendant/Crossroads Config")]
    public class CrossroadsConfig : ScriptableObject
    {
        [Tooltip("First Àṣẹ milestone (mantissa × 10^exponent). When first crossed this life, a crossroads fires.")]
        public double milestoneMantissa = 1.0;
        public int milestoneExponent = 3;

        [Tooltip("Additional milestones beyond the first. Each one queues another crossroads when crossed.")]
        public CrossroadsMilestone[] extraMilestones = new CrossroadsMilestone[0];

        [Tooltip("Seed deck of crossroads cards; one is drawn at random each time a milestone fires.")]
        public CrossroadsCard[] deck;

        public BigNumber GetMilestone() => new BigNumber(milestoneMantissa, milestoneExponent);

        public int DeckSize => deck != null ? deck.Length : 0;

        public CrossroadsCard GetCard(int index) =>
            deck != null && index >= 0 && index < deck.Length ? deck[index] : null;

        /// <summary>Returns all milestones (first + extra) sorted ascending.</summary>
        public List<BigNumber> GetAllMilestones()
        {
            var list = new List<BigNumber> { GetMilestone() };
            if (extraMilestones != null)
                foreach (var m in extraMilestones)
                    list.Add(m.Value);
            list.Sort();
            return list;
        }

        /// <summary>How many milestones are at or below the given Àṣẹ amount.</summary>
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
