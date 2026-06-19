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
        private enum Phase { Hidden, Confirm, Transition, StormWaves, Silence, Reveal, AncestorCard, Summary, FinalBeat }

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

        private void Awake()
        {
            if (_oddsToggle != null) _oddsToggle.onClick.AddListener(ToggleOdds);
            if (_notYetButton != null) _notYetButton.onClick.AddListener(HideConfirm);
            if (_ceremonyTapCatcher != null) _ceremonyTapCatcher.onClick.AddListener(HandleCeremonyTap);
            if (_continueButton != null) _continueButton.onClick.AddListener(AdvanceFromSummary);
            HideAllRoots();
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

            // Outcome table computed from the live config — never static strings
            // (Osun's ×2 must show; GAMEPLAY adjudication #6).
            double w = ServiceLocator.TryGet(out AncestralCouncilSystem council) ? council.W : 0.25;
            double mod = ServiceLocator.TryGet(out CultivationSystem cultivation)
                ? cultivation.CouncilBonusModifier : 1.0;

            if (_ascendLine != null)
                _ascendLine.text = "Ascend — radiant Ancestor, " + BonusCopy(w * 1.0, mod);
            if (_fallLine != null)
                _fallLine.text = "Fall — ember Ancestor, " + BonusCopy(w * 0.4, mod);

            // ADR-0004: the chance is foregrounded (not hidden behind the ?), and
            // it's the SAME value Resolve rolls against (displayed == rolled).
            if (_chanceToAscendText != null && _tribulation != null)
                _chanceToAscendText.text = $"Chance to ascend: {_tribulation.AscendChance:P0}";

            if (_oddsPanel != null) _oddsPanel.SetActive(false);
            _holdTimer = 0f;
            if (_holdFill != null) _holdFill.fillAmount = 0f;
            if (_confirmRoot != null) _confirmRoot.SetActive(true);
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
            if (_confirmRoot != null) _confirmRoot.SetActive(false);
            _phase = Phase.Hidden;
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

            // Skippability: full ceremony the first time each OUTCOME is seen.
            var save = _saveManager?.Current;
            int seenBit = _result.DidAscend ? SeenFlags.AscendCeremony : SeenFlags.FallCeremony;
            _canSkipWaves = save != null && save.HasSeen(seenBit);
            save?.MarkSeen(seenBit); // persists with the next save trigger

            if (_confirmRoot != null) _confirmRoot.SetActive(false);
            if (_ceremonyRoot != null) _ceremonyRoot.SetActive(true);
            SetCeremonyVisuals(flashAlpha: 0f, whiteAlpha: 0f, showReveal: false);
            _timer = 0f;
            _phase = Phase.Transition;
        }

        private void HandleCeremonyTap()
        {
            // Tap during the waves jumps to the held-breath beat — repeat views only.
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
                case Phase.Transition: TickTransition(); break;
                case Phase.StormWaves: TickStormWaves(); break;
                case Phase.Silence: TickSilence(); break;
                case Phase.Reveal: TickTimed(_config.revealSeconds, EnterAncestorCard); break;
                case Phase.AncestorCard: TickTimed(_config.ancestorCardSeconds, EnterSummary); break;
                case Phase.FinalBeat: TickTimed(_config.finalBeatSeconds, Finish); break;
            }
        }

        private void TickHold()
        {
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

            // Each wave: a flash that decays over its interval. Identical for
            // both outcomes by construction — _result is never read here.
            float withinWave = Mathf.Repeat(_timer, _config.stormWaveIntervalSeconds);
            float decay = 1f - withinWave / _config.stormWaveIntervalSeconds;
            SetCeremonyVisuals(flashAlpha: decay * 0.85f, whiteAlpha: 0f, showReveal: false);

            if (_timer >= total) EnterSilence();
        }

        private void EnterSilence()
        {
            _timer = 0f;
            _phase = Phase.Silence;
            SetCeremonyVisuals(flashAlpha: 0f, whiteAlpha: 1f, showReveal: false); // whiteout, no UI, no sound
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
                // Prove the gain is real — the honest inverse of losses-disguised-
                // as-wins (GAMEPLAY §8). Shown on both branches.
                _deltaLine.text =
                    $"Lineage Àṣẹ: ×{_result.LineageFactorBefore:0.00} → ×{_result.LineageFactorAfter:0.00}";
            }
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

        private void EnterSummary()
        {
            _phase = Phase.Summary; // user-paced — peak-end rule: end on the number going up
            if (_cardRoot != null) _cardRoot.SetActive(false);
            if (_summaryRoot != null) _summaryRoot.SetActive(true);

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
                _summaryStats.text = $"{pathName}\nTime on the road: {h}h {m:D2}m\nPeak Àṣẹ: {_result.PeakAse}";
            }

            if (_ratePreview != null)
            {
                _ratePreview.text =
                    $"Stage 1 rate: {_result.OldStage1Rate}/s → {_result.NewStage1Rate}/s";
            }
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
        }
    }
}
