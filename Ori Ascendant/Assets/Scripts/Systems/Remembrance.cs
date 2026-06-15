using System.Collections.Generic;
using OriAscendant.Data;
using OriAscendant.Save;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure derivation of how a finished Cultivator is remembered (CONTEXT.md:
    /// Title / Nickname / Defining Deed) — the single source of truth for the
    /// <see cref="AncestorData.remembrance"/> string written at the Crossing.
    /// Ascend → "{honorific} {personalName}". Fall → the epithet of the Defining
    /// Deed (the FIRST strayed choice); a faithful fall (no stray) → the shared
    /// faithful-fall line. NEVER reads the deity-Path — path-independence is
    /// structural (it is not an input), per ADR-0004 orthogonality.
    /// </summary>
    public static class Remembrance
    {
        public static string Derive(bool didAscend, string honorific,
            IReadOnlyList<DeedData> deeds, CrossroadsDeckConfig deck,
            RemembranceConfig config, int nameIndex)
        {
            return didAscend
                ? Title(honorific, config, nameIndex)
                : Nickname(deeds, deck, config);
        }

        /// <summary>"{honorific} {personalName}" — the peak-stage name borne as an
        /// honorific plus a pooled personal name. The index is clamped to the pool, so
        /// any draw is safe.</summary>
        private static string Title(string honorific, RemembranceConfig config, int nameIndex)
        {
            string[] pool = config != null ? config.personalNames : null;
            if (pool == null || pool.Length == 0) return honorific;
            int i = nameIndex < 0 ? 0 : (nameIndex >= pool.Length ? pool.Length - 1 : nameIndex);
            string name = pool[i];
            return string.IsNullOrEmpty(honorific) ? name : $"{honorific} {name}";
        }

        /// <summary>The epithet of the Defining Deed — the FIRST strayed choice. A life
        /// that never strayed (or one whose stray cannot be named) shares the single
        /// dignified faithful-fall line.</summary>
        private static string Nickname(IReadOnlyList<DeedData> deeds,
            CrossroadsDeckConfig deck, RemembranceConfig config)
        {
            string faithfulFall = config != null ? config.faithfulFallLine : null;
            if (deeds == null) return faithfulFall;

            for (int i = 0; i < deeds.Count; i++)
            {
                if (deeds[i].aligned) continue; // held the vow — keep looking for the first stray

                // The first stray IS the Defining Deed; its beat's epithet is the Nickname.
                if (deck != null && deck.beats != null)
                {
                    int idx = deeds[i].crossroadsIndex;
                    if (idx >= 0 && idx < deck.beats.Length)
                    {
                        string epithet = deck.beats[idx].fallenEpithet;
                        if (!string.IsNullOrEmpty(epithet)) return epithet;
                    }
                }
                return faithfulFall; // strayed but un-nameable → the dignified line, never a later stray
            }

            return faithfulFall; // never strayed yet still fell → the shared faithful-fall line
        }
    }
}
