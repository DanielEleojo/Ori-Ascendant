using System;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>One option at a Crossroads — a choice tagged with the virtue (Ori
    /// index) it expresses. There is no "right" option; only whether it holds to the
    /// life's vowed Ori.</summary>
    [Serializable]
    public class CrossroadsOption
    {
        [Tooltip("Index into the Ori set this option expresses (0-based).")]
        public int oriIndex;

        [TextArea]
        public string text;
    }

    /// <summary>One Crossroads dilemma — a prompt and its virtue-tagged options. The
    /// seed deck gives every beat one option per Ori so the player's vow is always on
    /// the table (temptation, not a trap).</summary>
    [Serializable]
    public class CrossroadsBeat
    {
        public string id;

        [TextArea]
        public string prompt;

        public CrossroadsOption[] options;
    }

    /// <summary>
    /// The authored Crossroads deck (DYNASTY_REDESIGN, ADR-0003). Beats are drawn
    /// along the climb. Seed content is PLACEHOLDER, pre-§7.10 review (slice #10).
    /// </summary>
    [CreateAssetMenu(fileName = "CrossroadsDeck", menuName = "Ori Ascendant/Crossroads Deck")]
    public class CrossroadsDeckConfig : ScriptableObject
    {
        public CrossroadsBeat[] beats;
    }
}
