using System;
using System.Collections.Generic;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>Everything the post-resolution UI needs, captured at the moment
    /// of the Crossing (the save itself already holds generation N+1).</summary>
    public sealed class TribulationResult
    {
        public bool DidAscend;
        public AncestorData Ancestor;
        public AncestorData RetiredAncestor;            // null unless the council was full
        public int CompletedGenerationNumber;            // 1-based, the generation that just ended
        public long TimeInGenerationSeconds;
        public int PathIndexAtCrossing;
        public BigNumber PeakAse;
        public double LineageFactorBefore;               // neutral basis (path-less), for the delta line
        public double LineageFactorAfter;
        public BigNumber OldStage1Rate;                  // what gen N's stage-1 rate was
        public BigNumber NewStage1Rate;                  // gen N+1's actual starting rate
        public double RenownGranted;                     // renown the Crossing granted the lineage (issue #36)
    }

    /// <summary>
    /// Ìrékọjá resolution (TECH_DESIGN §4, GAMEPLAY §3.5/§4.4). ROLL ONCE,
    /// PERSIST FIRST: the ascend roll resolves at confirmation and the COMPLETE
    /// next-generation state is written and saved before any ceremony frame
    /// plays — if the app dies mid-animation, the outcome survives. The ceremony
    /// is replayable theater driven by the returned <see cref="TribulationResult"/>.
    /// Odds derive from steadfastness (held/trials) via the config floor→ceiling
    /// curve (ADR-0004), shown on the confirm sheet exactly as rolled; identical on
    /// every path (PathConfig.tribulationType is presentation-only).
    /// </summary>
    public class TribulationSystem : MonoBehaviour
    {
        [SerializeField] private TribulationConfig _config;
        [SerializeField] private GameplayConfig _gameplayConfig;
        [SerializeField] private RemembranceConfig _remembranceConfig;
        [SerializeField] private CrossroadsDeckConfig _crossroadsDeck;

        /// <summary>Locked signature (TECH_DESIGN §4). Notification-only: all
        /// state is already written and saved when this fires.</summary>
        public event Action<bool, AncestorData> OnTribulationComplete;

        private SaveData _save;
        private IRandomSource _random = new UnityRandomSource();

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        /// <summary>Test seam — production keeps the default UnityRandomSource.</summary>
        public void SetRandomSource(IRandomSource random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        /// <summary>Bonus to AscendChance from holding the same Ori across consecutive
        /// generations (light dynasty compounding, issue #8). Counts consecutive tail
        /// entries in the chronicle where chosenOri matches the current life's chosenOri,
        /// reading backwards from the most recent. Bounded by config.lineLegacyMaxBonus.
        /// Returns 0 when the current life has no Ori vow (chosenOri == -1) or no
        /// matching streak exists.</summary>
        public double LineLegacyBonus
        {
            get
            {
                if (_save?.chronicle == null || _save.chosenOri < 0 || _config == null) return 0.0;
                int consecutive = 0;
                for (int i = _save.chronicle.Count - 1; i >= 0; i--)
                {
                    if (_save.chronicle[i].chosenOri == _save.chosenOri)
                        consecutive++;
                    else
                        break;
                }
                double bonus = consecutive * _config.lineLegacyBonusPerGen;
                return Math.Min(bonus, _config.lineLegacyMaxBonus);
            }
        }

        /// <summary>Ascend probability for the current life, derived from steadfastness
        /// (held/trials) via the config floor→ceiling curve, plus the line-legacy bonus
        /// (bounded, clamped to the ceiling — ADR-0004 / issue #8). trials==0 → floor
        /// (a life that faced no resolved crossroads earns no steadfastness credit).
        /// Reads ONLY the tally + config — never the deity-Path (ADR-0004 orthogonality).
        /// The confirm sheet shows this exact value, and Resolve rolls against it.</summary>
        public double AscendChance
        {
            get
            {
                if (_save == null || _config == null) return 0.0;
                if (_save.oriTrials <= 0)
                    return Math.Min(_config.ascendFloor + LineLegacyBonus, _config.ascendCeiling);
                double rate = (double)_save.oriHeld / _save.oriTrials;
                double baseChance = _config.ascendFloor + (_config.ascendCeiling - _config.ascendFloor) * rate;
                return Math.Min(baseChance + LineLegacyBonus, _config.ascendCeiling);
            }
        }

        /// <summary>Re-checks eligibility from state — never trusts the UI.</summary>
        public bool CanResolve()
        {
            if (_save == null || _config == null) return false;
            if (!ServiceLocator.TryGet(out CultivationSystem cultivation)) return false;
            return cultivation.IsAtFinalStage && _save.GetAse() >= _config.GetAseThreshold();
        }

        /// <summary>
        /// The atomic Crossing. Mutation order (GAMEPLAY §4.4) is unchanged — it is now
        /// expressed as named phases for locality: roll → name draw → remembrance →
        /// ancestor (from pre-reset state) → forebear id → chronicle → pre-reset result
        /// snapshot → <see cref="CommitAtomicWrite"/> (the single, un-reorderable atomic
        /// block: induct → reset → recompute rate → re-arm → SAVE → cloud push) → notify.
        /// Returns null when ineligible. All random draws stay in the same order so the
        /// FakeRandom sequence in tests is preserved.
        /// </summary>
        public TribulationResult Resolve()
        {
            if (!CanResolve()) return null;

            ResolveServices(out CultivationSystem cultivation,
                            out AncestralCouncilSystem council,
                            out AseGenerationSystem aseGen);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            bool ascended = RollOutcome();
            int nameIndex = DrawPersonalName(ascended);
            string remembrance = DeriveRemembrance(ascended, cultivation, nameIndex);
            AncestorData ancestor = BuildAncestor(ascended, now, remembrance);
            string forebearCrossroadsId = FindForebearCrossroadsId();
            AppendChronicle(ascended, now, remembrance, forebearCrossroadsId);

            TribulationResult result = BuildResultPreReset(ascended, ancestor, council, now);

            CommitAtomicWrite(result, ancestor, council, aseGen, cultivation, now);

            NotifyComplete(ascended, ancestor);
            return result;
        }

        // ---- Crossing phases — extracted from Resolve for locality (GAMEPLAY §4.4).
        //      Order, randomness sequence, and the atomic-write boundary are unchanged. ----

        /// <summary>Resolves the three sibling systems the Crossing touches. Any may be
        /// null on a bare test host — TryGet keeps them neutral, exactly as the old inline
        /// gets did.</summary>
        private static void ResolveServices(out CultivationSystem cultivation,
            out AncestralCouncilSystem council, out AseGenerationSystem aseGen)
        {
            ServiceLocator.TryGet(out cultivation);
            ServiceLocator.TryGet(out council);
            ServiceLocator.TryGet(out aseGen);
        }

        /// <summary>The single ascend roll (roll-once rule), against the exact AscendChance
        /// the confirm sheet showed.</summary>
        private bool RollOutcome() => _random.NextDouble() < AscendChance;

        /// <summary>Second random draw — personal-name selection for an ascended cultivator
        /// (a fall uses no randomness). Drawn BEFORE the reset so the sequence stays
        /// deterministic against the test FakeRandom. Returns 0 with no name pool or a fall.</summary>
        private int DrawPersonalName(bool ascended)
        {
            if (!ascended || !(_remembranceConfig?.personalNames?.Length > 0)) return 0;
            double nameRoll = _random.NextDouble();
            int nameIndex = (int)(nameRoll * _remembranceConfig.personalNames.Length);
            return Math.Min(nameIndex, _remembranceConfig.personalNames.Length - 1);
        }

        /// <summary>Derives how this life is remembered from the honorific + deeds, BEFORE
        /// the atomic reset clears them. Pure presentation — never touches odds.</summary>
        private string DeriveRemembrance(bool ascended, CultivationSystem cultivation, int nameIndex)
        {
            string honorific = cultivation?.PeekStageName(_save.currentStage) ?? string.Empty;
            return Remembrance.Derive(
                ascended, honorific, _save.deeds,
                _crossroadsDeck, _remembranceConfig, nameIndex);
        }

        /// <summary>Builds the ancestor record from pre-reset state. A fall still produces an
        /// ancestor (bonusMultiplier 0.4) — never a dead end (locked, PRD §6).</summary>
        private AncestorData BuildAncestor(bool ascended, long now, string remembrance) =>
            new AncestorData
            {
                peakStage = _save.currentStage,
                path = _save.currentPath,
                didAscend = ascended,
                bonusMultiplier = ascended ? 1.0 : 0.4, // locked: a fall still produces an ancestor
                completedTimestamp = now,
                remembrance = remembrance,
            };

        /// <summary>Forebear compounding (issue #8): the Defining Deed's card ID (first stray),
        /// stored in the chronicle so descendants may be offered the same crossroads.</summary>
        private string FindForebearCrossroadsId()
        {
            if (_save.deeds == null) return "";
            foreach (var deed in _save.deeds)
                if (deed.strayed && !string.IsNullOrEmpty(deed.crossroadsId))
                    return deed.crossroadsId;
            return "";
        }

        /// <summary>Appends the unbounded saga record BEFORE the reset so pre-reset fields
        /// (chosenOri, generationCount) are captured intact. Survives Council retirement (issue #7).</summary>
        private void AppendChronicle(bool ascended, long now, string remembrance, string forebearCrossroadsId)
        {
            _save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = _save.lineage.generationCount + 1,
                chosenOri = _save.chosenOri,
                didAscend = ascended,
                remembrance = remembrance,
                completedTimestamp = now,
                forebearCrossroadsId = forebearCrossroadsId,
            });
        }

        /// <summary>Captures everything the post-Crossing UI needs from pre-reset state. The
        /// OldStage1Rate uses a NEUTRAL (path-less) basis — the honest comparable, since
        /// generation N+1 starts with no path.</summary>
        private TribulationResult BuildResultPreReset(
            bool ascended, AncestorData ancestor, AncestralCouncilSystem council, long now)
        {
            double sumBefore = council?.ActiveCouncilSum ?? 0.0;
            double permBefore = _save.lineage.permanentAseBonus;
            double baseRate = _gameplayConfig != null ? _gameplayConfig.baseRate : 1.0;

            return new TribulationResult
            {
                DidAscend = ascended,
                Ancestor = ancestor,
                CompletedGenerationNumber = _save.lineage.generationCount + 1,
                TimeInGenerationSeconds = Math.Max(0, now - _save.generationStartTimestamp),
                PathIndexAtCrossing = _save.currentPath,
                PeakAse = _save.GetAse(),
                // Delta shown on a NEUTRAL (path-less) basis — generation N+1
                // starts with no path, so this is the honest comparable.
                LineageFactorBefore = 1.0 + (permBefore + sumBefore),
                OldStage1Rate = RateCalculator.ComputeRate(baseRate, 1.0, 1.0, 1.0, permBefore, sumBefore),
            };
        }

        /// <summary>The atomic Crossing write — kept as ONE method by design so the
        /// persist-before-notify boundary cannot be accidentally reordered. Induct →
        /// reset to generation N+1 → recompute rate (AseGen, sole writer) → re-arm the
        /// once-per-generation announcement → fill after-rates → SAVE TO DISK →
        /// opportunistic cloud push. If the app dies mid-ceremony, the outcome is already
        /// persisted.</summary>
        private void CommitAtomicWrite(TribulationResult result, AncestorData ancestor,
            AncestralCouncilSystem council, AseGenerationSystem aseGen, CultivationSystem cultivation, long now)
        {
            // ---- atomic write begins ----
            result.RetiredAncestor = council != null ? council.InductAncestor(ancestor) : null;

            _save.SetAse(BigNumber.Zero);
            _save.currentStage = 0;
            _save.currentPath = -1;
            _save.chosenOri = -1;           // Àkùnlẹ̀yàn is re-vowed at the start of the next life
            _save.oriHeld = 0;              // steadfastness tally is per-life
            _save.oriTrials = 0;
            _save.pendingCrossroadsId = ""; // patient crossroads expire at the Crossing
            _save.pendingCrossroadsQueue?.Clear(); // whole queue expires too
            _save.pendingContest = null;     // contests are per-life cadence (issue #38)
            _save.contestsResolved = 0;
            if (_save.deeds != null) _save.deeds.Clear(); // per-life history; Crossroads system writes, reset clears
            _save.generationStartTimestamp = now;
            _save.lineage.generationCount++;

            // Renown grant (issue #36): the Crossing feeds the Marketplace. Applied INSIDE the
            // atomic block (before Save → persist-first) and BEFORE RecalculateRate so the new
            // generation's rate already reflects it. Outside the council wrap (path-agnostic).
            double renownGrant = result.DidAscend ? _config.ascendRenownGrant : _config.fallRenownGrant;
            _save.lineage.renown += renownGrant;
            result.RenownGranted = renownGrant;

            aseGen?.RecalculateRate();          // gen N+1 stage-1 rate, council factor included
            cultivation?.ResetForNewGeneration(); // re-arm the once-per-generation announcement

            double sumAfter = council?.ActiveCouncilSum ?? 0.0;
            result.LineageFactorAfter = 1.0 + (_save.lineage.permanentAseBonus + sumAfter);
            result.NewStage1Rate = _save.GetAsePerSecond();

            if (ServiceLocator.TryGet(out SaveManager saveManager))
            {
                saveManager.Save(); // persisted BEFORE any ceremony frame
            }
            // Cloud sync on every Tribulation completion (locked business rule);
            // opportunistic + silent, inert when no provider is available.
            if (ServiceLocator.TryGet(out CloudSaveManager cloud)) cloud.PushLatest();
            // ---- atomic write ends ----
        }

        /// <summary>Fires the locked notification — all state is already written and saved.</summary>
        private void NotifyComplete(bool ascended, AncestorData ancestor) =>
            OnTribulationComplete?.Invoke(ascended, ancestor);
    }
}
