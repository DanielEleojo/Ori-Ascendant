using System;
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
    /// Seed deck of crossroads dilemmas and the Àṣẹ milestone at which one fires
    /// (Dynasty PRD Phase 1, slice 2a). Final deck content lands after the §7.10
    /// native-speaker review; the field shape is the contract.
    /// </summary>
    [CreateAssetMenu(fileName = "CrossroadsConfig", menuName = "Ori Ascendant/Crossroads Config")]
    public class CrossroadsConfig : ScriptableObject
    {
        [Tooltip("Àṣẹ milestone: when accumulated Àṣẹ first reaches this amount this life, a crossroads fires.")]
        public double milestoneMantissa = 1.0;
        public int milestoneExponent = 3;

        [Tooltip("Seed deck of crossroads cards; drawn at random when the milestone fires.")]
        public CrossroadsCard[] deck;

        public BigNumber GetMilestone() => new BigNumber(milestoneMantissa, milestoneExponent);

        public int DeckSize => deck != null ? deck.Length : 0;

        public CrossroadsCard GetCard(int index) =>
            deck != null && index >= 0 && index < deck.Length ? deck[index] : null;
    }
}
