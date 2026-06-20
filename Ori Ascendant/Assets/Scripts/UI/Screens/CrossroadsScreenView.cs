using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// Crossroads modal (Dynasty PRD Phase 1, slice 2a). Mirrors the OriScreenView
    /// pattern: shows when CrossroadsSystem.HasPending is true, driven by
    /// MainScreenController.TickCrossroadsPrompt rather than an event subscription
    /// (avoids Start-order race conditions). The player selects an option and
    /// confirms; the confirm call goes through CrossroadsSystem.MakeChoice — this
    /// view never writes game state directly.
    /// </summary>
    public class CrossroadsScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private CrossroadsOptionView[] _optionViews;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TMP_Text _confirmLabel;

        private CrossroadsSystem _crossroadsSystem;
        private int _selectedIndex = -1;
        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        public bool IsOpen => _root != null && _root.activeSelf;

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
            if (_crossroadsSystem == null) ServiceLocator.TryGet(out _crossroadsSystem);
            if (_crossroadsSystem == null || !_crossroadsSystem.HasPending) return;

            CrossroadsCard card = _crossroadsSystem.PendingCard;
            if (card == null) return;

            if (_promptText != null) _promptText.text = card.prompt;

            int optionCount = card.options?.Length ?? 0;
            for (int i = 0; i < _optionViews.Length; i++)
            {
                if (_optionViews[i] == null) continue;
                bool visible = i < optionCount;
                _optionViews[i].gameObject.SetActive(visible);
                if (visible) _optionViews[i].Bind(card.options[i], i, Select);
            }

            _selectedIndex = -1;
            RefreshConfirm();
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        private void Select(int index)
        {
            _selectedIndex = index;
            for (int i = 0; i < _optionViews.Length; i++)
            {
                _optionViews[i]?.SetSelected(i == _selectedIndex);
            }
            RefreshConfirm();
        }

        private void RefreshConfirm()
        {
            if (_confirmButton != null) _confirmButton.interactable = _selectedIndex >= 0;
            if (_confirmLabel != null)
            {
                _confirmLabel.text = _selectedIndex >= 0 ? "Hold to this choice" : "Choose your path";
            }
        }

        private void Confirm()
        {
            if (_selectedIndex < 0 || _crossroadsSystem == null) return;
            if (_crossroadsSystem.MakeChoice(_selectedIndex))
            {
                _transition.Close();
            }
        }
    }
}
