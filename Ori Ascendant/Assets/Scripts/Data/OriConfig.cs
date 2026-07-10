using System;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One vow-virtue option in the Ori seed set (PRD Phase 1, slice 1).
    /// Copy fully authored — awaiting only the §7.10 native-speaker review.
    /// Config only; never mutated at runtime.
    /// </summary>
    [Serializable]
    public class OriVirtue
    {
        [Tooltip("Display name shown on the Ori card and the main-screen badge.")]
        public string virtueName;

        [Tooltip("Short vow line shown on the selection card; authored, pending §7.10 review.")]
        [TextArea]
        public string vowLine;
    }

    /// <summary>
    /// The seed set of Ori virtues a player can vow themselves to (Àkùnlẹ̀yàn,
    /// chosen at birth) — surfaced once per generation. Copy authored; awaiting the
    /// §7.10 native-speaker review. Crossroads-tied content may extend this set,
    /// but the field shape stays the same.
    /// </summary>
    [CreateAssetMenu(fileName = "OriConfig", menuName = "Ori Ascendant/Ori Config")]
    public class OriConfig : ScriptableObject
    {
        [Tooltip("Ordered virtue list — index is what persists in SaveData.chosenOri.")]
        public OriVirtue[] virtues;

        public int Count => virtues != null ? virtues.Length : 0;

        public OriVirtue GetVirtue(int index) =>
            virtues != null && index >= 0 && index < virtues.Length ? virtues[index] : null;
    }
}
