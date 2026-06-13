using System;
using OriAscendant.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>One selectable path card on the PathScreen (GAMEPLAY §3.3) —
    /// deity name, tradition of origin, and ONE concrete stat line.</summary>
    public class PathCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _traditionText;
        [SerializeField] private TMP_Text _identityText;

        private static readonly Color Idle = new Color(0.102f, 0.122f, 0.161f);     // panel
        private static readonly Color Selected = new Color(0.231f, 0.192f, 0.102f); // gold-tinted

        public void Bind(PathConfig path, int index, Action<int> onSelected)
        {
            if (_nameText != null) _nameText.text = path.pathName;
            if (_traditionText != null) _traditionText.text = path.traditionLabel;
            if (_identityText != null) _identityText.text = path.identityLine;

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
