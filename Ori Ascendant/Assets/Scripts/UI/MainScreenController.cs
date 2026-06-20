using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Interactive MainScreen surfaces (GAMEPLAY §3.2 zones 4–6): the Advance
    /// CTA, the stage/tribulation progress bar, tap-to-channel on the portrait,
    /// and the one-time channel hint. Reads state every frame (cheap struct
    /// compares, strings rebuilt only on change). Writes go through system APIs
    /// (TryAdvance / ChoosePath / ChannelTap) and, for the hint lifetime, directly
    /// to the add-only channelHintShownAt / seenFlags fields on SaveData.
    /// </summary>
    public class MainScreenController : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private TribulationConfig _tribulationConfig;
        [SerializeField] private Image _stormVignette;
        [SerializeField] private TribulationScreen _tribulationScreen;
        [SerializeField] private GameObject _progressRoot;
        [SerializeField] private Image _barFill;
        [SerializeField] private TMP_Text _progressLabel;
        [SerializeField] private GameObject _ctaRoot;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private TMP_Text _advanceLabel;
        [SerializeField] private Button _portraitButton;
        [SerializeField] private RectTransform _floatingTextAnchor;
        [SerializeField] private GameObject _hintRoot;
        [SerializeField] private PathScreenView _pathScreen;
        [SerializeField] private OriScreenView _oriScreen;
        [SerializeField] private Screens.CrossroadsScreenView _crossroadsScreen;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Screens.SettingsScreenView _settingsScreen;

        private static readonly Color ChannelColor = new Color(0.851f, 0.643f, 0.255f); // àṣẹ gold

        private AseGenerationSystem _aseGeneration;
        private CultivationSystem _cultivation;
        private SaveManager _saveManager;
        private OriSystem _oriSystem;
        private Systems.CrossroadsSystem _crossroadsSystem;

        private float _secondsSinceLaunch;
        private string _lastProgressText;
        private string _lastCtaText;

        private const float HintAppearSeconds = 10f;
        private const long HintLifetimeSeconds = 6L;

        private void Awake()
        {
            if (_advanceButton != null) _advanceButton.onClick.AddListener(HandleAdvanceTapped);
            if (_portraitButton != null) _portraitButton.onClick.AddListener(HandlePortraitTapped);
            if (_settingsButton != null && _settingsScreen != null)
                _settingsButton.onClick.AddListener(_settingsScreen.Show);
            if (_hintRoot != null) _hintRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_advanceButton != null) _advanceButton.onClick.RemoveListener(HandleAdvanceTapped);
            if (_portraitButton != null) _portraitButton.onClick.RemoveListener(HandlePortraitTapped);
            if (_settingsButton != null && _settingsScreen != null)
                _settingsButton.onClick.RemoveListener(_settingsScreen.Show);
        }

        private void Start()
        {
            _aseGeneration = ServiceLocator.Get<AseGenerationSystem>();
            _cultivation = ServiceLocator.Get<CultivationSystem>();
            ServiceLocator.TryGet(out _saveManager);
            ServiceLocator.TryGet(out _oriSystem);
            ServiceLocator.TryGet(out _crossroadsSystem);

            if (_progressRoot != null) _progressRoot.SetActive(true);
            if (_ctaRoot != null) _ctaRoot.SetActive(true);
        }

        private void Update()
        {
            if (_cultivation == null || _aseGeneration == null) return;

            RefreshProgress();
            RefreshCta();
            TickHint();
            TickOriPrompt();
            TickCrossroadsPrompt();
        }

        // ---- post-Crossing re-vow: surface the modal on the first frame after
        //      Resolve clears chosenOri (cheap field compare; one-shot per gen).
        private void TickOriPrompt()
        {
            if (_oriScreen == null || _oriSystem == null) return;
            if (_oriSystem.HasChosen || _oriScreen.IsOpen) return;
            _oriScreen.Show();
        }

        // ---- pending crossroads: surface the modal once Àṣẹ passes the milestone
        //      and the crossroads fires. Patient — waits across sessions.
        private void TickCrossroadsPrompt()
        {
            if (_crossroadsScreen == null || _crossroadsSystem == null) return;
            if (!_crossroadsSystem.HasPending || _crossroadsScreen.IsOpen) return;
            _crossroadsScreen.Show();
        }

        // ---- progress bar (zone 5) ----

        private void RefreshProgress()
        {
            var save = _saveManager?.Current;
            if (save == null) return;

            BigNumber ase = _aseGeneration.CurrentAse;
            BigNumber target = _cultivation.CurrentTarget;
            if (target.IsZero) return;

            double ratio = (ase / target).ToDouble();
            float fill = Mathf.Clamp01((float)ratio);
            if (_barFill != null) _barFill.fillAmount = fill;

            string text;
            if (_cultivation.IsAtFinalStage)
            {
                // Tribulation bar: one-decimal percent so a 2-min session always
                // visibly moves (GAMEPLAY §2.4 condition).
                text = $"Ìrékọjá — {System.Math.Min(ratio, 1.0) * 100.0:0.0}%";
            }
            else
            {
                string next = _cultivation.PeekStageName(save.currentStage + 1);
                text = next != null
                    ? $"Next: {next} — {ase} / {target}"
                    : $"{ase} / {target}";
            }

            if (text != _lastProgressText)
            {
                _lastProgressText = text;
                if (_progressLabel != null) _progressLabel.text = text;
            }

            RefreshStormVignette(ratio);
        }

        /// <summary>Ambient escalation stub (GAMEPLAY §3.5 buildup): vignette alpha
        /// steps at the config's ambient fractions while at the final stage. On a
        /// resume, this jumps straight to the current state — intermediate
        /// stingers never replay (§3.4 sequencing rule). Full art is Phase D.</summary>
        private void RefreshStormVignette(double ratio)
        {
            if (_stormVignette == null) return;

            float alpha = 0f;
            if (_cultivation.IsAtFinalStage && _tribulationConfig != null &&
                _tribulationConfig.ambientFractions != null)
            {
                var fractions = _tribulationConfig.ambientFractions;
                if (fractions.Length > 0 && ratio >= fractions[0]) alpha = 0.15f;
                if (fractions.Length > 1 && ratio >= fractions[1]) alpha = 0.35f;
            }

            var c = _stormVignette.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                _stormVignette.color = c;
            }
        }

        // ---- CTA (zone 6) ----

        private void RefreshCta()
        {
            string text;
            bool interactable;

            if (_cultivation.IsAtFinalStage)
            {
                // The CTA morph: armed the moment the 25M gate is met. (The
                // Welcome-Back modal sits above this canvas and blocks input, so
                // collect always resolves before the player can trigger this.)
                text = "Face the Tribulation";
                interactable = _cultivation.IsTribulationEligibleNow;
            }
            else
            {
                text = "Advance";
                interactable = _cultivation.CanAdvance();
            }

            if (_advanceButton != null && _advanceButton.interactable != interactable)
            {
                _advanceButton.interactable = interactable;
            }
            if (text != _lastCtaText)
            {
                _lastCtaText = text;
                if (_advanceLabel != null) _advanceLabel.text = text;
            }
        }

        private void HandleAdvanceTapped()
        {
            if (_cultivation.IsAtFinalStage)
            {
                if (_cultivation.IsTribulationEligibleNow && _tribulationScreen != null)
                {
                    _tribulationScreen.ShowConfirm();
                }
                return;
            }

            switch (_cultivation.TryAdvance())
            {
                case AdvanceOutcome.NeedsPathChoice:
                    if (_pathScreen != null) _pathScreen.Show();
                    break;
                case AdvanceOutcome.Advanced:
                    // The rate-line jump is the feedback; nothing else to do.
                    break;
            }
        }

        // ---- tap-to-channel (zone 4, GAMEPLAY §5.3) ----

        private void HandlePortraitTapped()
        {
            var save = _saveManager?.Current;
            BigNumber before = _aseGeneration.CurrentAse;
            _aseGeneration.ChannelTap();
            BigNumber granted = _aseGeneration.CurrentAse - before;

            if (!granted.IsZero && _floatingTextAnchor != null)
            {
                FloatingText.Spawn(_floatingTextAnchor, new Vector2(0f, 40f), "+" + granted, ChannelColor);
            }

            if (_hintRoot != null && _hintRoot.activeSelf) _hintRoot.SetActive(false);
            if (save != null && !save.HasSeen(SeenFlags.ChannelHint))
            {
                save.MarkSeen(SeenFlags.ChannelHint); // persists with the next save
            }
        }

        // ---- one-time hint (GAMEPLAY §5.3 discoverability) ----

        private void TickHint()
        {
            var save = _saveManager?.Current;
            if (save == null || _hintRoot == null) return;

            // seenFlags.ChannelHint is the "never show again" authority — set on
            // auto-expiry or user-tap. Once set, nothing below can re-show the hint.
            if (save.HasSeen(SeenFlags.ChannelHint))
            {
                if (_hintRoot.activeSelf) _hintRoot.SetActive(false);
                return;
            }

            _secondsSinceLaunch += Time.unscaledDeltaTime;
            long nowUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Write the appear timestamp once, after the appear delay.
            if (save.channelHintShownAt == 0 && _secondsSinceLaunch >= HintAppearSeconds)
                save.channelHintShownAt = nowUtc;

            // Derive visibility from persisted state — survives resume and scene reload.
            var state = ChannelHintDecision.Evaluate(save.channelHintShownAt, nowUtc, HintLifetimeSeconds);
            bool shouldShow = state == ChannelHintState.Active;
            if (_hintRoot.activeSelf != shouldShow) _hintRoot.SetActive(shouldShow);

            // Mark seen on auto-expiry so the hint never reappears.
            if (state == ChannelHintState.Expired)
                save.MarkSeen(SeenFlags.ChannelHint);
        }
    }
}
