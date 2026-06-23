using System;
using OriAscendant.Data;
using OriAscendant.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>One selectable path card on the PathScreen (GAMEPLAY §3.3) —
    /// deity name, tradition of origin, and ONE concrete stat line.</summary>
    public class PathCardView : MonoBehaviour
    {
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _traditionText;
        [SerializeField] private TMP_Text _identityText;

        // Hardcoded Idle/Selected colors removed — CardViewSpec is now the source.

        private Image     _ring;        // ponytail: created once in Awake
        private Graphic[] _labelGraphics; // cached to avoid per-SetSelected GC alloc

        private void Awake()
        {
            CardSelectionVisual.InitBackground(_background);
            _ring = CardSelectionVisual.CreateRing(transform);
            _labelGraphics = new Graphic[] { _nameText, _identityText };
        }

        public void Bind(PathConfig path, int index, Action<int> onSelected)
        {
            // Ensure ring/sprite exist when Awake hasn't run (EditMode test setup).
            if (_ring == null) _ring = CardSelectionVisual.CreateRing(transform);
            if (_background != null && _background.sprite == null)
                CardSelectionVisual.InitBackground(_background);
            if (_labelGraphics == null)
                _labelGraphics = new Graphic[] { _nameText, _identityText };

            if (_nameText      != null) _nameText.text      = path.pathName;
            if (_traditionText != null) _traditionText.text = path.traditionLabel;
            if (_identityText  != null) _identityText.text  = path.identityLine;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onSelected?.Invoke(index));
            }
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            // Primary texts: name and identity line are the salient labels.
            CardSelectionVisual.Apply(selected, transform, _background, _ring, _labelGraphics);
        }
    }
}
