using System.Collections.Generic;
using OriAscendant.Data;
using OriAscendant.Save;

namespace OriAscendant.Systems
{
    /// <summary>
    /// The per-life Chronicle engine (CONTEXT.md: Chronicle / Deed / Title / Nickname).
    /// The single owner of a life's recorded Crossroads choices:
    ///   • RECORD — turns a chosen option into a <see cref="DeedData"/>; the ONE place
    ///     the alignment rule (option.oriIndex == currentOri) lives.
    ///   • STEADFASTNESS — a read-model OVER the recorded Deeds (held = aligned count,
    ///     trials = deed count); the Crossing maps the rate through its floor→ceiling
    ///     curve (ADR-0004).
    ///   • REMEMBER — how the finished life is named (Title on ascend, the Defining
    ///     Deed's Nickname on fall); absorbs the former Remembrance helper and owns the
    ///     ascended personal-name roll.
    ///   • RESET — clears the per-life ledger each generation.
    ///
    /// Pure C# (no MonoBehaviour) — the interface IS the test surface. Deeds are the
    /// source of truth; <see cref="SaveData.oriHeld"/>/<see cref="SaveData.oriTrials"/>
    /// are kept as a written cache (add-only SaveData) but no logic reads them. The
    /// deity-Path is never an input — path-independence is structural (ADR-0004).
    /// </summary>
    public static class Chronicle
    {
        // ──────────────────────────── WRITE ────────────────────────────

        /// <summary>Records the chosen option as a Deed and advances the steadfastness
        /// cache in lockstep. The single home of the alignment rule. Caller validates
        /// the beat/option are in range.</summary>
        public static void RecordChoice(SaveData save, CrossroadsBeat beat, int beatIndex, int optionIndex)
        {
            if (save == null || beat?.options == null ||
                optionIndex < 0 || optionIndex >= beat.options.Length) return;

            CrossroadsOption option = beat.options[optionIndex];
            bool aligned = option.oriIndex == save.currentOri;

            save.deeds.Add(new DeedData
            {
                crossroadsIndex = beatIndex,
                chosenOri = option.oriIndex,
                stage = save.currentStage,
                aligned = aligned,
            });

            // Written cache kept in lockstep with the Deeds (add-only SaveData fields).
            save.oriTrials++;
            if (aligned) save.oriHeld++;
        }

        // ──────────────────────── STEADFASTNESS ────────────────────────
        // A read-model over the recorded Deeds — the Deeds are the source of truth.

        /// <summary>Resolved Crossroads this life — "the M of N".</summary>
        public static int Trials(SaveData save) => save?.deeds?.Count ?? 0;

        /// <summary>Crossroads whose chosen option held to the vowed Ori — "the N of M".</summary>
        public static int Held(SaveData save)
        {
            if (save?.deeds == null) return 0;
            int held = 0;
            for (int i = 0; i < save.deeds.Count; i++)
                if (save.deeds[i].aligned) held++;
            return held;
        }

        /// <summary>held/trials in [0,1]; 0 when no Crossroads were resolved (a life that
        /// faced none earns no steadfastness credit → the Crossing's floor).</summary>
        public static double SteadfastnessRate(SaveData save)
        {
            int trials = Trials(save);
            return trials <= 0 ? 0.0 : (double)Held(save) / trials;
        }

        // ───────────────────────────  REMEMBER  ───────────────────────────

        /// <summary>How this finished life is remembered. Ascend → Title (peak-stage
        /// honorific + a pooled personal name, drawn HERE from the injected randomness);
        /// fall → the Defining Deed's Nickname (a fall draws no name). Must be called
        /// before <see cref="ResetForNewGeneration"/> clears the Deeds.</summary>
        public static string Remember(SaveData save, bool didAscend, string honorific,
            CrossroadsDeckConfig deck, RemembranceConfig config, IRandomSource random)
        {
            int nameIndex = 0;
            if (didAscend)
            {
                int poolCount = config != null && config.personalNames != null
                    ? config.personalNames.Length : 0;
                double nameRoll = random != null ? random.NextDouble() : 0.0;
                nameIndex = poolCount > 0 ? (int)(nameRoll * poolCount) : 0;
            }
            return DeriveRemembrance(didAscend, honorific, save?.deeds, deck, config, nameIndex);
        }

        /// <summary>Pure derivation (no randomness) — the directly testable core.
        /// Ascend → "{honorific} {personalName}". Fall → the epithet of the Defining
        /// Deed (the FIRST strayed choice); a faithful fall (no stray) → the shared
        /// faithful-fall line. NEVER reads the deity-Path (ADR-0004 orthogonality).</summary>
        public static string DeriveRemembrance(bool didAscend, string honorific,
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

        // ───────────────────────────── RESET ─────────────────────────────

        /// <summary>Clears the per-life ledger at the Crossing — the Deeds and the
        /// steadfastness cache. Call inside Resolve's atomic write, AFTER
        /// <see cref="Remember"/> has read the Deeds.</summary>
        public static void ResetForNewGeneration(SaveData save)
        {
            if (save == null) return;
            save.deeds.Clear();
            save.oriHeld = 0;
            save.oriTrials = 0;
        }
    }
}
