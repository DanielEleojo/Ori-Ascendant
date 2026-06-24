using System;
using OriAscendant.Data;
using OriAscendant.UI;
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
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _background;
        [SerializeField] private TMP_Text _optionText;

        // Hardcoded Idle/Selected colors removed — CardViewSpec is now the source.

        private Image     _ring;        // ponytail: created once in Awake
        private Graphic[] _labelGraphics; // cached to avoid per-SetSelected GC alloc

        private void Awake()
        {
            CardSelectionVisual.InitBackground(_background);
            _ring = CardSelectionVisual.CreateRing(transform);
            _labelGraphics = new Graphic[] { _optionText };
        }

        public void Bind(CrossroadsOption option, int index, Action<int> onSelected)
        {
            // Ensure ring/sprite exist when Awake hasn't run (EditMode test setup).
            if (_ring == null) _ring = CardSelectionVisual.CreateRing(transform);
            if (_background != null && _background.sprite == null)
                CardSelectionVisual.InitBackground(_background);
            if (_labelGraphics == null)
                _labelGraphics = new Graphic[] { _optionText };

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
            CardSelectionVisual.Apply(selected, transform, _background, _ring, _labelGraphics);
        }
    }
}
