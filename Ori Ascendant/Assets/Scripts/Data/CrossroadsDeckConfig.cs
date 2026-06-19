using System;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One Crossroads beat in the deck — a single decision point in a life.
    /// fallenEpithet is the Nickname a cultivator earns when this beat is their
    /// Defining Deed (the first strayed choice of that life). Pre-§7.10 placeholder;
    /// final copy lands via the native-speaker review (Phase 5, issue #10).
    /// </summary>
    [Serializable]
    public class CrossroadsBeat
    {
        [Tooltip("The Nickname line awarded when this beat is the Defining Deed (first stray).")]
        [TextArea]
        public string fallenEpithet;
    }

    /// <summary>
    /// The ordered deck of Crossroads beats that can appear during a life
    /// (Dynasty PRD Phase 2). TribulationSystem reads beats[DeedData.beatIndex]
    /// to resolve the Defining Deed epithet at the Crossing (slice 4a).
    /// Config only — never written at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "CrossroadsDeckConfig", menuName = "Ori Ascendant/Crossroads Deck Config")]
    public class CrossroadsDeckConfig : ScriptableObject
    {
        [Tooltip("Ordered beat list — DeedData.beatIndex is the index into this array.")]
        public CrossroadsBeat[] beats;

        public int Count => beats != null ? beats.Length : 0;

        public CrossroadsBeat GetBeat(int index) =>
            beats != null && index >= 0 && index < beats.Length ? beats[index] : null;
    }
}
