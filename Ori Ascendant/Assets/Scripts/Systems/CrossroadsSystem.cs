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
    /// Crossroads — virtue-testing dilemma events (Dynasty PRD Phase 1, slice 2a).
    /// Owns SaveData.pendingCrossroadsId and SaveData.deeds writes.
    ///
    /// Milestone: a single Àṣẹ threshold defined in CrossroadsConfig. When first
    /// crossed this life, a card is drawn at random from the deck and set pending.
    /// The event is patient — it waits indefinitely for a player choice.
    ///
    /// Choice: MakeChoice(optionIndex) records a Deed, updates the steadfastness
    /// tally (oriHeld / oriTrials in SaveData), fires OnCrossroadsResolved,
    /// and saves. oriHeld increments only when the chosen option's virtueIndex
    /// matches the life's chosenOri; oriTrials always increments.
    /// </summary>
    public class CrossroadsSystem : MonoBehaviour
    {
        [SerializeField] private CrossroadsConfig _config;

        /// <summary>Raised when a crossroads becomes active (card id). Fires on
        /// Begin() if a pending crossroads survived from a previous session.</summary>
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
            SubscribeAse();
            // Surface any crossroads that survived from a previous session.
            if (HasPending) { OnCrossroadsReady?.Invoke(_save.pendingCrossroadsId); return; }
            // A save loaded mid-life may already be above the milestone (offline accrual).
            EvaluateMilestone();
        }

        /// <summary>Checks whether the milestone has been crossed and fires a
        /// crossroads if so. Called from Begin() and from the AseChanged event.
        /// Tests call this directly after setting save.SetAse() to avoid the
        /// event-subscription path (RecalculateRate fires OnRateRecalculated,
        /// not OnAseChanged).</summary>
        public void EvaluateMilestone()
        {
            if (_save == null) return;
            CheckMilestone(_save.GetAse());
        }

        /// <summary>
        /// Commits the player's choice for the pending crossroads.
        /// - Ori-aligned choice (option.virtueIndex == save.chosenOri): held++ and trials++
        /// - Any other choice: trials++ only
        /// Records a Deed and fires OnCrossroadsResolved.
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

            // Resolve the card's deck position for Remembrance (CrossroadsDeckConfig.beats 1:1 with deck).
            int beatIndex = -1;
            if (_config.deck != null)
            {
                for (int i = 0; i < _config.deck.Length; i++)
                {
                    if (_config.deck[i]?.id == card.id) { beatIndex = i; break; }
                }
            }

            var deed = new DeedData
            {
                crossroadsId = card.id,
                chosenOptionIndex = optionIndex,
                wasOriAligned = aligned,
                strayed = !aligned,
                beatIndex = beatIndex,
            };
            _save.deeds.Add(deed);
            _save.pendingCrossroadsId = "";

            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            OnCrossroadsResolved?.Invoke(deed);
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

        private void CheckMilestone(BigNumber ase)
        {
            if (_save == null || _config == null || _config.DeckSize == 0) return;
            if (HasPending) return;                        // already waiting for a choice
            if (_save.deeds != null && _save.deeds.Count > 0) return; // fired this life already
            if (ase < _config.GetMilestone()) return;

            FireCrossroads();
        }

        private void FireCrossroads()
        {
            // Caller (CheckMilestone) already verified DeckSize > 0.
            // NextDouble() is contractually [0, 1), so the cast is in [0, DeckSize - 1];
            // the Min clamp defends against a test source returning 1.0 exactly.
            int index = (int)(_random.NextDouble() * _config.DeckSize);
            index = Math.Min(_config.DeckSize - 1, index);
            CrossroadsCard card = _config.GetCard(index);
            if (card == null || string.IsNullOrEmpty(card.id)) return;

            _save.pendingCrossroadsId = card.id;
            if (ServiceLocator.TryGet(out SaveManager saveManager)) saveManager.Save();
            OnCrossroadsReady?.Invoke(card.id);
        }
    }
}
