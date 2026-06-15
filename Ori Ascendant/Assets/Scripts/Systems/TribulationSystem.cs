using System;
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
    }

    /// <summary>
    /// Ìrékọjá resolution (TECH_DESIGN §4, GAMEPLAY §3.5/§4.4). ROLL ONCE,
    /// PERSIST FIRST: the 60/40 coin resolves at confirmation and the COMPLETE
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

        /// <summary>Ascend probability for the current life, derived from steadfastness
        /// (held/trials) via the config floor→ceiling curve. trials==0 → floor (a life
        /// that faced no resolved crossroads earns no steadfastness credit). Reads ONLY
        /// the tally + config — never the deity-Path (ADR-0004 orthogonality). The
        /// confirm sheet shows this exact value, and Resolve rolls against it.</summary>
        public double AscendChance
        {
            get
            {
                if (_save == null || _config == null) return 0.0;
                if (_save.oriTrials <= 0) return _config.ascendFloor;
                double rate = (double)_save.oriHeld / _save.oriTrials;
                return _config.ascendFloor + (_config.ascendCeiling - _config.ascendFloor) * rate;
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
        /// The atomic Crossing. Mutation order (GAMEPLAY §4.4): roll → ancestor
        /// built from pre-reset state → council induction (retire-first, via
        /// AncestralCouncilSystem's synchronous API — see its ownership note) →
        /// generation reset → rate recompute (AseGen, sole writer) → eligibility
        /// re-arm → SAVE TO DISK → notify. Returns null when ineligible.
        /// </summary>
        public TribulationResult Resolve()
        {
            if (!CanResolve()) return null;

            ServiceLocator.TryGet(out CultivationSystem cultivation);
            ServiceLocator.TryGet(out AncestralCouncilSystem council);
            ServiceLocator.TryGet(out AseGenerationSystem aseGen);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool ascended = _random.NextDouble() < AscendChance;

            var ancestor = new AncestorData
            {
                peakStage = _save.currentStage,
                path = _save.currentPath,
                didAscend = ascended,
                bonusMultiplier = ascended ? 1.0 : 0.4, // locked: a fall still produces an ancestor
                completedTimestamp = now,
            };

            double w = council != null ? council.W : 0.25;
            double sumBefore = council?.ActiveCouncilSum ?? 0.0;
            double permBefore = _save.lineage.permanentAseBonus;
            double baseRate = _gameplayConfig != null ? _gameplayConfig.baseRate : 1.0;

            var result = new TribulationResult
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

            // ---- atomic write begins ----
            result.RetiredAncestor = council != null ? council.InductAncestor(ancestor) : null;

            _save.SetAse(BigNumber.Zero);
            _save.currentStage = 0;
            _save.currentPath = -1;
            _save.currentOri = -1;
            _save.oriHeld = 0;
            _save.oriTrials = 0;
            _save.pendingCrossroads = -1;
            _save.crossroadsQueue.Clear();
            _save.deeds.Clear();
            _save.generationStartTimestamp = now;
            _save.lineage.generationCount++;

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

            OnTribulationComplete?.Invoke(ascended, ancestor);
            return result;
        }
    }
}
