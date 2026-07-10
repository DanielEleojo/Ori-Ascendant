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
    /// Ìrékọjá — the Crossing (GAMEPLAY §3.5). Pure theater: the outcome is
    /// rolled and PERSISTED by TribulationSystem.Resolve() before the first
    /// ceremony frame; this screen replays the returned result. Beats are
    /// visually identical for both outcomes until the reveal — no staged RNG,
    /// no near-misses, ever (§8 honest-design commitments). All timings come
    /// from TribulationConfig (no magic numbers).
    /// </summary>
    public class TribulationScreen : MonoBehaviour
    {
        /// <summary>Fires in Finish() after HideAllRoots() — the overlay is fully down.
        /// MainScreenSkin subscribes to start the star-ignition ceremony at this moment
        /// rather than when OnTribulationComplete fires (which is while the overlay is still up).</summary>
        public event Action OnCeremonyClosed;

        private enum Phase { Hidden, Confirm, ClosingConfirm, Transition, StormWaves, Silence, Reveal, AncestorCard, Summary, FinalBeat }

        [SerializeField] private TribulationConfig _config;

        [Header("Confirm sheet")]
        [SerializeField] private GameObject _confirmRoot;
        [SerializeField] private TMP_Text _chanceToAscendText;
        [SerializeField] private TMP_Text _ascendLine;
        [SerializeField] private TMP_Text _fallLine;
        [SerializeField] private GameObject _oddsPanel;
        [SerializeField] private Button _oddsToggle;
        [SerializeField] private Button _notYetButton;
        [SerializeField] private HoldButton _holdButton;
        [SerializeField] private Image _holdFill;

        [Header("Ceremony")]
        [SerializeField] private GameObject _ceremonyRoot;
        [SerializeField] private Image _flash;
        [SerializeField] private Image _whiteout;
        [SerializeField] private TMP_Text _revealTitle;
        [SerializeField] private TMP_Text _revealSubtitle;
        [SerializeField] private TMP_Text _deltaLine;
        [SerializeField] private Button _ceremonyTapCatcher;
        [SerializeField] private Image _victoryPortrait;
        [SerializeField] private Image _ascensionFxOverlay;

        [Header("Ancestor card")]
        [SerializeField] private GameObject _cardRoot;
        [SerializeField] private Image _cardFrame;
        [SerializeField] private Image _cardMotif;
        [SerializeField] private TMP_Text _cardTitle;
        [SerializeField] private TMP_Text _cardBonus;
        [SerializeField] private TMP_Text _cardRetireLine;

        [Header("Generation summary")]
        [SerializeField] private GameObject _summaryRoot;
        [SerializeField] private TMP_Text _summaryTitle;
        [SerializeField] private TMP_Text _summaryStats;
        [SerializeField] private TMP_Text _ratePreview;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _finalRoot;
        [SerializeField] private TMP_Text _finalText;

        private Phase _phase = Phase.Hidden;
        private float _timer;
        private float _holdTimer;
        private bool _canSkipWaves;
        private TribulationResult _result;
        private TribulationSystem _tribulation;
        private SaveManager _saveManager;

        private CanvasGroup _confirmCanvasGroup;
        private OverlayTransition _confirmTransition;
        private CanvasGroup _cardCanvasGroup;
        private OverlayTransition _cardTransition;
        private CanvasGroup _summaryCanvasGroup;
        private OverlayTransition _summaryTransition;

        private void Awake()
        {
            if (_oddsToggle != null) _oddsToggle.onClick.AddListener(ToggleOdds);
            if (_notYetButton != null) _notYetButton.onClick.AddListener(HideConfirm);
            if (_ceremonyTapCatcher != null) _ceremonyTapCatcher.onClick.AddListener(HandleCeremonyTap);
            if (_continueButton != null) _continueButton.onClick.AddListener(AdvanceFromSummary);
            if (_confirmRoot != null)
                _confirmCanvasGroup = _confirmRoot.GetComponent<CanvasGroup>() ?? _confirmRoot.AddComponent<CanvasGroup>();
            if (_cardRoot != null)
                _cardCanvasGroup = _cardRoot.GetComponent<CanvasGroup>() ?? _cardRoot.AddComponent<CanvasGroup>();
            if (_summaryRoot != null)
                _summaryCanvasGroup = _summaryRoot.GetComponent<CanvasGroup>() ?? _summaryRoot.AddComponent<CanvasGroup>();
            HideAllRoots();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (_oddsToggle != null) _oddsToggle.onClick.RemoveListener(ToggleOdds);
            if (_notYetButton != null) _notYetButton.onClick.RemoveListener(HideConfirm);
            if (_ceremonyTapCatcher != null) _ceremonyTapCatcher.onClick.RemoveListener(HandleCeremonyTap);
            if (_continueButton != null) _continueButton.onClick.RemoveListener(AdvanceFromSummary);
        }

        private void HideAllRoots()
        {
            if (_confirmRoot != null) _confirmRoot.SetActive(false);
            if (_ceremonyRoot != null) _ceremonyRoot.SetActive(false);
            if (_cardRoot != null) _cardRoot.SetActive(false);
            if (_summaryRoot != null) _summaryRoot.SetActive(false);
            if (_finalRoot != null) _finalRoot.SetActive(false);
        }

        // ---- confirm sheet (player-triggered, voluntary commitment) ----

        public void ShowConfirm()
        {
            _tribulation = ServiceLocator.Get<TribulationSystem>();
            ServiceLocator.TryGet(out _saveManager);

            double w = ServiceLocator.TryGet(out AncestralCouncilSystem council) ? council.W : 0.25;
            double mod = ServiceLocator.TryGet(out CultivationSystem cultivation)
                ? cultivation.CouncilBonusModifier : 1.0;

            if (_ascendLine != null)
                _ascendLine.text = "Ascend — radiant Ancestor, " + BonusCopy(w * 1.0, mod);
            if (_fallLine != null)
                _fallLine.text = "Fall — ember Ancestor, " + BonusCopy(w * 0.4, mod);

            if (_chanceToAscendText != null && _tribulation != null)
                _chanceToAscendText.text = $"Chance to ascend: {_tribulation.AscendChance:P0}";

            if (_oddsPanel != null) _oddsPanel.SetActive(false);
            _holdTimer = 0f;
            if (_holdFill != null) _holdFill.fillAmount = 0f;
            if (_confirmRoot != null) _confirmRoot.SetActive(true);
            if (_confirmCanvasGroup != null) _confirmCanvasGroup.alpha = 0f;
            _confirmTransition.Open();
            _phase = Phase.Confirm;
        }

        private static string BonusCopy(double basePortion, double councilModifier)
        {
            string copy = $"+{basePortion:P0} lineage Àṣẹ";
            if (councilModifier > 1.0)
            {
                copy += $" (×{councilModifier:0.#} while this Path flows: +{basePortion * councilModifier:P0})";
            }
            return copy;
        }

        private void ToggleOdds()
        {
            if (_oddsPanel != null) _oddsPanel.SetActive(!_oddsPanel.activeSelf);
        }

        private void HideConfirm()
        {
            _confirmTransition.Close();
            _phase = Phase.ClosingConfirm;
        }

        // ---- the crossing ----

        private void BeginCrossing()
        {
            _result = _tribulation != null ? _tribulation.Resolve() : null;
            if (_result == null)
            {
                HideConfirm(); // ineligible (stale UI) — never fake a ceremony
                return;
            }

            var save = _saveManager?.Current;
            int seenBit = _result.DidAscend ? SeenFlags.AscendCeremony : SeenFlags.FallCeremony;
            _canSkipWaves = save != null && save.HasSeen(seenBit);
            save?.MarkSeen(seenBit);

            if (_confirmRoot != null) _confirmRoot.SetActive(false);
            if (_ceremonyRoot != null) _ceremonyRoot.SetActive(true);
            SetCeremonyVisuals(flashAlpha: 0f, whiteAlpha: 0f, showReveal: false);
            _timer = 0f;
            _phase = Phase.Transition;
        }

        private void HandleCeremonyTap()
        {
            if (_canSkipWaves && (_phase == Phase.Transition || _phase == Phase.StormWaves))
            {
                EnterSilence();
            }
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Confirm: TickHold(); break;
                case Phase.ClosingConfirm: TickClosingConfirm(); break;
                case Phase.Transition: TickTransition(); break;
                case Phase.StormWaves: TickStormWaves(); break;
                case Phase.Silence: TickSilence(); break;
                case Phase.Reveal: TickReveal(); break;
                case Phase.AncestorCard: TickAncestorCard(); break;
                case Phase.Summary: TickSummary(); break;
                case Phase.FinalBeat: TickTimed(_config.finalBeatSeconds, Finish); break;
            }
        }

        private void TickHold()
        {
            Transform rootT = _confirmRoot != null ? _confirmRoot.transform : null;
            _confirmTransition.TickAndApply(_confirmCanvasGroup, rootT,
                Time.unscaledDeltaTime, MotionHelper.IsReduceMotion());

            if (_holdButton == null || _config == null) return;

            _holdTimer = _holdButton.IsHeld
                ? _holdTimer + Time.unscaledDeltaTime
                : 0f;
            if (_holdFill != null)
            {
                _holdFill.fillAmount = Mathf.Clamp01(_holdTimer / (float)_config.holdToConfirmSeconds);
            }
            if (_holdTimer >= (float)_config.holdToConfirmSeconds)
            {
                BeginCrossing();
            }
        }

        private void TickClosingConfirm()
        {
            Transform rootT = _confirmRoot != null ? _confirmRoot.transform : null;
            if (_confirmTransition.TickAndApply(_confirmCanvasGroup, rootT,
                    Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
            {
                if (_confirmRoot != null) _confirmRoot.SetActive(false);
                _phase = Phase.Hidden;
            }
        }

        private void TickTransition()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= _config.transitionSeconds)
            {
                _timer = 0f;
                _phase = Phase.StormWaves;
            }
        }

        private void TickStormWaves()
        {
            _timer += Time.unscaledDeltaTime;
            float total = _config.stormWaveCount * _config.stormWaveIntervalSeconds;

            float withinWave = Mathf.Repeat(_timer, _config.stormWaveIntervalSeconds);
            float decay = MotionHelper.IsReduceMotion()
                ? 0f // Reduce Motion: no wave flashes — flash stays at base; phase timing unchanged
                : 1f - withinWave / _config.stormWaveIntervalSeconds;
            SetCeremonyVisuals(flashAlpha: decay * 0.85f, whiteAlpha: 0f, showReveal: false);

            if (_timer >= total) EnterSilence();
        }

        private void EnterSilence()
        {
            _timer = 0f;
            _phase = Phase.Silence;
            SetCeremonyVisuals(flashAlpha: 0f, whiteAlpha: 1f, showReveal: false);
        }

        private void TickSilence()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= _config.silenceHoldSeconds)
            {
                _timer = 0f;
                _phase = Phase.Reveal;
                ShowReveal();
            }
        }

        private void TickReveal()
        {
            _timer += Time.unscaledDeltaTime;
            TickAscensionFx();
            if (_timer >= _config.revealSeconds)
            {
                _timer = 0f;
                EnterAncestorCard();
            }
        }

        private void TickAscensionFx()
        {
            if (_ascensionFxOverlay == null || _result == null || !_result.DidAscend) return;
            float pulse = MotionHelper.IsReduceMotion()
                ? 0.4f // Reduce Motion: hold the glow at the pulse's centre — steady, no pulsing
                : 0.4f + 0.3f * Mathf.Sin(_timer * Mathf.PI * MotionScale.AscensionPulseFrequency);
            var c = _ascensionFxOverlay.color;
            c.a = pulse;
            _ascensionFxOverlay.color = c;
        }

        private void ShowReveal()
        {
            bool ascended = _result.DidAscend;
            SetCeremonyVisuals(flashAlpha: 0f, whiteAlpha: 0f, showReveal: true);

            if (_revealTitle != null)
            {
                _revealTitle.text = ascended ? "ASCENDED" : "THE LINE ENDURES";
                _revealTitle.color = ascended ? PathMotif.Radiance : PathMotif.Ember;
            }
            if (_revealSubtitle != null)
            {
                _revealSubtitle.text = ascended
                    ? "A good crossing — seated among the ancestors in full radiance."
                    : "A fallen cultivator still watches over their blood.";
            }
            if (_deltaLine != null)
            {
                _deltaLine.text =
                    $"Lineage Àṣẹ: ×{_result.LineageFactorBefore:0.00} → ×{_result.LineageFactorAfter:0.00}";
            }

            if (_victoryPortrait != null)
            {
                ServiceLocator.TryGet(out CultivationSystem cultivation);
                Sprite stage6Portrait = cultivation?.CurrentStageConfig?.portrait;
                _victoryPortrait.sprite = _config?.RevealSprite(ascended, stage6Portrait);
                _victoryPortrait.gameObject.SetActive(true);
            }

            if (_ascensionFxOverlay != null)
                _ascensionFxOverlay.gameObject.SetActive(ascended);
        }

        private void TickTimed(float duration, System.Action next)
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer >= duration)
            {
                _timer = 0f;
                next();
            }
        }

        private void EnterAncestorCard()
        {
            _phase = Phase.AncestorCard;
            if (_ceremonyRoot != null) _ceremonyRoot.SetActive(false);
            if (_cardRoot != null) _cardRoot.SetActive(true);
            if (_cardCanvasGroup != null) _cardCanvasGroup.alpha = 0f;
            _cardTransition.Open();

            var ancestor = _result.Ancestor;
            if (_cardFrame != null)
                _cardFrame.color = ancestor.didAscend ? PathMotif.Radiance : PathMotif.Ember;
            if (_cardMotif != null)
                _cardMotif.color = PathMotif.AncestorTint(ancestor.path, ancestor.didAscend);
            if (_cardTitle != null)
                _cardTitle.text = $"Gen {_result.CompletedGenerationNumber} — Aṣẹ́gun of {PathMotif.TitleOf(ancestor.path)}";

            double w = ServiceLocator.TryGet(out AncestralCouncilSystem council) ? council.W : 0.25;
            if (_cardBonus != null)
                _cardBonus.text = $"+{w * ancestor.bonusMultiplier:P0} lineage Àṣẹ";

            if (_cardRetireLine != null)
            {
                bool retired = _result.RetiredAncestor != null;
                _cardRetireLine.gameObject.SetActive(retired);
                if (retired)
                {
                    _cardRetireLine.text =
                        $"An elder settles into the foundation of the house " +
                        $"(+{w * _result.RetiredAncestor.bonusMultiplier:P0} permanent)";
                }
            }
        }

        private void TickAncestorCard()
        {
            // Fade runs alongside the beat's timer — it never gates the phase duration.
            Transform rootT = _cardRoot != null ? _cardRoot.transform : null;
            _cardTransition.TickAndApply(_cardCanvasGroup, rootT,
                Time.unscaledDeltaTime, MotionHelper.IsReduceMotion());
            TickTimed(_config.ancestorCardSeconds, EnterSummary);
        }

        private void EnterSummary()
        {
            _phase = Phase.Summary;
            if (_cardRoot != null) _cardRoot.SetActive(false);
            if (_summaryRoot != null) _summaryRoot.SetActive(true);
            if (_summaryCanvasGroup != null) _summaryCanvasGroup.alpha = 0f;
            _summaryTransition.Open();

            if (_summaryTitle != null)
                _summaryTitle.text = $"Generation {_result.CompletedGenerationNumber} complete";

            if (_summaryStats != null)
            {
                string pathName = ServiceLocator.TryGet(out CultivationSystem cultivation) &&
                                  _result.PathIndexAtCrossing >= 0 &&
                                  _result.PathIndexAtCrossing < cultivation.Paths.Length
                    ? cultivation.Paths[_result.PathIndexAtCrossing].pathName
                    : "No path walked";
                long h = _result.TimeInGenerationSeconds / 3600;
                long m = (_result.TimeInGenerationSeconds % 3600) / 60;
                _summaryStats.text = $"{pathName}\nTime on the road: {h}h {m:D2}m\nPeak Àṣẹ: {_result.PeakAse}\nRenown gained: +{_result.RenownGranted:0.00}";
            }

            if (_ratePreview != null)
            {
                _ratePreview.text =
                    $"Stage 1 rate: {_result.OldStage1Rate} → {_result.NewStage1Rate} Àṣẹ per breath";
            }
        }

        private void TickSummary()
        {
            // Fade-in only — Summary has no auto-advance; it waits for Continue.
            Transform rootT = _summaryRoot != null ? _summaryRoot.transform : null;
            _summaryTransition.TickAndApply(_summaryCanvasGroup, rootT,
                Time.unscaledDeltaTime, MotionHelper.IsReduceMotion());
        }

        private void AdvanceFromSummary()
        {
            if (_phase != Phase.Summary) return;
            if (_summaryRoot != null) _summaryRoot.SetActive(false);
            if (_finalRoot != null) _finalRoot.SetActive(true);
            if (_finalText != null) _finalText.text = "A child of the lineage takes up the path";
            _timer = 0f;
            _phase = Phase.FinalBeat;
        }

        private void Finish()
        {
            HideAllRoots();
            _phase = Phase.Hidden;
            _result = null;
            OnCeremonyClosed?.Invoke();
        }

        private void SetCeremonyVisuals(float flashAlpha, float whiteAlpha, bool showReveal)
        {
            if (_flash != null)
            {
                var c = _flash.color; c.a = flashAlpha; _flash.color = c;
            }
            if (_whiteout != null)
            {
                var c = _whiteout.color; c.a = whiteAlpha; _whiteout.color = c;
            }
            if (_revealTitle != null) _revealTitle.gameObject.SetActive(showReveal);
            if (_revealSubtitle != null) _revealSubtitle.gameObject.SetActive(showReveal);
            if (_deltaLine != null) _deltaLine.gameObject.SetActive(showReveal);

            if (!showReveal)
            {
                if (_victoryPortrait != null) _victoryPortrait.gameObject.SetActive(false);
                if (_ascensionFxOverlay != null) _ascensionFxOverlay.gameObject.SetActive(false);
            }
        }
    }
}
