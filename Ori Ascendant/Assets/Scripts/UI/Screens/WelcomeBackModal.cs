using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// Welcome Back collect screen v1 (GAMEPLAY §3.4): time away (honest about
    /// the 8h cap), earned amount, rate context, single Collect button.
    /// Subscribes to the static offline event in Awake — all Awakes run before
    /// any Start, so the GameManager.Start application can never be missed.
    /// Earnings are already credited when the event fires; this modal only
    /// presents them (UI never writes game state). Below the welcomeBackMinSeconds
    /// threshold the gain stays silent (no popup spam on quick app switches).
    /// </summary>
    public class WelcomeBackModal : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private GameObject _modalRoot;
        [SerializeField] private TMP_Text _timeAwayText;
        [SerializeField] private TMP_Text _earnedText;
        [SerializeField] private TMP_Text _rateContextText;
        [SerializeField] private TMP_Text _bonusLineText;
        [SerializeField] private Button _collectButton;

        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private void Awake()
        {
            OfflineProgressCalculator.OnOfflineProgressApplied += HandleOfflineProgress;
            if (_collectButton != null) _collectButton.onClick.AddListener(Hide);
            if (_modalRoot != null)
            {
                _modalRoot.SetActive(false);
                _canvasGroup = _modalRoot.GetComponent<CanvasGroup>() ?? _modalRoot.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            OfflineProgressCalculator.OnOfflineProgressApplied -= HandleOfflineProgress;
            if (_collectButton != null) _collectButton.onClick.RemoveListener(Hide);
        }

        private const float CountUpSeconds = 1.2f;
        private BigNumber _earnedTarget;
        private float _countUpElapsed;
        private bool _countingUp;

        private void HandleOfflineProgress(BigNumber earned, long countedSeconds)
        {
            if (_config == null || countedSeconds < _config.welcomeBackMinSeconds) return;
            if (earned.IsZero) return;

            if (_timeAwayText != null) _timeAwayText.text = "Away " + FormatDuration(countedSeconds);
            // Count-up animation (GAMEPLAY §3.4): from 0 to earned, tap-to-skip
            // via Collect. Cosmetic only — the Àṣẹ is already credited.
            _earnedTarget = earned;
            if (MotionHelper.IsReduceMotion())
            {
                // Reduce Motion: land on the full amount at once, no count-up.
                _countingUp = false;
                if (_earnedText != null) _earnedText.text = "+" + earned + " Àṣẹ";
            }
            else
            {
                _countUpElapsed = 0f;
                _countingUp = true;
                if (_earnedText != null) _earnedText.text = "+0 Àṣẹ";
            }
            RefreshBonusLine(earned);
            if (_rateContextText != null)
            {
                // The cached rate the calculation actually used (recalc happens after).
                if (ServiceLocator.TryGet(out AseGenerationSystem gen))
                {
                    _rateContextText.text = "at " + gen.CurrentRate + " Àṣẹ per breath";
                }
                else
                {
                    _rateContextText.text = string.Empty;
                }
            }

            // Fade-scale in via OverlayTransition like every other overlay
            // (alpha-only under Reduce Motion — the struct handles it).
            if (_modalRoot != null) _modalRoot.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        /// <summary>Ane's legibility moment (GAMEPLAY §2.3): itemize the offline
        /// bonus as its own highlighted line. earned already INCLUDES the
        /// modifier, so the bonus portion = earned − earned/modifier; the event
        /// signature stays unchanged.</summary>
        private void RefreshBonusLine(BigNumber earned)
        {
            if (_bonusLineText == null) return;

            string line = null;
            if (ServiceLocator.TryGet(out CultivationSystem cultivation))
            {
                var path = cultivation.CurrentPathConfig;
                double modifier = cultivation.PathOfflineRateModifier;
                if (path != null && modifier > 1.0 && !string.IsNullOrEmpty(path.offlineBonusLabel))
                {
                    BigNumber bonus = earned - earned * (1.0 / modifier);
                    line = $"{path.offlineBonusLabel} ×{modifier:0.#}: +{bonus}";
                }
            }

            bool show = line != null;
            if (_bonusLineText.gameObject.activeSelf != show) _bonusLineText.gameObject.SetActive(show);
            if (show) _bonusLineText.text = line;
        }

        private void Update()
        {
            if (_modalRoot == null || !_modalRoot.activeSelf) return;
            if (_transition.TickAndApply(_canvasGroup, _modalRoot.transform, Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
            {
                _modalRoot.SetActive(false);
                return;
            }

            if (!_countingUp || _earnedText == null) return;

            _countUpElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_countUpElapsed / CountUpSeconds);
            BigNumber shown = t >= 1f ? _earnedTarget : _earnedTarget * (double)t;
            _earnedText.text = "+" + shown + " Àṣẹ";
            if (t >= 1f) _countingUp = false;
        }

        private void Hide()
        {
            // Tap-to-skip: collecting mid-count snaps the number to full first.
            if (_countingUp && _earnedText != null)
            {
                _earnedText.text = "+" + _earnedTarget + " Àṣẹ";
                _countingUp = false;
            }
            // Fade-scale out; Update deactivates the root once fully closed.
            _transition.Close();
        }

        /// <summary>"6h 12m" / "4m 03s"; exactly at the cap: "8h (cap)" — honesty
        /// about the cap is a design commitment (GAMEPLAY §8).</summary>
        private static string FormatDuration(long seconds)
        {
            if (seconds >= OfflineProgressCalculator.MaxOfflineSeconds) return "8h (cap)";

            long h = seconds / 3600;
            long m = (seconds % 3600) / 60;
            long s = seconds % 60;
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m {s:D2}s";
        }
    }
}
