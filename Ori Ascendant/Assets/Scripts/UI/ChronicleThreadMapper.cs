using System;
using System.Collections.Generic;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Visual state for a single chronicle node in the unbroken thread (issue #27).
    /// </summary>
    public struct ChronicleNodeState
    {
        /// <summary>AseGold at full brightness (ascended) or EmberWarm dimmed (fallen).</summary>
        public Color NodeColor;
        /// <summary>"Gen N  —  Ascended" or "Gen N  —  Fell".</summary>
        public string Label;
        /// <summary>Title (ascended) or Nickname (fallen); never null.</summary>
        public string Remembrance;
    }

    /// <summary>
    /// Pure mapping from chronicle data to node visual state (issue #27).
    /// No MonoBehaviour — testable in EditMode without a scene.
    ///
    /// Two node states:
    ///   Ascended → AseGold, full brightness   (bright node)
    ///   Fallen   → EmberWarm, dimmed           (ember node — present, honoured, softer)
    ///
    /// The connecting thread is always drawn at ThreadLineColor; it is never skipped,
    /// so the visual line is unbroken across every generation.
    /// </summary>
    public static class ChronicleThreadMapper
    {
        private const float AscendedAlpha = 1.00f;
        private const float FallenAlpha   = 0.55f;

        /// <summary>
        /// Colour for the continuous vertical thread that connects all nodes.
        /// Always rendered — even between fallen generations — so the line never breaks.
        /// </summary>
        public static readonly Color ThreadLineColor = Palette.AseGold.WithAlpha(0.25f);

        /// <summary>Maps a single chronicle entry to its node visual state.</summary>
        public static ChronicleNodeState Map(ChronicleEntry entry)
        {
            Color nodeColor = entry.didAscend
                ? Palette.AseGold.WithAlpha(AscendedAlpha)
                : Palette.EmberWarm.WithAlpha(FallenAlpha);

            string outcome = entry.didAscend ? "Ascended" : "Fell";
            return new ChronicleNodeState
            {
                NodeColor   = nodeColor,
                Label       = $"Gen {entry.generationNumber}  —  {outcome}",
                Remembrance = entry.remembrance ?? "—",
            };
        }

        /// <summary>
        /// Maps every entry in <paramref name="chronicle"/> to a node, preserving order.
        /// Returns one node per entry — the thread is never broken.
        /// </summary>
        public static ChronicleNodeState[] MapAll(List<ChronicleEntry> chronicle)
        {
            if (chronicle == null) return Array.Empty<ChronicleNodeState>();
            var result = new ChronicleNodeState[chronicle.Count];
            for (int i = 0; i < chronicle.Count; i++)
                result[i] = Map(chronicle[i]);
            return result;
        }
    }
}
