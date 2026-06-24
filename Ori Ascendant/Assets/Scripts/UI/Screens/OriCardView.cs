using System;
using OriAscendant.Data;
using OriAscendant.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>One selectable virtue card on the OriScreen (Dynasty PRD Phase 1,
    /// slice 1) — virtue name + the placeholder vow line. Mirrors PathCardView.</summary>
    public class OriCardView : MonoBehaviour
    {
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _background;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _vowText;

        // Hardcoded Idle/Selected colors removed — CardViewSpec is now the source.

        private Image     _ring;        // ponytail: created once in Awake
        private Graphic[] _labelGraphics; // cached to avoid per-SetSelected GC alloc

        private void Awake()
        {
            CardSelectionVisual.InitBackground(_background);
            _ring = CardSelectionVisual.CreateRing(transform);
            _labelGraphics = new Graphic[] { _nameText, _vowText };
        }

        public void Bind(OriVirtue virtue, int index, Action<int> onSelected)
        {
            // Ensure ring/sprite exist when Awake hasn't run (EditMode test setup).
            if (_ring == null) _ring = CardSelectionVisual.CreateRing(transform);
            if (_background != null && _background.sprite == null)
                CardSelectionVisual.InitBackground(_background);
            if (_labelGraphics == null)
                _labelGraphics = new Graphic[] { _nameText, _vowText };

            if (_nameText != null) _nameText.text = virtue?.virtueName;
            if (_vowText  != null) _vowText.text  = virtue?.vowLine;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => onSelected?.Invoke(index));
            }
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            // Primary text: name is the salient label; vow is a subtitle but still useful to tint.
            CardSelectionVisual.Apply(selected, transform, _background, _ring, _labelGraphics);
        }
    }
}
