using System;
using OriAscendant.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>One selectable Ori (virtue-vow) card on the OriScreen — the virtue
    /// name and a short line of flavour. Mirrors PathCardView.</summary>
    public class OriCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _descriptionText;

        private static readonly Color Idle = new Color(0.102f, 0.122f, 0.161f);     // panel
        private static readonly Color Selected = new Color(0.231f, 0.192f, 0.102f); // gold-tinted

        public void Bind(OriConfig ori, int index, Action<int> onSelected)
        {
            if (_nameText != null) _nameText.text = ori.oriName;
            if (_descriptionText != null) _descriptionText.text = ori.oriDescription;

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
