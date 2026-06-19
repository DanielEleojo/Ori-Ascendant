using System;
using OriAscendant.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// One tappable option button on the CrossroadsScreen (Dynasty PRD Phase 1,
    /// slice 2a). Mirrors the OriCardView selection pattern.
    /// </summary>
    public class CrossroadsOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _optionText;

        private static readonly Color Idle     = new Color(0.102f, 0.122f, 0.161f); // panel
        private static readonly Color Selected = new Color(0.231f, 0.192f, 0.102f); // gold-tinted

        public void Bind(CrossroadsOption option, int index, Action<int> onSelected)
        {
            if (_optionText != null) _optionText.text = option?.optionText;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onSelected?.Invoke(index));
            }
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_background != null) _background.color = selected ? Selected : Idle;
        }
    }
}
