using OriAscendant.Core;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The Àkùnlẹ̀yàn modal (Dynasty PRD Phase 1, slice 1). Mirrors PathScreenView:
    /// mandatory at the start of every life, runtime-bound cards from the live
    /// OriConfig, no dismiss — choosing IS the vow. UI never writes state
    /// directly: the confirm call goes through OriSystem.ChooseOri.
    /// </summary>
    public class OriScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private OriCardView[] _cards;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;

        private OriSystem _oriSystem;
        private int _selectedIndex = -1;
        private bool _bound;

        /// <summary>True while the modal root is active in the scene.</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

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
            _oriSystem = ServiceLocator.Get<OriSystem>();
            if (!_bound)
            {
                var config = _oriSystem != null ? _oriSystem.Config : null;
                int count = config != null ? config.Count : 0;
                for (int i = 0; i < _cards.Length && i < count; i++)
                {
                    _cards[i].Bind(config.GetVirtue(i), i, Select);
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
                _confirmLabel.text = _selectedIndex >= 0 ? "Vow this Ori" : "Choose an Ori";
            }
        }

        private void Confirm()
        {
            if (_selectedIndex < 0 || _oriSystem == null) return;
            if (_oriSystem.ChooseOri(_selectedIndex) && _root != null)
            {
                _root.SetActive(false);
            }
        }
    }
}
