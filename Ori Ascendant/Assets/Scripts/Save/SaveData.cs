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

        /// <summary>Queue of additional crossroads waiting to be presented after the
        /// currently active one. When multiple milestones are crossed while the player
        /// is away, all fired crossroads accumulate here; they are promoted to
        /// pendingCrossroadsId one-by-one as each choice is resolved.
        /// Persists across save/load and app restart (no expiry). Add-only field (no schema bump).</summary>
        public List<string> pendingCrossroadsQueue = new List<string>();

        /// <summary>The active rival House awaiting a stance (issue #38), or null when none. Per-life — cleared at the Crossing. Add-only.</summary>
        public PendingContest pendingContest = null;

        /// <summary>Contests resolved THIS life — drives the milestone trigger count so a resolved contest doesn't re-fire. Per-life; reset at the Crossing. Add-only.</summary>
        public int contestsResolved = 0;

        /// <summary>Deeds recorded this life — one per resolved Crossroads choice.
        /// CrossroadsSystem writes; TribulationSystem reads at the Crossing to derive
        /// the Defining Deed; cleared at generation reset. Add-only field per ADR-0001.</summary>
        public List<DeedData> deeds = new List<DeedData>();

        /// <summary>Unix seconds UTC of the last save — the offline-calc anchor.
        /// 0 means "never saved" (fresh install: no offline gain).</summary>
        public long lastSaveTimestamp = 0;

        /// <summary>Unix seconds UTC when the current generation began (gen summary).</summary>
        public long generationStartTimestamp = 0;

        /// <summary>Unix seconds UTC when the channel hint was first shown, or 0 if it has
        /// never been shown. Drives ChannelHintDecision across resumes and scene reloads.
        /// Add-only field per ADR-0001; old saves load with 0 so the hint surfaces after
        /// the appear delay on first resume.</summary>
        public long channelHintShownAt = 0;

        /// <summary>One-time-event bitmask; see <see cref="SeenFlags"/>.</summary>
        public int seenFlags = 0;

        /// <summary>Active Ancestral Council, max 5 (CouncilConfig.maxCouncil).</summary>
        public List<AncestorData> council = new List<AncestorData>();

        /// <summary>Unbounded saga record — every completed generation in order.
        /// Unlike the Council (max 5), this list never shrinks: retired ancestors
        /// remain here so the player can read the full bloodline history.
        /// Add-only field per ADR-0001; old saves load with an empty list.</summary>
        public List<ChronicleEntry> chronicle = new List<ChronicleEntry>();

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
    /// One completed generation in the saga, appended to SaveData.chronicle at the
    /// Crossing. Unlike the Council (capped at 5), the Chronicle never shrinks —
    /// every generation is remembered even after retirement. Add-only per ADR-0001.
    /// </summary>
    [Serializable]
    public class ChronicleEntry
    {
        public int generationNumber;    // 1-based
        public int chosenOri;           // virtue index, or -1 if no vow was held
        public bool didAscend;
        public string remembrance;      // Title (ascend) or Nickname (fall); null for legacy
        public long completedTimestamp; // Unix seconds UTC

        /// <summary>The crossroads card ID from the Defining Deed of this life (the first
        /// strayed choice), or "" if the life was faithful. Stored so descendants may
        /// encounter the same crossroads (light dynasty compounding, issue #8).
        /// Add-only field per ADR-0001; old saves load with null → treated as empty.</summary>
        public string forebearCrossroadsId;
    }

    /// <summary>
    /// One resolved Crossroads decision in a life. Written by CrossroadsSystem when
    /// the player makes a choice; read by Remembrance.Derive at the Crossing to find
    /// the Defining Deed (the first stray). beatIndex is the card's position in the
    /// CrossroadsConfig deck (parallel to CrossroadsDeckConfig.beats). strayed mirrors
    /// !wasOriAligned for Remembrance's lookup without a runtime negation.
    /// Add-only field per ADR-0001: old saves load with an empty list.
    /// Add-only per ADR-0001: old saves load with field defaults.
    /// </summary>
    [Serializable]
    public class DeedData
    {
        public string crossroadsId;     // persistent card id from CrossroadsConfig.deck
        public int chosenOptionIndex;   // which option the player chose
        public bool wasOriAligned;      // true = chose the Ori-vow option
        public int beatIndex;           // index into CrossroadsDeckConfig.beats for epithet lookup
        public bool strayed;            // true = !wasOriAligned, kept in sync for Remembrance
    }

    /// <summary>The rival House currently awaiting the player's stance (issue #38), persisted
    /// so a challenger that appeared before the app closed is the SAME House on return. Stance
    /// stored as an int to keep SaveData free of a Systems dependency. Per-life — cleared at the
    /// Crossing. Add-only (ADR-0001).</summary>
    [Serializable]
    public class PendingContest
    {
        public string houseName;
        public int housePath;
        public double housePowerRatio;
        public int houseStance; // (int)OriAscendant.Systems.Stance
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

        /// <summary>Lineage-permanent Marketplace standing (issue #35). Raises the
        /// production rate via a CAPPED additive term inside the lineage factor but OUTSIDE
        /// the councilBonusModifier (Osun ×2) wrap — so it leaves retirement neutrality intact.
        /// The stored value is UNCAPPED (it will later drive marketplace rank); only its rate
        /// bonus is capped. Add-only per ADR-0001 — old saves load with 0.0.</summary>
        public double renown = 0.0;
    }
}
