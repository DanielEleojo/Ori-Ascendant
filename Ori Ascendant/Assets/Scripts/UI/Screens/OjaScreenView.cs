using OriAscendant.Core;
using OriAscendant.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    public class OjaScreenView : MonoBehaviour
    {
        [Header("Standing")]
        [SerializeField] private TMP_Text _rankNumberText;
        [SerializeField] private TMP_Text _rankOrdinalText;
        [SerializeField] private TMP_Text _renownValueText;
        [SerializeField] private TMP_Text _nextRankLine;
        [SerializeField] private Image _rankProgressBar;
        [SerializeField] private Button _closeButton;

        [SerializeField] private GameObject _root;

        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private const double RenownPerRank = 0.01;

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
            double renown = ResolveRenown();
            var standing = MarketplaceStandingPresenter.Map(renown);

            if (_rankNumberText != null)
            {
                _rankNumberText.text = standing.Rank.ToString();
                _rankNumberText.color = Palette.OsunRiverTeal;
            }

            if (_rankOrdinalText != null)
                _rankOrdinalText.text = $"{MarketplaceStandingPresenter.Ordinal(standing.Rank)} in the Marketplace";

            if (_renownValueText != null)
                _renownValueText.text = $"Renown {renown:0.00}";

            double nextThreshold = (System.Math.Floor(renown / RenownPerRank) + 1.0) * RenownPerRank;
            if (_nextRankLine != null)
                _nextRankLine.text = $"Next rank at {nextThreshold:0.00} renown";

            if (_rankProgressBar != null)
            {
                float progress = (float)(renown % RenownPerRank / RenownPerRank);
                _rankProgressBar.fillAmount = Mathf.Clamp01(progress);
                _rankProgressBar.color = Color.Lerp(Palette.OsunRiverTeal, Palette.OsunPale, progress);
            }
        }

        private static double ResolveRenown()
        {
            if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current != null)
                return saveManager.Current.lineage.renown;
            return 0.0;
        }
    }
}
