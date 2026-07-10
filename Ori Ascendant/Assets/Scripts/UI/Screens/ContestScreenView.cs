using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// Ọjà clash UI — the three-beat sequence for a pending House contest (issue #43):
    ///   Challenge  — player reads the House, picks a stance, holds to commit or declines.
    ///   Reveal     — outcome shown with gold (won) or ember (lost) palette; auto-advances.
    ///   Summary    — standing line from MarketplaceStandingPresenter; continue to close.
    ///
    /// The outcome is resolved by MarketplaceSystem.ChooseStance() when the hold completes —
    /// this screen only replays the result (same honest-design principle as TribulationScreen).
    /// Declining hides without resolving; the House waits patiently across sessions.
    /// </summary>
    public class ContestScreenView : MonoBehaviour
    {
        public event Action OnScreenClosed;

        private enum Phase { Hidden, Challenge, ClosingChallenge, Reveal, Summary }

        [SerializeField] private ContestConfig _config;

        [Header("Challenge")]
        [SerializeField] private GameObject _challengeRoot;
        [SerializeField] private TMP_Text _houseNameText;
        [SerializeField] private TMP_Text _housePathText;
        [SerializeField] private TMP_Text _housePowerText;
        [SerializeField] private TMP_Text _strikeOddsText;
        [SerializeField] private TMP_Text _endureOddsText;
        [SerializeField] private TMP_Text _flowOddsText;
        [SerializeField] private Button _strikeButton;
        [SerializeField] private Button _endureButton;
        [SerializeField] private Button _flowButton;
        [SerializeField] private HoldButton _holdButton;
        [SerializeField] private Image _holdFill;
        [SerializeField] private Button _declineButton;

        [Header("Reveal")]
        [SerializeField] private GameObject _revealRoot;
        [SerializeField] private TMP_Text _revealTitle;
        [SerializeField] private TMP_Text _renownDeltaText;

        [Header("Summary")]
        [SerializeField] private GameObject _summaryRoot;
        [SerializeField] private TMP_Text _standingText;
        [SerializeField] private Button _continueButton;

        private Phase _phase = Phase.Hidden;
        private float _timer;
        private float _holdTimer;
        private Stance _selectedStance = Stance.Strike;
        private ContestOutcome _outcome;

        private CanvasGroup _challengeCanvasGroup;
        private OverlayTransition _challengeTransition;
        private CanvasGroup _revealCanvasGroup;
        private OverlayTransition _revealTransition;
        private CanvasGroup _summaryCanvasGroup;
        private OverlayTransition _summaryTransition;

        // Win-title pop (Reveal): brief 1.0→1.08→1.0 scale arch as VICTORY lands.
        // Loss gets no pop — the fade alone (matches the honest "stood firm" copy).
        private const float RevealPopSeconds = 0.35f;
        private const float RevealPopAmplitude = 0.08f;

        private MarketplaceSystem _marketplace;

        // ---- MonoBehaviour lifecycle ----

        private void Awake()
        {
            if (_strikeButton != null) _strikeButton.onClick.AddListener(() => SelectStance(Stance.Strike));
            if (_endureButton != null) _endureButton.onClick.AddListener(() => SelectStance(Stance.Endure));
            if (_flowButton != null) _flowButton.onClick.AddListener(() => SelectStance(Stance.Flow));
            if (_declineButton != null) _declineButton.onClick.AddListener(Decline);
            if (_continueButton != null) _continueButton.onClick.AddListener(Close);
            if (_challengeRoot != null)
                _challengeCanvasGroup = _challengeRoot.GetComponent<CanvasGroup>()
                    ?? _challengeRoot.AddComponent<CanvasGroup>();
            if (_revealRoot != null)
                _revealCanvasGroup = _revealRoot.GetComponent<CanvasGroup>()
                    ?? _revealRoot.AddComponent<CanvasGroup>();
            if (_summaryRoot != null)
                _summaryCanvasGroup = _summaryRoot.GetComponent<CanvasGroup>()
                    ?? _summaryRoot.AddComponent<CanvasGroup>();
            HideAllRoots();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_strikeButton != null) _strikeButton.onClick.RemoveAllListeners();
            if (_endureButton != null) _endureButton.onClick.RemoveAllListeners();
            if (_flowButton != null) _flowButton.onClick.RemoveAllListeners();
            if (_declineButton != null) _declineButton.onClick.RemoveListener(Decline);
            if (_continueButton != null) _continueButton.onClick.RemoveListener(Close);

            if (_marketplace != null)
                _marketplace.OnContestResolved -= HandleContestResolved;

            ServiceLocator.Unregister(this);
        }

        private void HideAllRoots()
        {
            if (_challengeRoot != null) _challengeRoot.SetActive(false);
            if (_revealRoot != null) _revealRoot.SetActive(false);
            if (_summaryRoot != null) _summaryRoot.SetActive(false);
        }

        // ---- Update phase machine ----

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Challenge: TickChallenge(); break;
                case Phase.ClosingChallenge: TickClosingChallenge(); break;
                case Phase.Reveal: TickReveal(); break;
                case Phase.Summary: TickSummary(); break;
            }
        }

        // ---- Show (called by MainScreenController when HasPending) ----

        public void Show()
        {
            if (_phase != Phase.Hidden) return;

            _marketplace = ServiceLocator.Get<MarketplaceSystem>();
            if (_marketplace == null) return;

            // Unsubscribe then re-subscribe (defensive — no double-subscription).
            _marketplace.OnContestResolved -= HandleContestResolved;
            _marketplace.OnContestResolved += HandleContestResolved;

            var pending = _marketplace.Pending;
            if (pending == null) return;

            PopulateChallenge(pending);
            _selectedStance = Stance.Strike; // default to Strike so hold button always has a valid stance
            _holdTimer = 0f;
            if (_holdFill != null) _holdFill.fillAmount = 0f;
            ApplyStanceSelection();

            if (_challengeRoot != null) _challengeRoot.SetActive(true);
            if (_challengeCanvasGroup != null) _challengeCanvasGroup.alpha = 0f;
            _challengeTransition.Open();
            _phase = Phase.Challenge;
        }

        public bool IsOpen => _phase != Phase.Hidden;

        // ---- Challenge phase ----

        private void PopulateChallenge(PendingContest pending)
        {
            if (_houseNameText != null)
                _houseNameText.text = "House of " + pending.houseName;

            if (_housePathText != null)
                _housePathText.text = PathMotif.LongNameOf(pending.housePath);

            if (_housePowerText != null)
                _housePowerText.text = PowerTierLabel(pending.housePowerRatio);

            // Preview per-stance odds.
            if (_strikeOddsText != null)
                _strikeOddsText.text = $"Strike — {_marketplace.PreviewOdds(Stance.Strike):P0}";
            if (_endureOddsText != null)
                _endureOddsText.text = $"Endure — {_marketplace.PreviewOdds(Stance.Endure):P0}";
            if (_flowOddsText != null)
                _flowOddsText.text = $"Flow — {_marketplace.PreviewOdds(Stance.Flow):P0}";
        }

        private void SelectStance(Stance stance)
        {
            _selectedStance = stance;
            ApplyStanceSelection();
        }

        private void ApplyStanceSelection()
        {
            SetButtonHighlight(_strikeButton, _selectedStance == Stance.Strike);
            SetButtonHighlight(_endureButton, _selectedStance == Stance.Endure);
            SetButtonHighlight(_flowButton, _selectedStance == Stance.Flow);
        }

        private static void SetButtonHighlight(Button btn, bool selected)
        {
            if (btn == null) return;
            var image = btn.GetComponent<Image>();
            if (image == null) return;
            // Selected = full gold tint; not selected = dimmed panel color.
            image.color = selected ? PathMotif.Radiance : Palette.IndigoLift;
        }

        private void TickChallenge()
        {
            Transform rootT = _challengeRoot != null ? _challengeRoot.transform : null;
            _challengeTransition.TickAndApply(_challengeCanvasGroup, rootT,
                Time.unscaledDeltaTime, MotionHelper.IsReduceMotion());

            if (_holdButton == null) return;

            _holdTimer = _holdButton.IsHeld
                ? _holdTimer + Time.unscaledDeltaTime
                : 0f;

            float holdDuration = _config != null && _config.holdToConfirmSeconds > 0
                ? (float)_config.holdToConfirmSeconds
                : 0.8f;

            if (_holdFill != null)
                _holdFill.fillAmount = Mathf.Clamp01(_holdTimer / holdDuration);

            if (_holdTimer >= holdDuration)
                BeginResolve();
        }

        private void BeginResolve()
        {
            // ChooseStance resolves and fires OnContestResolved (we handle it below).
            // We move to Reveal only once the event arrives to guard against re-entry.
            if (_marketplace == null) return;
            if (_challengeRoot != null) _challengeRoot.SetActive(false);
            _marketplace.ChooseStance(_selectedStance);
            // HandleContestResolved sets _outcome and transitions to Reveal.
        }

        private void HandleContestResolved(ContestOutcome outcome)
        {
            _outcome = outcome;
            EnterReveal();
        }

        private void Decline()
        {
            _challengeTransition.Close();
            _phase = Phase.ClosingChallenge;
        }

        private void TickClosingChallenge()
        {
            Transform rootT = _challengeRoot != null ? _challengeRoot.transform : null;
            if (_challengeTransition.TickAndApply(_challengeCanvasGroup, rootT,
                    Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
            {
                if (_challengeRoot != null) _challengeRoot.SetActive(false);
                _phase = Phase.Hidden;
            }
        }

        // ---- Reveal phase ----

        private void EnterReveal()
        {
            _timer = 0f;
            _phase = Phase.Reveal;

            if (_challengeRoot != null) _challengeRoot.SetActive(false);
            if (_revealRoot != null) _revealRoot.SetActive(true);
            if (_revealCanvasGroup != null) _revealCanvasGroup.alpha = 0f;
            _revealTransition.Open();

            if (_revealTitle != null)
            {
                _revealTitle.text = RevealTitle(_outcome.Won);
                _revealTitle.color = _outcome.Won ? PathMotif.Radiance : PathMotif.Ember;
                _revealTitle.transform.localScale = Vector3.one; // win pop re-drives this in TickReveal
            }

            if (_renownDeltaText != null)
            {
                double delta = _outcome.RenownDelta;
                string sign = delta >= 0 ? "+" : "";
                _renownDeltaText.text = $"{sign}{delta:0.00} renown";
                _renownDeltaText.color = _outcome.Won ? PathMotif.Radiance : PathMotif.Ember;
            }
        }

        private void TickReveal()
        {
            bool reduceMotion = MotionHelper.IsReduceMotion();
            Transform rootT = _revealRoot != null ? _revealRoot.transform : null;
            _revealTransition.TickAndApply(_revealCanvasGroup, rootT,
                Time.unscaledDeltaTime, reduceMotion);

            _timer += Time.unscaledDeltaTime;

            // Celebratory pop on a win only; TapPulseScale settles back to 1.0 after
            // RevealPopSeconds and holds steady under Reduce Motion.
            if (_outcome.Won && _revealTitle != null)
            {
                float pop = MotionHelper.TapPulseScale(_timer, RevealPopSeconds, RevealPopAmplitude, reduceMotion);
                _revealTitle.transform.localScale = new Vector3(pop, pop, 1f);
            }

            float revealSeconds = _config != null && _config.revealSeconds > 0.0
                ? (float)_config.revealSeconds
                : 2.0f;
            if (_timer >= revealSeconds)
                EnterSummary();
        }

        // ---- Summary phase ----

        private void EnterSummary()
        {
            _phase = Phase.Summary;
            if (_revealRoot != null) _revealRoot.SetActive(false);
            if (_summaryRoot != null) _summaryRoot.SetActive(true);
            if (_summaryCanvasGroup != null) _summaryCanvasGroup.alpha = 0f;
            _summaryTransition.Open();

            if (_standingText != null)
            {
                double currentRenown = 0.0;
                if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current != null)
                    currentRenown = saveManager.Current.lineage.renown;

                _standingText.text = MarketplaceStandingPresenter.Map(currentRenown).Line;
            }
        }

        private void TickSummary()
        {
            // Fade-in only — Summary has no auto-advance; it waits for Continue.
            Transform rootT = _summaryRoot != null ? _summaryRoot.transform : null;
            _summaryTransition.TickAndApply(_summaryCanvasGroup, rootT,
                Time.unscaledDeltaTime, MotionHelper.IsReduceMotion());
        }

        // ---- Close ----

        private void Close()
        {
            HideAllRoots();
            _phase = Phase.Hidden;
            if (_marketplace != null)
                _marketplace.OnContestResolved -= HandleContestResolved;
            OnScreenClosed?.Invoke();
        }

        // ---- Pure helpers (internal static — testable without a scene) ----

        /// <summary>Human-readable power tier label for the rival House.</summary>
        public static string PowerTierLabel(double powerRatio)
        {
            if (powerRatio < 0.85) return "Weaker rival";
            if (powerRatio > 1.15) return "Stronger rival";
            return "Evenly matched";
        }

        /// <summary>Reveal title copy — VICTORY on win, THE HOUSE STOOD FIRM on loss.
        /// Never "defeat", never "loss" as a word (honest-design, ART_BIBLE §7).</summary>
        public static string RevealTitle(bool won) =>
            won ? "VICTORY" : "THE HOUSE STOOD FIRM";
    }
}
