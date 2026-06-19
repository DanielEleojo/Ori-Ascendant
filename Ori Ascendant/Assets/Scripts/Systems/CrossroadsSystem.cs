using System;
using System.Collections.Generic;
using System.Linq;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Crossroads — virtue-testing dilemma events (Dynasty PRD Phase 1, slices 2a/2b).
    /// Owns SaveData.pendingCrossroadsId, SaveData.pendingCrossroadsQueue, and SaveData.deeds writes.
    ///
    /// Milestones: CrossroadsConfig defines one or more Àṣẹ thresholds. When the player's
    /// accumulated Àṣẹ surpasses more milestones than there are already-triggered crossroads
    /// this life, the excess are queued. The queue is patient — cards wait indefinitely, and
    /// persist across save/load and app restart (no expiry).
    ///
    /// Queue protocol: the active crossroads sits in SaveData.pendingCrossroadsId; extras wait
    /// in SaveData.pendingCrossroadsQueue. After a choice is made, the next item is promoted
    /// from the queue and OnCrossroadsReady fires again.
    ///
    /// Choice: MakeChoice(optionIndex) records a Deed, updates the steadfastness tally
    /// (oriHeld / oriTrials in SaveData), dequeues the next crossroads if any, then
    /// fires OnCrossroadsResolved. oriHeld increments only when the chosen option's
    /// virtueIndex matches the life's chosenOri; oriTrials always increments.
    /// </summary>
    public class CrossroadsSystem : MonoBehaviour
    {
        [SerializeField] private CrossroadsConfig _config;

        /// <summary>Raised when a crossroads becomes active (card id). Fires on
        /// Begin() if a pending crossroads survived from a previous session, and
        /// again each time the next queued crossroads is promoted after a choice.</summary>
        public event Action<string> OnCrossroadsReady;

        /// <summary>Raised after a choice is committed (tally updated, deed recorded,
        /// state saved). Notification-only — all writes precede this.</summary>
        public event Action<DeedData> OnCrossroadsResolved;

        private SaveData _save;
        private IRandomSource _random = new UnityRandomSource();

        public CrossroadsConfig Config => _config;

        /// <summary>True when a crossroads is waiting for the player's choice.</summary>
        public bool HasPending =>
            _save != null && !string.IsNullOrEmpty(_save.pendingCrossroadsId);

        /// <summary>The active card, or null when none is pending.</summary>
        public CrossroadsCard PendingCard
        {
            get
            {
                if (_save == null || _config?.deck == null ||
                    string.IsNullOrEmpty(_save.pendingCrossroadsId)) return null;
                return _config.deck.FirstOrDefault(c => c.id == _save.pendingCrossroadsId);
            }
        }

        private void Awake() => ServiceLocator.Register(this);

        private void OnDestroy()
        {
            UnsubscribeAse();
            ServiceLocator.Unregister(this);
        }

        /// <summary>Test seam — production keeps the default UnityRandomSource.</summary>
        public void SetRandomSource(IRandomSource random) =>
            _random = random ?? throw new ArgumentNullException(nameof(random));

        /// <summary>Called by GameManager after the save is loaded.</summary>
        public void Begin(SaveData save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            if (_save.deeds == null) _save.deeds = new List<DeedData>();
            if (_save.pendingCrossroadsQueue == null) _save.pendingCrossroadsQueue = new List<string>();
            SubscribeAse();
            // Surface any crossroads that survived from a previous session.
            if (HasPending) { OnCrossroadsReady?.Invoke(_save.pendingCrossroadsId); return; }
            // A save loaded mid-life may already be above one or more milestones (offline accrual).
            EvaluateMilestone();
        }

        /// <summary>Checks whether any milestones have been newly crossed and queues
        /// crossroads for each. Called from Begin() and from the AseChanged event.
        /// Tests call this directly after SetAse() to avoid the event-subscription path
        /// (RecalculateRate fires OnRateRecalculated, not OnAseChanged).</summary>
        public void EvaluateMilestone()
        {
            if (_save == null) return;
            CheckMilestones(_save.GetAse());
        }

        /// <summary>
        /// Commits the player's choice for the pending crossroads.
        /// - Ori-aligned choice (option.virtueIndex == save.chosenOri): held++ and trials++
        /// - Any other choice: trials++ only
        /// Records a Deed, dequeues the next crossroads if any, and fires OnCrossroadsResolved.
        /// Returns false when no crossroads is pending or the index is out of range.
        /// </summary>
        public bool MakeChoice(int optionIndex)
        {
            if (_save == null || _config == null || string.IsNullOrEmpty(_save.pendingCrossroadsId))
                return false;

            CrossroadsCard card = PendingCard;
            if (card?.options == null) return false;
            if (optionIndex < 0 || optionIndex >= card.options.Length) return false;

            CrossroadsOption option = card.options[optionIndex];
            bool aligned = option.virtueIndex >= 0 && option.virtueIndex == _save.chosenOri;

            _save.oriTrials++;
            if (aligned) _save.oriHeld++;

            // card came from _config.deck via PendingCard, so IndexOf finds its slot.
            var deed = new DeedData
            {
                crossroadsId = card.id,
                chosenOptionIndex = optionIndex,
                wasOriAligned = aligned,
                beatIndex = Array.IndexOf(_config.deck, card),
                strayed = !aligned,
            };
            _save.deeds.Add(deed);

            // Promote next from queue, if any. Begin() guarantees the queue is non-null.
            string nextId = null;
            if (_save.pendingCrossroadsQueue.Count > 0)
            {
                nextId = _save.pendingCrossroadsQueue[0];
                _save.pendingCrossroadsQueue.RemoveAt(0);
            }
            _save.pendingCrossroadsId = nextId ?? "";

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            OnCrossroadsResolved?.Invoke(deed);
            if (nextId != null) OnCrossroadsReady?.Invoke(nextId);
            return true;
        }

        private void SubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
            {
                aseGen.OnAseChanged -= HandleAseChanged;
                aseGen.OnAseChanged += HandleAseChanged;
            }
        }

        private void UnsubscribeAse()
        {
            if (ServiceLocator.TryGet(out AseGenerationSystem aseGen))
                aseGen.OnAseChanged -= HandleAseChanged;
        }

        private void HandleAseChanged(BigNumber ase) => EvaluateMilestone();

        private void CheckMilestones(BigNumber ase)
        {
            if (_save == null || _config == null || _config.DeckSize == 0) return;

            int crossed = _config.CountMilestonesCrossed(ase);
            int triggered = TriggerCount();

            for (int i = triggered; i < crossed; i++)
                FireCrossroads();
        }

        // Total crossroads triggered this life: active + queued + already resolved.
        // Begin() guarantees both lists are non-null.
        private int TriggerCount()
        {
            int active = string.IsNullOrEmpty(_save.pendingCrossroadsId) ? 0 : 1;
            return active + _save.pendingCrossroadsQueue.Count + _save.deeds.Count;
        }

        private void FireCrossroads()
        {
            // NextDouble() is contractually [0, 1), so the cast is in [0, DeckSize - 1];
            // the Min clamp defends against a test source returning 1.0 exactly.
            int index = (int)(_random.NextDouble() * _config.DeckSize);
            index = Math.Min(_config.DeckSize - 1, index);
            CrossroadsCard card = _config.GetCard(index);
            if (card == null || string.IsNullOrEmpty(card.id)) return;

            // Empty active slot → this card becomes the current one; else it queues
            // and surfaces after the active is resolved. Begin() guarantees the queue is non-null.
            bool becomesActive = string.IsNullOrEmpty(_save.pendingCrossroadsId);
            if (becomesActive)
                _save.pendingCrossroadsId = card.id;
            else
                _save.pendingCrossroadsQueue.Add(card.id);

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            if (becomesActive) OnCrossroadsReady?.Invoke(card.id);
        }
    }
}
