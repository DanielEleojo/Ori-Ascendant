using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The bloodline Chronicle: an unbroken vertical thread of light flowing back
    /// through every generation (issue #27). One node per generation — bright for
    /// Ascended, ember for a fall. The thread is never cut; a fallen generation
    /// is an ember node, not a gap. Scrollable for long histories.
    /// Display-only; game state is never written here.
    /// </summary>
    public class ChronicleScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Button _closeButton;

        /// <summary>Layout constants for the bloodline thread — public so cohesion tests can
        /// assert they are self-consistent without requiring a scene. ponytail: inner class</summary>
        public static class Layout
        {
            public const float NodeRowHeight  = 80f;
            public const float ThreadX        = 24f;   // x-centre of the thread line
            public const float ThreadWidth    = 3f;
            public const float DotSize        = 14f;
            public const float TextLeftMargin = SpacingScale.Xxl; // ponytail: 48px section gap
            public const float GenLabelFontSize    = TypographicScale.BodySm;  // ponytail: 14pt — "Gen N — Ascended"
            public const float RemembranceFontSize = TypographicScale.Label;  // ponytail: 13pt — sub-caption
            public const float OriNameFontSize    = 12f;  // bespoke: between Caption(11) and Label(13)
        }

        public bool IsOpen => _root != null && _root.activeSelf;

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
            Refresh();
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        private void Hide()
        {
            _transition.Close();
        }

        private void Refresh()
        {
            if (_contentRoot == null) return;

            foreach (Transform child in _contentRoot)
                Destroy(child.gameObject);

            if (!ServiceLocator.TryGet(out SaveManager saveManager) || saveManager.Current == null) return;
            var save = saveManager.Current;

            string[] oriNames = ResolveOriNames();

            if (save.chronicle.Count == 0)
            {
                BuildPlaceholderRow("No generations recorded yet.");
                return;
            }

            var nodes = ChronicleThreadMapper.MapAll(save.chronicle);
            for (int i = 0; i < nodes.Length; i++)
                BuildThreadNode(nodes[i], save.chronicle[i], oriNames);
        }

        private static string[] ResolveOriNames()
        {
            if (!ServiceLocator.TryGet(out OriSystem oriSystem)) return null;
            OriVirtue[] virtues = oriSystem.Config?.virtues;
            if (virtues == null) return null;
            var names = new string[virtues.Length];
            for (int i = 0; i < virtues.Length; i++)
                names[i] = virtues[i].virtueName;
            return names;
        }

        private void BuildPlaceholderRow(string message)
        {
            var go = new GameObject("Empty", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_contentRoot, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
            le.flexibleWidth = 1f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = Layout.GenLabelFontSize; // ponytail: BodySm via Layout
            tmp.color = Palette.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Builds one node row: a thread segment (always drawn) + a coloured node dot +
        /// label and remembrance text beside it. The row height is uniform so the thread
        /// segments stack into one unbroken vertical line across the full history.
        /// </summary>
        private void BuildThreadNode(ChronicleNodeState node, ChronicleEntry entry, string[] oriNames)
        {
            var rowGo = new GameObject("Node", typeof(RectTransform));
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.SetParent(_contentRoot, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = Layout.NodeRowHeight;
            le.flexibleWidth = 1f;

            // Thread segment — drawn in every row so the line is never broken.
            var threadGo = new GameObject("Thread", typeof(RectTransform));
            var threadRt = (RectTransform)threadGo.transform;
            threadRt.SetParent(rowRt, false);
            threadRt.anchorMin = Vector2.zero;
            threadRt.anchorMax = new Vector2(0f, 1f);
            threadRt.pivot     = new Vector2(0f, 0.5f);
            threadRt.offsetMin = new Vector2(Layout.ThreadX - Layout.ThreadWidth * 0.5f, 0f);
            threadRt.offsetMax = new Vector2(Layout.ThreadX + Layout.ThreadWidth * 0.5f, 0f);
            var threadImg = threadGo.AddComponent<Image>();
            threadImg.color = ChronicleThreadMapper.ThreadLineColor;

            // Node dot — colour reflects outcome (bright gold or warm ember).
            var dotGo = new GameObject("Dot", typeof(RectTransform));
            var dotRt = (RectTransform)dotGo.transform;
            dotRt.SetParent(rowRt, false);
            dotRt.anchorMin = new Vector2(0f, 0.5f);
            dotRt.anchorMax = new Vector2(0f, 0.5f);
            dotRt.pivot     = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(Layout.ThreadX, 0f);
            dotRt.sizeDelta = new Vector2(Layout.DotSize, Layout.DotSize);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = node.NodeColor;

            // Label: "Gen N  —  Ascended / Fell"
            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(rowRt, false);
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(Layout.TextLeftMargin, 0f);
            labelRt.offsetMax = new Vector2(-8f, -4f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text      = node.Label;
            labelTmp.fontSize  = Layout.GenLabelFontSize; // ponytail: BodySm via Layout
            labelTmp.color     = entry.didAscend ? Palette.AseGold : Palette.EmberWarm;
            labelTmp.alignment = TextAlignmentOptions.BottomLeft;

            // Ori name (top-right, dimmed) — optional
            if (entry.chosenOri >= 0)
            {
                string oriName = oriNames != null && entry.chosenOri < oriNames.Length
                    ? oriNames[entry.chosenOri]
                    : $"Ori {entry.chosenOri}";
                var oriGo = new GameObject("Ori", typeof(RectTransform));
                var oriRt = (RectTransform)oriGo.transform;
                oriRt.SetParent(rowRt, false);
                oriRt.anchorMin = new Vector2(0.6f, 0.5f);
                oriRt.anchorMax = new Vector2(1f,   1f);
                oriRt.offsetMin = new Vector2(0f,  0f);
                oriRt.offsetMax = new Vector2(-8f, -4f);
                var oriTmp = oriGo.AddComponent<TextMeshProUGUI>();
                oriTmp.text      = oriName;
                oriTmp.fontSize  = Layout.OriNameFontSize; // ponytail: bespoke 12pt, not a token
                oriTmp.color     = Palette.TextSecondary;
                oriTmp.alignment = TextAlignmentOptions.BottomRight;
            }

            // Remembrance (bottom)
            var remGo = new GameObject("Remembrance", typeof(RectTransform));
            var remRt = (RectTransform)remGo.transform;
            remRt.SetParent(rowRt, false);
            remRt.anchorMin = new Vector2(0f, 0f);
            remRt.anchorMax = new Vector2(1f, 0.5f);
            remRt.offsetMin = new Vector2(Layout.TextLeftMargin, 4f);
            remRt.offsetMax = new Vector2(-8f, 0f);
            var remTmp = remGo.AddComponent<TextMeshProUGUI>();
            remTmp.text      = node.Remembrance;
            remTmp.fontSize  = Layout.RemembranceFontSize; // ponytail: Label via Layout
            remTmp.color     = Palette.TextPrimary;
            remTmp.alignment = TextAlignmentOptions.TopLeft;
        }
    }
}
