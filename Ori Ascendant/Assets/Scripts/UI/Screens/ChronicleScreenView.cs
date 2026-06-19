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
    /// The bloodline Chronicle: an unbounded scrollable list of every completed
    /// generation's Ori, outcome, and Title/Nickname (issue #7). Unlike the
    /// Ancestral Council (capped at 5), this screen remembers them all — retired
    /// ancestors remain in the Chronicle so the player can read the full saga.
    /// Display-only; game state is never written here.
    /// </summary>
    public class ChronicleScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Button _closeButton;

        private static readonly Color Gold = new Color(0.851f, 0.643f, 0.255f);
        private static readonly Color Text = new Color(0.925f, 0.902f, 0.847f);
        private static readonly Color TextDim = new Color(0.604f, 0.639f, 0.698f);
        private static readonly Color PanelLine = new Color(0.165f, 0.192f, 0.251f);

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            Refresh();
            if (_root != null) _root.SetActive(true);
        }

        private void Hide()
        {
            if (_root != null) _root.SetActive(false);
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

            foreach (var entry in save.chronicle)
                BuildRow(entry, oriNames);
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
            tmp.fontSize = 14f;
            tmp.color = TextDim;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void BuildRow(ChronicleEntry entry, string[] oriNames)
        {
            var rowGo = new GameObject("Row", typeof(RectTransform));
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.SetParent(_contentRoot, false);
            var le = rowGo.AddComponent<LayoutElement>();
            le.preferredHeight = 68f;
            le.flexibleWidth = 1f;
            var bg = rowGo.AddComponent<Image>();
            bg.color = PanelLine;

            // Generation + outcome (top-left)
            var topGo = new GameObject("Top", typeof(RectTransform));
            var topRt = (RectTransform)topGo.transform;
            topRt.SetParent(rowRt, false);
            topRt.anchorMin = new Vector2(0f, 0.55f);
            topRt.anchorMax = new Vector2(0.7f, 1f);
            topRt.offsetMin = new Vector2(12f, 0f);
            topRt.offsetMax = new Vector2(0f, -4f);
            var topText = topGo.AddComponent<TextMeshProUGUI>();
            topText.text = $"Gen {entry.generationNumber}  —  {(entry.didAscend ? "Ascended" : "Fell")}";
            topText.fontSize = 14f;
            topText.color = entry.didAscend ? Gold : Text;
            topText.alignment = TextAlignmentOptions.MidlineLeft;

            // Ori name (top-right, dimmed)
            if (entry.chosenOri >= 0)
            {
                string oriName = oriNames != null && entry.chosenOri < oriNames.Length
                    ? oriNames[entry.chosenOri]
                    : $"Ori {entry.chosenOri}";
                var oriGo = new GameObject("Ori", typeof(RectTransform));
                var oriRt = (RectTransform)oriGo.transform;
                oriRt.SetParent(rowRt, false);
                oriRt.anchorMin = new Vector2(0.7f, 0.55f);
                oriRt.anchorMax = new Vector2(1f, 1f);
                oriRt.offsetMin = new Vector2(0f, 0f);
                oriRt.offsetMax = new Vector2(-12f, -4f);
                var oriText = oriGo.AddComponent<TextMeshProUGUI>();
                oriText.text = oriName;
                oriText.fontSize = 12f;
                oriText.color = TextDim;
                oriText.alignment = TextAlignmentOptions.MidlineRight;
            }

            // Title / Nickname (bottom)
            var remGo = new GameObject("Remembrance", typeof(RectTransform));
            var remRt = (RectTransform)remGo.transform;
            remRt.SetParent(rowRt, false);
            remRt.anchorMin = new Vector2(0f, 0.05f);
            remRt.anchorMax = new Vector2(1f, 0.55f);
            remRt.offsetMin = new Vector2(12f, 0f);
            remRt.offsetMax = new Vector2(-12f, 0f);
            var remText = remGo.AddComponent<TextMeshProUGUI>();
            remText.text = entry.remembrance ?? "—";
            remText.fontSize = 13f;
            remText.color = Gold;
            remText.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }
}
