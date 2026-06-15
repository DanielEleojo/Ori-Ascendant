using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Climb-tied Crossroads (DYNASTY_REDESIGN). On each stage advance a dilemma is
    /// drawn from the deck and held pending until the player chooses; the choice
    /// records a Deed and moves the steadfastness tally ("held N of M") toward or
    /// away from the vowed Ori. There is no timer — a pending crossroads waits (slice
    /// 2b adds the multi-beat queue across check-ins). UI never writes state: it
    /// calls Choose. Dependencies resolve via ServiceLocator so the pure flows stay
    /// testable on bare hosts.
    /// </summary>
    public class CrossroadsSystem : MonoBehaviour
    {
        [SerializeField] private CrossroadsDeckConfig _deck;

        /// <summary>Raised when a crossroads is drawn and becomes pending.</summary>
        public event Action<CrossroadsBeat> OnCrossroadsPresented;

        /// <summary>Raised after a pending crossroads is resolved by a choice.</summary>
        public event Action OnCrossroadsResolved;

        private SaveData _save;
        private CultivationSystem _cultivation;

        public bool HasPending => _save != null && _save.pendingCrossroads >= 0;

        public CrossroadsBeat PendingBeat =>
            HasPending && _deck != null && _deck.beats != null &&
            _save.pendingCrossroads < _deck.beats.Length
                ? _deck.beats[_save.pendingCrossroads] : null;

        public int Held => _save?.oriHeld ?? 0;
        public int Trials => _save?.oriTrials ?? 0;

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            Unsubscribe();
            ServiceLocator.Unregister(this);
        }

        /// <summary>Called by GameManager after the save is loaded (subscribes to the
        /// climb so a crossroads is drawn on each advance).</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            Subscribe();
        }

        private void Subscribe()
        {
            if (ServiceLocator.TryGet(out _cultivation))
            {
                _cultivation.OnStageAdvanced -= HandleStageAdvanced; // defensive against double-Begin
                _cultivation.OnStageAdvanced += HandleStageAdvanced;
            }
        }

        private void Unsubscribe()
        {
            if (_cultivation != null) _cultivation.OnStageAdvanced -= HandleStageAdvanced;
        }

        private void HandleStageAdvanced(int _) => TryPresentNext();

        /// <summary>Draws the next beat (sequential) and holds it pending — unless one
        /// is already pending or the deck is exhausted for this life.</summary>
        public void TryPresentNext()
        {
            if (_save == null || _deck == null || _deck.beats == null || HasPending) return;
            int next = _save.oriTrials; // sequential: one beat per resolution this life
            if (next < 0 || next >= _deck.beats.Length) return;

            _save.pendingCrossroads = next;
            OnCrossroadsPresented?.Invoke(_deck.beats[next]);
        }

        /// <summary>Resolves the pending crossroads with the chosen option: records a
        /// Deed, moves the steadfastness tally, and clears the pending state.</summary>
        public bool Choose(int optionIndex)
        {
            if (!HasPending || _deck == null) return false;
            CrossroadsBeat beat = _deck.beats[_save.pendingCrossroads];
            if (beat.options == null || optionIndex < 0 || optionIndex >= beat.options.Length) return false;

            CrossroadsOption option = beat.options[optionIndex];
            bool aligned = option.oriIndex == _save.currentOri;

            _save.oriTrials++;
            if (aligned) _save.oriHeld++;
            _save.deeds.Add(new DeedData
            {
                crossroadsIndex = _save.pendingCrossroads,
                chosenOri = option.oriIndex,
                stage = _save.currentStage,
                aligned = aligned,
            });
            _save.pendingCrossroads = -1;

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save(); // progression event
            OnCrossroadsResolved?.Invoke();
            return true;
        }
    }
}
