using OriAscendant.Core;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The birth-of-life vow gate (Àkùnlẹ̀yàn). Opens at the start of a life when no
    /// Ori has been vowed; mandatory, no dismiss (the dim blocks the climb beneath).
    /// Cards populate at runtime from the OriConfig set so copy matches the live
    /// config. UI never writes state directly: the confirm calls
    /// CultivationSystem.ChooseOri. Unlike the Path gate, choosing does NOT advance
    /// a stage — the climb proceeds normally afterward. Mirrors PathScreenView.
    /// </summary>
    public class OriScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private OriCardView[] _cards; // index-aligned with CultivationSystem.Oris
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
                var oris = _cultivation.Oris;
                for (int i = 0; i < _cards.Length && oris != null && i < oris.Length; i++)
                {
                    _cards[i].Bind(oris[i], i, Select);
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
                _confirmLabel.text = _selectedIndex >= 0 ? "Take this Vow" : "Choose your Ori";
            }
        }

        private void Confirm()
        {
            if (_selectedIndex < 0 || _cultivation == null) return;
            if (_cultivation.ChooseOri(_selectedIndex) && _root != null)
            {
                _root.SetActive(false);
            }
        }
    }
}
