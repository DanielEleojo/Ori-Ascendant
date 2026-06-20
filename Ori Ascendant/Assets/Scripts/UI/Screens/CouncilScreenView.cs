using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The lineage shrine (GAMEPLAY §3.6): ancestor cards as silhouettes of light
    /// in path colour (issue #29), the lineage foundation, and the total blessing
    /// factor. Each row shows a silhouette Image coloured by ShrineAncestorPresenter,
    /// the ancestor's title, and their remembrance. Display-only.
    /// </summary>
    public class CouncilScreenView : MonoBehaviour
    {
        [System.Serializable]
        public struct CardRow
        {
            public GameObject root;
            /// <summary>The silhouette Image — tinted with ShrineAncestorPresenter.Map().SilhouetteColor.</summary>
            public Image motif;
            public TMP_Text title;
            public TMP_Text remembrance;
        }

        [SerializeField] private GameObject _root;
        [SerializeField] private CardRow[] _rows; // 5
        [SerializeField] private TMP_Text _foundationLine;
        [SerializeField] private TMP_Text _totalLine;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _chronicleButton;
        [SerializeField] private ChronicleScreenView _chronicleScreen;

        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_chronicleButton != null) _chronicleButton.onClick.AddListener(OpenChronicle);
            if (_root != null)
            {
                _root.SetActive(false);
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
            if (_chronicleButton != null) _chronicleButton.onClick.RemoveListener(OpenChronicle);
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_transition.TickAndApply(_canvasGroup, _root.transform, Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
                _root.SetActive(false);
        }

        private void OpenChronicle()
        {
            if (_chronicleScreen != null) _chronicleScreen.Show();
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
            if (!ServiceLocator.TryGet(out SaveManager saveManager) || saveManager.Current == null) return;
            var save = saveManager.Current;

            double sum = ServiceLocator.TryGet(out AncestralCouncilSystem council)
                ? council.ActiveCouncilSum : 0.0;
            double mod = ServiceLocator.TryGet(out CultivationSystem cultivation)
                ? cultivation.CouncilBonusModifier : 1.0;

            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].root != null) _rows[i].root.SetActive(true);

                ShrineAncestorRow row;
                if (i >= save.council.Count)
                {
                    row = ShrineAncestorPresenter.EmptySeat;
                }
                else
                {
                    var ancestor = save.council[i];
                    int generationNumber = save.lineage.generationCount - (save.council.Count - 1 - i);
                    row = ShrineAncestorPresenter.Map(ancestor, generationNumber);
                }

                if (_rows[i].motif != null) _rows[i].motif.color = row.SilhouetteColor;
                if (_rows[i].title != null) _rows[i].title.text = row.Title;
                if (_rows[i].remembrance != null) _rows[i].remembrance.text = row.Remembrance;
            }

            if (_foundationLine != null)
            {
                _foundationLine.text = save.lineage.permanentAseBonus > 0.0
                    ? $"Foundation of the house: +{save.lineage.permanentAseBonus * mod:P0}"
                    : "The foundation awaits its first elder";
            }
            if (_totalLine != null)
            {
                double factor = 1.0 + mod * (save.lineage.permanentAseBonus + sum);
                _totalLine.text = $"Lineage blessing: ×{factor:0.00}";
            }
        }
    }
}
