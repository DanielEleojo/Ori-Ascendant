using System;
using System.Collections.Generic;
using OriAscendant.Core;

namespace OriAscendant.Save
{
    /// <summary>
    /// The single serializable save structure — the source of truth for all
    /// player state (TECH_DESIGN §5, GAMEPLAY §6; schema locked 2026-06-12).
    ///
    /// OFF-LIMITS RULES (CLAUDE.md): never rename a field, change a type, or
    /// remove a field without bumping <see cref="schemaVersion"/> and adding a
    /// migration. Add-only changes are safe (Newtonsoft fills missing fields
    /// with the defaults below and ignores unknown JSON members).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Migration anchor — bump on ANY schema change.</summary>
        public int schemaVersion = 1;

        // Àṣẹ amounts persist as BigNumber split fields (mantissa × 10^exponent).
        // A brand-new save holds zero Àṣẹ: BigNumber.Zero is canonically (0, 0).
        public double aseMantissa = 0.0;
        public int aseExponent = 0;

        // Cached derived production rate. AseGenerationSystem.RecalculateRate()
        // is the SOLE writer; OfflineProgressCalculator only reads it.
        public double asePerSecondMantissa = 0.0;
        public int asePerSecondExponent = 0;

        /// <summary>Stage index 0–5 (MVP: 6 stages); display number = index + 1.</summary>
        public int currentStage = 0;

        /// <summary>-1 = not chosen (stages 0–2 are path-less); 0=Ane, 1=Sango, 2=Osun.
        /// Reset to -1 every generation (path is re-chosen at the Tier 1 gate).</summary>
        public int currentPath = -1;

        /// <summary>-1 = not chosen; otherwise the index into OriConfig.virtues.
        /// Àkùnlẹ̀yàn (chosen at birth) — set once per generation at the start of
        /// life, reset to -1 every Crossing (Dynasty PRD Phase 1, slice 1).
        /// Add-only field per ADR-0001: old v1 saves load with -1 unchanged.</summary>
        public int chosenOri = -1;

        /// <summary>Steadfastness tally for this life — "held N of M" crossroads kept
        /// true to the Ori. The Crossing reads these to derive AscendChance via the
        /// floor→ceiling curve (ADR-0004). Add-only fields (no schema bump).
        /// CrossroadsSystem owns the writers (slice 2a); TribulationSystem reads them.</summary>
        public int oriHeld = 0;
        public int oriTrials = 0;

        /// <summary>The pending crossroads card's id, or "" when none is active.
        /// Set by CrossroadsSystem when the Àṣẹ milestone fires; cleared when the
        /// player makes a choice. Survives save/load — the crossroads is patient.
        /// Add-only field (no schema bump per ADR-0001).</summary>
        public string pendingCrossroadsId = "";

        /// <summary>Deeds recorded this life — one per resolved Crossroads choice.
        /// Reset by TribulationSystem at the Crossing (the Chronicle will persist
        /// them long-term in a later slice). Add-only field (no schema bump).</summary>
        public List<DeedData> deeds = new List<DeedData>();

        /// <summary>Unix seconds UTC of the last save — the offline-calc anchor.
        /// 0 means "never saved" (fresh install: no offline gain).</summary>
        public long lastSaveTimestamp = 0;

        /// <summary>Unix seconds UTC when the current generation began (gen summary).</summary>
        public long generationStartTimestamp = 0;

        /// <summary>One-time-event bitmask; see <see cref="SeenFlags"/>.</summary>
        public int seenFlags = 0;

        /// <summary>Active Ancestral Council, max 5 (CouncilConfig.maxCouncil).</summary>
        public List<AncestorData> council = new List<AncestorData>();

        /// <summary>Crossroads decisions made in the current life, in encounter order.
        /// The Crossroads system (slices 2a/2b) writes entries; TribulationSystem reads
        /// them at the Crossing to find the Defining Deed. Cleared at each generation
        /// reset alongside all other per-life state. Add-only field per ADR-0001.</summary>
        public List<DeedData> deeds = new List<DeedData>();

        public LineageData lineage = new LineageData();

        // ---- BigNumber bridge helpers (methods, not properties, so Newtonsoft
        //      serializes only the split fields above) ----

        public BigNumber GetAse() => new BigNumber(aseMantissa, aseExponent);

        public void SetAse(BigNumber value)
        {
            aseMantissa = value.Mantissa;
            aseExponent = value.Exponent;
        }

        public BigNumber GetAsePerSecond() => new BigNumber(asePerSecondMantissa, asePerSecondExponent);

        public void SetAsePerSecond(BigNumber value)
        {
            asePerSecondMantissa = value.Mantissa;
            asePerSecondExponent = value.Exponent;
        }

        public bool HasSeen(int flag) => (seenFlags & flag) != 0;

        public void MarkSeen(int flag) => seenFlags |= flag;
    }

    /// <summary>Bit values for <see cref="SaveData.seenFlags"/> (GAMEPLAY §6).</summary>
    public static class SeenFlags
    {
        public const int ChannelHint = 1;
        public const int AscendCeremony = 2;
        public const int FallCeremony = 4;
    }

    /// <summary>
    /// One completed cultivator. Created by TribulationSystem at resolution;
    /// read by AncestralCouncilSystem for the council factor.
    /// </summary>
    [Serializable]
    public class AncestorData
    {
        public int peakStage;
        public int path;
        public bool didAscend;          // true = full power, false = lesser
        public double bonusMultiplier;  // 1.0 if ascended, 0.4 if fallen (locked)
        public long completedTimestamp; // Unix seconds UTC; retirement order key

        /// <summary>How this cultivator is remembered — a Title ("Aṣẹ́gun Adé") for
        /// ascended, a Nickname (the Defining Deed epithet or faithful-fall line) for
        /// fallen. Derived at the Crossing by Remembrance.Derive() before the reset.
        /// Add-only field per ADR-0001: old saves load with null.</summary>
        public string remembrance;
    }

    /// <summary>
    /// One resolved Crossroads decision in a life. beatIndex indexes into
    /// CrossroadsDeckConfig.beats; strayed is true when the cultivator chose
    /// against their Ori vow. Written by the Crossroads system (slices 2a/2b);
    /// read by TribulationSystem to derive the Defining Deed at the Crossing.
    /// Add-only field per ADR-0001: old saves load with an empty list.
    /// </summary>
    [Serializable]
    public class DeedData
    {
        public int beatIndex;  // index into CrossroadsDeckConfig.beats
        public bool strayed;   // true = cultivator chose against their Ori vow
    }

    /// <summary>
    /// A recorded Crossroads choice — one per resolved dilemma this life.
    /// wasOriAligned drives the steadfastness tally; crossroadsId + chosenOptionIndex
    /// will key the deed-tied Nickname in the Chronicle (later slice).
    /// </summary>
    [Serializable]
    public class DeedData
    {
        public string crossroadsId;
        public int chosenOptionIndex;
        public bool wasOriAligned;
    }

    [Serializable]
    public class LineageData
    {
        /// <summary>ADDITIVE accumulator of retired ancestors' contributions
        /// (W × bonusMultiplier each). Lives inside the same (1 + …) term as the
        /// active council sum, which is what makes retirement Àṣẹ-neutral.
        /// Default 0.0 — NOT a multiplier.</summary>
        public double permanentAseBonus = 0.0;

        /// <summary>Completed generations; gen 1 is generationCount == 0.</summary>
        public int generationCount = 0;
    }
}
