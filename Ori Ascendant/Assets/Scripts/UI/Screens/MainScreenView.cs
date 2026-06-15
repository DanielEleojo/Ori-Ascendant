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
    /// start; text is rebuilt only when the version changes, and assigned only
    /// when the formatted string actually differs (minimizes canvas rebuilds).
    /// </summary>
    public class MainScreenView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _aseCounterText;
        [SerializeField] private TMP_Text _rateText;
        [SerializeField] private TMP_Text _stageText;
        [SerializeField] private TMP_Text _generationText;
        [SerializeField] private TMP_Text _pathBadge;
        [SerializeField] private TMP_Text _oriBadge;

        private AseGenerationSystem _aseGeneration;
        private SaveManagerHandle _saveHandle;
        private int _lastVersion = -1;
        private string _lastCounter;
        private string _lastRate;

        private void Start()
        {
            _aseGeneration = ServiceLocator.Get<AseGenerationSystem>();
            _saveHandle = new SaveManagerHandle();
        }

        private void Update()
        {
            if (_aseGeneration == null || _aseGeneration.StateVersion == _lastVersion) return;
            _lastVersion = _aseGeneration.StateVersion;

            string counter = _aseGeneration.CurrentAse.ToString();
            if (counter != _lastCounter)
            {
                _lastCounter = counter;
                if (_aseCounterText != null) _aseCounterText.text = counter;
            }

            string rate = "+" + _aseGeneration.CurrentRate + " Àṣẹ/s";
            if (rate != _lastRate)
            {
                _lastRate = rate;
                if (_rateText != null) _rateText.text = rate;
            }

            RefreshIdentity();
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
                var ori = cultivation?.CurrentOriConfig;
                bool show = ori != null && !string.IsNullOrEmpty(ori.oriName);
                if (_oriBadge.gameObject.activeSelf != show) _oriBadge.gameObject.SetActive(show);
                if (show && _oriBadge.text != ori.oriName) _oriBadge.text = ori.oriName;
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
