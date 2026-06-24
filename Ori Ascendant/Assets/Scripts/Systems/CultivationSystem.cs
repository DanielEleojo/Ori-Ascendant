using System;
using System.Linq;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>Result of a manual Advance attempt (GAMEPLAY §3.3, §4.3).</summary>
    public enum AdvanceOutcome
    {
        Advanced,
        NeedsPathChoice,   // at the Tier 1 gate with no path — choosing IS the advance
        ThresholdNotMet,
        AtFinalStage,      // stage 6 is gated by the Tribulation, not Advance
    }

    /// <summary>
    /// Stage / path / tier state (TECH_DESIGN §4). Owns: manual advancement
    /// (one stage per tap), the mandatory path gate out of the Tier 0 peak,
    /// and the once-per-generation Tribulation eligibility announcement.
    /// Writes SaveData.currentStage / currentPath; triggers rate recalc + save
    /// on every progression event. Does NOT own Àṣẹ amounts or Tribulation
    /// resolution. Dependencies resolve via TryGet so the pure flows stay
    /// testable on bare hosts.
    /// </summary>
    public class CultivationSystem : MonoBehaviour
    {
        [SerializeField] private CultivationStageConfig[] _stages; // index = stage index, 6 for MVP
        [SerializeField] private PathConfig[] _paths;              // 0=Ane, 1=Sango, 2=Osun
        [SerializeField] private TribulationConfig _tribulationConfig;

        /// <summary>Raised after currentStage changes (new stage index).</summary>
        public event Action<int> OnStageAdvanced;

        /// <summary>Raised after a path is chosen (path index).</summary>
        public event Action<int> OnPathChosen;

        /// <summary>Raised once per generation when the Tribulation becomes available.</summary>
        public event Action OnTribulationAvailable;

        private SaveData _save;
        private StageProgression _progression;
        private int _pathGateStageIndex;
        private bool _tribulationAnnounced;

        // ---- read accessors (AseGenerationSystem + UI) ----

        public CultivationStageConfig CurrentStageConfig =>
            _save != null && _stages != null ? _stages[_save.currentStage] : null;

        public PathConfig CurrentPathConfig =>
            _save != null && _paths != null && _save.currentPath >= 0 && _save.currentPath < _paths.Length
                ? _paths[_save.currentPath] : null;

        public PathConfig[] Paths => _paths;

        public double StageProductionMultiplier => CurrentStageConfig?.productionMultiplier ?? 1.0;

        // currentPath == -1 (stages 1–3 path-less) ⇒ every modifier reads 1.0.
        public double PathOnlineMultiplier => CurrentPathConfig?.aseGenerationModifier ?? 1.0;
        public double PathOfflineRateModifier => CurrentPathConfig?.offlineRateModifier ?? 1.0;
        public double CouncilBonusModifier => CurrentPathConfig?.councilBonusModifier ?? 1.0;

        public bool IsAtFinalStage => _save != null && _progression != null &&
                                      _save.currentStage == _progression.FinalStageIndex;

        /// <summary>Total stage count — used by VesselFillRatio to normalise fill.</summary>
        public int StageCount => _progression?.StageCount ?? 0;

        public bool IsAtPathGate => _save != null && _save.currentStage == _pathGateStageIndex &&
                                    _save.currentPath < 0;

        /// <summary>Cumulative target the current stage fills toward (UI progress bar).</summary>
        public BigNumber CurrentTarget => _progression?.TargetFor(_save?.currentStage ?? 0) ?? BigNumber.Zero;

        /// <summary>Display name of an arbitrary stage index (UI "Next: …" label).</summary>
        public string PeekStageName(int stageIndex) =>
            _stages != null && stageIndex >= 0 && stageIndex < _stages.Length
                ? _stages[stageIndex].stageName : null;

        /// <summary>Live eligibility check for the CTA (the announcement event
        /// fires once; this is the polled state).</summary>
        public bool IsTribulationEligibleNow =>
            _save != null && _progression != null &&
            _progression.IsTribulationEligible(_save.currentStage, _save.GetAse());

        /// <summary>Called by TribulationSystem inside the atomic resolve: the
        /// save fields are already reset; this re-arms the once-per-generation
        /// announcement for generation N+1.</summary>
        public void ResetForNewGeneration()
        {
            _tribulationAnnounced = false;
        }

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            UnsubscribeAse();
            ServiceLocator.Unregister(this);
        }

        /// <summary>Called by GameManager after the save is loaded (before AseGen begins).</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));

            BigNumber[] thresholds = _stages.Select(s => s.GetAdvanceThreshold()).ToArray();
            _progression = new StageProgression(thresholds, _tribulationConfig.GetAseThreshold());

            // The path gate is the Tier 0 peak — derived from config, not hardcoded.
            _pathGateStageIndex = 0;
            for (int i = 0; i < _stages.Length; i++)
            {
                if (_stages[i].tier == 0) _pathGateStageIndex = i;
            }

            _tribulationAnnounced = false;
            SubscribeAse();
            CheckTribulationEligibility(); // a loaded save may already be eligible
        }

        public bool CanAdvance() =>
            _save != null && _progression.CanAdvance(_save.currentStage, _save.GetAse());

        /// <summary>Manual Advance — exactly one stage per call (multi-advance is
        /// the player tapping again while still over the next threshold).</summary>
        public AdvanceOutcome TryAdvance()
        {
            if (_save == null) return AdvanceOutcome.ThresholdNotMet;
            if (_save.currentStage >= _progression.FinalStageIndex) return AdvanceOutcome.AtFinalStage;
            if (!CanAdvance()) return AdvanceOutcome.ThresholdNotMet;
            if (IsAtPathGate) return AdvanceOutcome.NeedsPathChoice;

            DoAdvance();
            return AdvanceOutcome.Advanced;
        }

        /// <summary>Commits the path AND advances into Tier 1 — choosing IS the
        /// advance (GAMEPLAY §3.3). Locked for the rest of the generation.</summary>
        public bool ChoosePath(int pathIndex)
        {
            if (_save == null || !IsAtPathGate || !CanAdvance()) return false;
            if (pathIndex < 0 || pathIndex >= _paths.Length) return false;

            _save.currentPath = pathIndex;
            OnPathChosen?.Invoke(pathIndex);
            DoAdvance();
            return true;
        }

        private void DoAdvance()
        {
            _save.currentStage++;
            OnStageAdvanced?.Invoke(_save.currentStage);

            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen)) aseGen.RecalculateRate();
            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save(); // progression event

            CheckTribulationEligibility();
        }

        private void SubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
            {
                aseGen.OnAseChanged -= HandleAseChanged; // defensive against double-Begin
                aseGen.OnAseChanged += HandleAseChanged;
            }
        }

        private void UnsubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
            {
                aseGen.OnAseChanged -= HandleAseChanged;
            }
        }

        private void HandleAseChanged(BigNumber _) => CheckTribulationEligibility();

        private void CheckTribulationEligibility()
        {
            if (_tribulationAnnounced || _save == null || _progression == null) return;
            if (_progression.IsTribulationEligible(_save.currentStage, _save.GetAse()))
            {
                _tribulationAnnounced = true; // once per generation (reset in Begin / Phase C reset)
                OnTribulationAvailable?.Invoke();
            }
        }
    }
}
