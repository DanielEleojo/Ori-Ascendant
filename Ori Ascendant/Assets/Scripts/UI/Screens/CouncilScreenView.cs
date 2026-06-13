using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The lineage shrine (GAMEPLAY §3.6): ancestor cards, the lineage
    /// foundation (retired ancestors' permanent bonus), and the total blessing
    /// factor. Display-only — carved-staff/calabash framing arrives with Phase D
    /// art; abstract tints only (§7.3). Per-ancestor contribution copy is
    /// computed from the CURRENT path's councilBonusModifier at display time.
    /// </summary>
    public class CouncilScreenView : MonoBehaviour
    {
        [System.Serializable]
        public struct CardRow
        {
            public GameObject root;
            public Image motif;
            public TMP_Text title;
            public TMP_Text contribution;
        }

        [SerializeField] private GameObject _root;
        [SerializeField] private CardRow[] _rows; // 5
        [SerializeField] private TMP_Text _foundationLine;
        [SerializeField] private TMP_Text _totalLine;
        [SerializeField] private Button _closeButton;

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
            if (!ServiceLocator.TryGet(out SaveManager saveManager) || saveManager.Current == null) return;
            var save = saveManager.Current;

            double w = ServiceLocator.TryGet(out AncestralCouncilSystem council) ? council.W : 0.25;
            double sum = council?.ActiveCouncilSum ?? 0.0;
            double mod = ServiceLocator.TryGet(out CultivationSystem cultivation)
                ? cultivation.CouncilBonusModifier : 1.0;

            for (int i = 0; i < _rows.Length; i++)
            {
                bool filled = i < save.council.Count;
                if (_rows[i].root != null) _rows[i].root.SetActive(true);

                if (!filled)
                {
                    if (_rows[i].motif != null) _rows[i].motif.color = PathMotif.Neutral;
                    if (_rows[i].title != null) _rows[i].title.text = "An empty seat awaits";
                    if (_rows[i].contribution != null) _rows[i].contribution.text = string.Empty;
                    continue;
                }

                var ancestor = save.council[i];
                // Council list is append-ordered; its generation = position in
                // lineage history. Reconstruct: current generationCount minus the
                // members after it (newest joined last generation).
                int generationNumber = save.lineage.generationCount - (save.council.Count - 1 - i);

                if (_rows[i].motif != null)
                    _rows[i].motif.color = PathMotif.AncestorTint(ancestor.path, ancestor.didAscend);
                if (_rows[i].title != null)
                    _rows[i].title.text =
                        $"Gen {generationNumber} — Aṣẹ́gun of {PathMotif.TitleOf(ancestor.path)}" +
                        (ancestor.didAscend ? string.Empty : "  (ember)");
                if (_rows[i].contribution != null)
                {
                    double portion = w * ancestor.bonusMultiplier * mod;
                    _rows[i].contribution.text = $"+{portion:P0}";
                }
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
