using OriAscendant.Core;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The path-choice gate (GAMEPLAY §3.3). Opens when the player taps Advance
    /// at the Tier 0 peak; mandatory, no dismiss — choosing IS the advance into
    /// Tier 1. Cards populate at runtime from the PathConfig assets so copy and
    /// numbers always match the live config. UI never writes state directly:
    /// the confirm calls CultivationSystem.ChoosePath.
    /// </summary>
    public class PathScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private PathCardView[] _cards; // 3, index-aligned with CultivationSystem.Paths
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;

        private CultivationSystem _cultivation;
        private int _selectedIndex = -1;
        private bool _bound;
        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private void Awake()
        {
            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            if (_root != null)
            {
                _root.SetActive(false);
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(Confirm);
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_transition.TickAndApply(_canvasGroup, _root.transform, Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
                _root.SetActive(false);
        }

        public void Show()
        {
            _cultivation = ServiceLocator.Get<CultivationSystem>();
            if (!_bound)
            {
                var paths = _cultivation.Paths;
                for (int i = 0; i < _cards.Length && i < paths.Length; i++)
                {
                    _cards[i].Bind(paths[i], i, Select);
                }
                _bound = true;
            }

            _selectedIndex = -1;
            RefreshSelection();
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        private void Select(int index)
        {
            _selectedIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetSelected(i == _selectedIndex);
            }
            if (_confirmButton != null) _confirmButton.interactable = _selectedIndex >= 0;
            if (_confirmLabel != null)
            {
                _confirmLabel.text = _selectedIndex >= 0 ? "Walk this Path" : "Choose a Path";
            }
        }

        private void Confirm()
        {
            int idx = _selectedIndex;
            if (idx < 0 || _cultivation == null) return;
            _selectedIndex = -1;
            if (_confirmButton != null) _confirmButton.interactable = false;
            if (_cultivation.ChoosePath(idx)) _transition.Close();
        }
    }
}
