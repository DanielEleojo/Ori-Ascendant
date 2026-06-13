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

        private void Awake()
        {
            if (_confirmButton != null) _confirmButton.onClick.AddListener(Confirm);
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(Confirm);
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
            if (_selectedIndex < 0 || _cultivation == null) return;
            if (_cultivation.ChoosePath(_selectedIndex) && _root != null)
            {
                _root.SetActive(false);
            }
        }
    }
}
