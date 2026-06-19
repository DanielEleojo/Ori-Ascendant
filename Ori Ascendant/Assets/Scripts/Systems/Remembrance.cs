using System.Collections.Generic;
using OriAscendant.Data;
using OriAscendant.Save;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure derivation of how a cultivator is remembered (Dynasty PRD slice 4a).
    /// Ascend → "{honorific} {personalName}" (a Title).
    /// Fall  → fallenEpithet of the Defining Deed (first strayed crossroads of the life),
    ///         or faithfulFallLine when the life held true to its Ori throughout.
    /// Never reads the deity-Path (ADR-0004 orthogonality — structural: no path parameter).
    /// </summary>
    public static class Remembrance
    {
        /// <summary>
        /// Derives the remembrance string for a completed cultivator.
        /// Called by TribulationSystem.Resolve() BEFORE the generation reset so that
        /// the deeds list is still intact.
        /// </summary>
        /// <param name="didAscend">True if the Crossing roll resulted in ascent.</param>
        /// <param name="honorific">The peak-stage name captured before the reset.</param>
        /// <param name="deeds">Life's Crossroads decisions, in encounter order.</param>
        /// <param name="deck">Config supplying each beat's fallenEpithet.</param>
        /// <param name="config">Name pool + faithful-fall line.</param>
        /// <param name="nameIndex">Pre-computed index (second random draw, clamped to pool).</param>
        public static string Derive(
            bool didAscend,
            string honorific,
            IReadOnlyList<DeedData> deeds,
            CrossroadsDeckConfig deck,
            RemembranceConfig config,
            int nameIndex)
        {
            if (didAscend)
            {
                if (config?.personalNames == null || config.personalNames.Length == 0)
                    return honorific ?? string.Empty;
                int i = System.Math.Max(0, System.Math.Min(nameIndex, config.personalNames.Length - 1));
                return $"{honorific} {config.personalNames[i]}";
            }

            // Fall: find the Defining Deed — the first strayed crossroads of this life.
            if (deeds != null && deck?.beats != null)
            {
                foreach (var deed in deeds)
                {
                    if (deed.strayed && deed.beatIndex >= 0 && deed.beatIndex < deck.beats.Length)
                    {
                        string epithet = deck.beats[deed.beatIndex].fallenEpithet;
                        if (!string.IsNullOrEmpty(epithet))
                            return epithet;
                    }
                }
            }

            // No stray found — faithful to their vow until the end.
            return config?.faithfulFallLine ?? string.Empty;
        }
    }
}
