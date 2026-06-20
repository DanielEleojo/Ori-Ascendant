using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>About &amp; Glossary (GAMEPLAY §3.7). Builds its body from
    /// <see cref="HeritageContent"/> at show-time so the content has a single
    /// source of truth. Display-only.</summary>
    public class AboutScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private Button _closeButton;

        private bool _built;
        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_root != null)
            {
                _root.SetActive(false);
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_transition.TickAndApply(_canvasGroup, _root.transform, Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
                _root.SetActive(false);
        }

        public void Show()
        {
            if (!_built) { BuildBody(); _built = true; }
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        private void Hide()
        {
            _transition.Close();
        }

        private void BuildBody()
        {
            if (_body == null) return;

            var sb = new StringBuilder();
            sb.Append("<size=120%><b>Ori Ascendant</b></size>\n\n");
            sb.Append(HeritageContent.Heritage);
            sb.Append("\n\n<size=120%><b>Glossary</b></size>\n");
            foreach (var term in HeritageContent.Glossary)
            {
                sb.Append($"\n<b>{term.Word}</b>  <i>{term.Pronunciation}</i>  <size=80%>({term.Tradition})</size>\n");
                sb.Append($"{term.Meaning}\n");
            }
            _body.text = sb.ToString();
        }
    }
}
