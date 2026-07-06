using OriAscendant.Core;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// MainScreen read-only view (GAMEPLAY §3.2 zones 2–3). UI never writes game
    /// state. The counter polls AseGenerationSystem.StateVersion — a cheap int
    /// compare per frame — instead of racing event-subscription order at scene
    /// start. The hero counter is an odometer: the displayed value eases toward
    /// the latest true Àṣẹ (WelcomeBackModal count-up feel) instead of snapping;
    /// text is assigned only when the formatted string actually differs
    /// (minimizes canvas rebuilds).
    /// </summary>
    public class MainScreenView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _aseCounterText;
        [SerializeField] private TMP_Text _rateText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _generationText;
        [SerializeField] private TMP_Text _pathBadge;
        [SerializeField] private TMP_Text _oriBadge;
        [SerializeField] private TMP_Text _steadfastnessText;

        private AseGenerationSystem _aseGeneration;
        private SaveManagerHandle _saveHandle;
        private int _lastVersion = -1;
        private string _lastCounter;
        private string _lastRate;

        // Hero counter odometer roll: display-only interpolation in double space
        // (.ToDouble() — game math stays in BigNumber). Retargeted to the latest
        // true value on every state change, never queued.
        private const float RollSeconds = 0.4f; // capped approach — even an 8h offline jump lands in one roll
        private BigNumber _targetAse;
        private double _targetDouble;
        private double _displayedDouble;
        private double _rollFrom;
        private float _rollElapsed;
        private bool _rolling;

        private void Start()
        {
            _aseGeneration = ServiceLocator.Get<AseGenerationSystem>();
            _saveHandle = new SaveManagerHandle();
        }

        private void Update()
        {
            if (_aseGeneration == null) return;

            if (_aseGeneration.StateVersion != _lastVersion)
            {
                _lastVersion = _aseGeneration.StateVersion;
                RetargetCounter();

                string rate = "+" + _aseGeneration.CurrentRate + " Àṣẹ per breath";
                if (rate != _lastRate)
                {
                    _lastRate = rate;
                    if (_rateText != null) _rateText.text = rate;
                }

                RefreshIdentity();
            }

            TickCounterRoll();
        }

        /// <summary>Points the odometer at the latest true Àṣẹ. Increases roll
        /// (same ease-out feel as the WelcomeBackModal count-up); Reduce Motion,
        /// decreases (spend/reset), and values past double range snap instead.</summary>
        private void RetargetCounter()
        {
            _targetAse = _aseGeneration.CurrentAse;
            _targetDouble = _targetAse.ToDouble();

            // ponytail: ToDouble() saturates past e300 — interpolating there would
            // overflow, so extreme late-game just snaps like today.
            bool snap = MotionHelper.IsReduceMotion()
                || _targetDouble <= _displayedDouble
                || _targetDouble >= double.MaxValue;
            if (snap)
            {
                FinishRoll();
                return;
            }

            _rollFrom = _displayedDouble;
            _rollElapsed = 0f;
            _rolling = true;
        }

        private void TickCounterRoll()
        {
            if (!_rolling) return;

            _rollElapsed += Time.unscaledDeltaTime;
            float t = _rollElapsed / RollSeconds;
            double shown = _rollFrom + (_targetDouble - _rollFrom) * MotionHelper.EaseOut(t);

            // Land exactly at end-of-roll or within epsilon of the target;
            // Reduce Motion toggled mid-roll also snaps to done.
            if (t >= 1f || _targetDouble - shown <= _targetDouble * 1e-9 || MotionHelper.IsReduceMotion())
            {
                FinishRoll();
                return;
            }

            _displayedDouble = shown;
            // Same formatter as the true value so rolled digits read identically.
            // One small ToString per frame while rolling is the TMP floor; settled
            // frames do zero work.
            SetCounterText(BigNumber.FromDouble(shown).ToString());
        }

        private void FinishRoll()
        {
            _rolling = false;
            _displayedDouble = _targetDouble;
            // The true BigNumber's own string — no double round-trip on the final value.
            SetCounterText(_targetAse.ToString());
        }

        private void SetCounterText(string counter)
        {
            if (counter == _lastCounter) return;
            _lastCounter = counter;
            if (_aseCounterText != null) _aseCounterText.text = counter;
        }

        private void RefreshIdentity()
        {
            var save = _saveHandle.Current;
            if (save == null) return;

            ServiceLocator.TryGet(out CultivationSystem cultivation);

            if (_stageText != null)
            {
                string name = cultivation?.CurrentStageConfig?.stageName;
                _stageText.text = name != null
                    ? $"Stage {save.currentStage + 1} — {name}"
                    : $"Stage {save.currentStage + 1}";
            }
            if (_generationText != null)
            {
                _generationText.text = $"Gen {save.lineage.generationCount + 1}";
            }
            if (_pathBadge != null)
            {
                var path = cultivation?.CurrentPathConfig;
                bool show = path != null && !string.IsNullOrEmpty(path.hookBadge);
                if (_pathBadge.gameObject.activeSelf != show) _pathBadge.gameObject.SetActive(show);
                if (show && _pathBadge.text != path.hookBadge) _pathBadge.text = path.hookBadge;
            }
            if (_oriBadge != null)
            {
                // Àkùnlẹ̀yàn vow shown on the main screen for the whole life
                // (Dynasty PRD Phase 1, slice 1). Cleared by the Crossing reset.
                ServiceLocator.TryGet(out OriSystem oriSystem);
                var virtue = oriSystem?.ChosenVirtue;
                bool show = virtue != null && !string.IsNullOrEmpty(virtue.virtueName);
                if (_oriBadge.gameObject.activeSelf != show) _oriBadge.gameObject.SetActive(show);
                if (show)
                {
                    string label = $"Ori — {virtue.virtueName}";
                    if (_oriBadge.text != label) _oriBadge.text = label;
                }
            }
            if (_steadfastnessText != null)
            {
                // Live steadfastness tally (Dynasty PRD Phase 1, slice 2a).
                // Hidden until the first crossroads has been resolved (oriTrials == 0
                // means no dilemma has been faced yet this life).
                bool show = save.oriTrials > 0;
                if (_steadfastnessText.gameObject.activeSelf != show)
                    _steadfastnessText.gameObject.SetActive(show);
                if (show)
                {
                    string label = $"held {save.oriHeld} of {save.oriTrials}";
                    if (_steadfastnessText.text != label) _steadfastnessText.text = label;
                }
            }
        }

        /// <summary>Lazy, null-tolerant SaveManager accessor (view must work in
        /// partially-wired test scenes).</summary>
        private sealed class SaveManagerHandle
        {
            private Save.SaveManager _manager;

            public Save.SaveData Current
            {
                get
                {
                    if (_manager == null) ServiceLocator.TryGet(out _manager);
                    return _manager != null ? _manager.Current : null;
                }
            }
        }
    }
}
