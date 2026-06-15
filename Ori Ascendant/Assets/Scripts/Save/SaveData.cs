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

        /// <summary>-1 = not chosen; the virtue (Ori) vowed at the start of this
        /// life — Àkùnlẹ̀yàn — as an index into the Ori set. Reset to -1 every
        /// generation (re-chosen at birth). Add-only field (no schema bump).
        /// See docs/DYNASTY_REDESIGN.md, ADR-0004.</summary>
        public int currentOri = -1;

        /// <summary>Steadfastness tally for this life — "held N of M" crossroads kept
        /// true to the Ori. Reset every generation (DYNASTY_REDESIGN, ADR-0004).</summary>
        public int oriHeld = 0;
        public int oriTrials = 0;

        /// <summary>DEPRECATED (slice 2a): the single pending crossroads index, -1 =
        /// none. Slice 2b migrates this into <see cref="crossroadsQueue"/> on load and
        /// clears it; kept only for back-compat with old saves. Do not write in new code.</summary>
        public int pendingCrossroads = -1;

        /// <summary>The patient queue of crossroads awaiting a choice — beat indices in
        /// draw order (FIFO). Crossroads queue at Àṣẹ milestones (including ones crossed
        /// offline), never expire, and are resolved front-first. Reset every generation.
        /// Add-only field (no schema bump; see header + ADR-0001).</summary>
        public List<int> crossroadsQueue = new List<int>();

        /// <summary>Unix seconds UTC of the last save — the offline-calc anchor.
        /// 0 means "never saved" (fresh install: no offline gain).</summary>
        public long lastSaveTimestamp = 0;

        /// <summary>Unix seconds UTC when the current generation began (gen summary).</summary>
        public long generationStartTimestamp = 0;

        /// <summary>One-time-event bitmask; see <see cref="SeenFlags"/>.</summary>
        public int seenFlags = 0;

        /// <summary>Active Ancestral Council, max 5 (CouncilConfig.maxCouncil).</summary>
        public List<AncestorData> council = new List<AncestorData>();

        /// <summary>Crossroads choices recorded this life — the raw material the
        /// Title/Nickname and the Chronicle draw from. Reset every generation.</summary>
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
    }

    /// <summary>One recorded Crossroads choice (DYNASTY_REDESIGN). The per-life list
    /// of these is the raw material a fallen Nickname and the Chronicle draw from.</summary>
    [Serializable]
    public class DeedData
    {
        public int crossroadsIndex; // which beat in the deck
        public int chosenOri;       // the virtue of the option taken
        public int stage;           // stage index when chosen
        public bool aligned;        // did it hold to the life's Ori?
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
