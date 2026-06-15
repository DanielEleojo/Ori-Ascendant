using System;
using System.Collections.Generic;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Climb-tied Crossroads (DYNASTY_REDESIGN, slices 2a/2b). Dilemmas are drawn at
    /// Àṣẹ milestones (each non-final stage's advance threshold, so they are detected
    /// from accrued Àṣẹ rather than the manual Advance tap — a long absence that banks
    /// past several thresholds queues several at once). They are held in a patient
    /// queue that never expires and survives save/load, and are resolved front-first;
    /// each choice records a Deed and moves the steadfastness tally ("held N of M")
    /// toward or away from the vowed Ori. UI never writes state: it calls Choose.
    /// Dependencies resolve via ServiceLocator so the pure flows stay testable on bare
    /// hosts.
    /// </summary>
    public class CrossroadsSystem : MonoBehaviour
    {
        [SerializeField] private CrossroadsDeckConfig _deck;
        [SerializeField] private CultivationStageConfig[] _stages; // milestone schedule = advance thresholds

        /// <summary>Raised when a crossroads is drawn and enqueued.</summary>
        public event Action<CrossroadsBeat> OnCrossroadsPresented;

        /// <summary>Raised after a pending crossroads is resolved by a choice.</summary>
        public event Action OnCrossroadsResolved;

        private SaveData _save;
        private AseGenerationSystem _aseGen;
        private BigNumber[] _milestones; // cumulative Àṣẹ at which each crossroads is drawn

        public bool HasPending => _save != null && _save.crossroadsQueue.Count > 0;

        /// <summary>How many crossroads are waiting in the patient queue.</summary>
        public int PendingCount => _save?.crossroadsQueue.Count ?? 0;

        public CrossroadsBeat PendingBeat =>
            HasPending && _deck != null && _deck.beats != null &&
            _save.crossroadsQueue[0] < _deck.beats.Length
                ? _deck.beats[_save.crossroadsQueue[0]] : null;

        public int Held => _save?.oriHeld ?? 0;
        public int Trials => _save?.oriTrials ?? 0;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            Unsubscribe();
            ServiceLocator.Unregister(this);
        }

        /// <summary>Called by GameManager after the save is loaded. Migrates any
        /// slice-2a single-pending crossroads into the queue, derives the milestone
        /// schedule, subscribes to Àṣẹ changes, and catches up any milestones the
        /// loaded total already crosses.</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            if (_save.crossroadsQueue == null) _save.crossroadsQueue = new List<int>();

            // Migrate the deprecated slice-2a single-pending field.
            if (_save.pendingCrossroads >= 0)
            {
                if (!_save.crossroadsQueue.Contains(_save.pendingCrossroads))
                    _save.crossroadsQueue.Add(_save.pendingCrossroads);
                _save.pendingCrossroads = -1;
            }

            _milestones = BuildMilestones();
            Subscribe();
            CheckMilestones(); // a loaded save may already sit past undrawn milestones
        }

        /// <summary>Milestone schedule = each non-final stage's advance threshold
        /// (cumulative Àṣẹ); the final stage is Tribulation-gated, not a milestone.
        /// Placeholder reuse pre balance-sim (DYNASTY_REDESIGN "density is a rate").</summary>
        private BigNumber[] BuildMilestones()
        {
            if (_stages == null || _stages.Length < 2) return Array.Empty<BigNumber>();
            int count = _stages.Length - 1;
            var milestones = new BigNumber[count];
            for (int i = 0; i < count; i++) milestones[i] = _stages[i].GetAdvanceThreshold();
            return milestones;
        }

        private void Subscribe()
        {
            if (ServiceLocator.TryGet(out _aseGen))
            {
                _aseGen.OnAseChanged -= HandleAseChanged; // defensive against double-Begin
                _aseGen.OnAseChanged += HandleAseChanged;
            }
        }

        private void Unsubscribe()
        {
            if (_aseGen != null) _aseGen.OnAseChanged -= HandleAseChanged;
        }

        private void HandleAseChanged(BigNumber _) => CheckMilestones();

        /// <summary>Draws a crossroads for every milestone the accrued Àṣẹ has crossed
        /// but not yet drawn, appending to the patient queue (sequential: the next beat
        /// index is resolved + queued). Caps at the deck length. Idempotent — safe to
        /// call on every Àṣẹ change.</summary>
        public void CheckMilestones()
        {
            if (_save == null || _deck == null || _deck.beats == null || _milestones == null) return;

            bool drew = false;
            int drawn = _save.oriTrials + _save.crossroadsQueue.Count;
            while (drawn < _milestones.Length && drawn < _deck.beats.Length &&
                   _save.GetAse() >= _milestones[drawn])
            {
                _save.crossroadsQueue.Add(drawn);
                OnCrossroadsPresented?.Invoke(_deck.beats[drawn]);
                drawn++;
                drew = true;
            }

            if (drew && ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save(); // progression event
        }

        /// <summary>Resolves the front crossroads with the chosen option: records a
        /// Deed, moves the steadfastness tally, and dequeues it.</summary>
        public bool Choose(int optionIndex)
        {
            if (!HasPending || _deck == null || _deck.beats == null) return false;
            int beatIndex = _save.crossroadsQueue[0];
            if (beatIndex < 0 || beatIndex >= _deck.beats.Length) return false;
            CrossroadsBeat beat = _deck.beats[beatIndex];
            if (beat.options == null || optionIndex < 0 || optionIndex >= beat.options.Length) return false;

            CrossroadsOption option = beat.options[optionIndex];
            bool aligned = option.oriIndex == _save.currentOri;

            _save.oriTrials++;
            if (aligned) _save.oriHeld++;
            _save.deeds.Add(new DeedData
            {
                crossroadsIndex = beatIndex,
                chosenOri = option.oriIndex,
                stage = _save.currentStage,
                aligned = aligned,
            });
            _save.crossroadsQueue.RemoveAt(0);

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save(); // progression event
            OnCrossroadsResolved?.Invoke();
            return true;
        }
    }
}
