using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Wave B procedural skin for the Ìrékọjá crossing ceremony (ADR-0001).
    /// Enriches the three ceremony phases — reveal, ancestor card, and summary —
    /// by adding child Image objects to already-existing GameObjects. Follows the
    /// exact pattern of MainScreenSkin: RuntimeInitializeOnLoadMethod bootstrap,
    /// Start() decoration inside a silent try/catch.
    ///
    /// Enrichments added:
    ///   Reveal  — hairlines above/below the reveal title block; lineage-delta bar.
    ///   Card    — path-motif dot (top-right corner); inner glow on the card frame.
    ///   Summary — thin top border above the stats block.
    ///
    /// The skin NEVER modifies TribulationScreen, Palette, or any test file. It
    /// degrades silently to the base UI on any failure (cf. cloud-save rule, §6).
    /// </summary>
    public sealed class TribulationScreenSkin : MonoBehaviour
    {
        // Reasonable ceiling for the lineage-factor bar — at 5× the bar is full.
        private const float MaxExpectedFactor = 5.0f;

        // ---- Result state stashed from OnTribulationComplete ----
        private bool _didAscend;
        private float _lineageFactorAfter;
        private bool _resultReady;

        // Shared sprite built once in Start.
        private Sprite _dotSprite;

        // ---- Animated references ----
        private Image _lineageFillImage;
        private Image _revealHairlineAbove;
        private Image _revealHairlineBelow;
        private Image _cardInnerGlow;
        private Image _cardPathDot;

        // ---- Bootstrap (identical pattern to MainScreenSkin) ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Only activate when there is a live TribulationScreen; keeps the skin
            // inert in EditMode/PlayMode test scenes and the main-screen-only scene.
            if (!ServiceLocator.TryGet(out TribulationScreen _)) return;
            new GameObject(nameof(TribulationScreenSkin)).AddComponent<TribulationScreenSkin>();
        }

        // ---- Lifecycle ----

        private void Start()
        {
            // A decorative skin must NEVER break gameplay — degrade silently on failure.
            try
            {
                _dotSprite = ProceduralSprites.BuildDot(64);

                if (ServiceLocator.TryGet(out TribulationScreen tribScreen))
                {
                    EnrichReveal(tribScreen.transform);
                    EnrichAncestorCard(tribScreen.transform);
                    EnrichSummary(tribScreen.transform);
                    tribScreen.OnCeremonyClosed += OnCeremonyClosed;
                }

                if (ServiceLocator.TryGet(out TribulationSystem trib))
                    trib.OnTribulationComplete += OnTribulationComplete;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(
                    $"[TribulationScreenSkin] skin pass failed, leaving base UI: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out TribulationSystem trib))
                trib.OnTribulationComplete -= OnTribulationComplete;
            if (ServiceLocator.TryGet(out TribulationScreen tribScreen))
                tribScreen.OnCeremonyClosed -= OnCeremonyClosed;
        }

        // ---- Event handlers ----

        /// <summary>Stash the outcome and drive visuals while the ceremony overlay is
        /// visible. lineageFactorAfter is computed from live save state — CommitAtomicWrite
        /// runs before NotifyComplete fires so the values are already persisted.</summary>
        private void OnTribulationComplete(bool didAscend, AncestorData ancestor)
        {
            _didAscend = didAscend;
            _resultReady = true;

            // TribulationResult is not retained publicly. Recompute lineageFactorAfter
            // from live service state, which CommitAtomicWrite has already updated.
            _lineageFactorAfter = ComputeLineageFactorAfter();

            RefreshOutcomeColors(ancestor?.path ?? -1);
        }

        /// <summary>Recomputes the lineage factor from live service state.
        /// Mirrors TribulationSystem.BuildResultPreReset's after-write formula:
        ///   1 + (permanentAseBonus + ActiveCouncilSum)
        /// Safe to call from OnTribulationComplete because CommitAtomicWrite
        /// (which updates those fields) runs before NotifyComplete fires.</summary>
        private static float ComputeLineageFactorAfter()
        {
            double perm = 0.0;
            double council = 0.0;
            if (ServiceLocator.TryGet(out SaveManager saveManager) && saveManager.Current != null)
                perm = saveManager.Current.lineage.permanentAseBonus;
            if (ServiceLocator.TryGet(out AncestralCouncilSystem councilSys))
                council = councilSys.ActiveCouncilSum;
            return (float)(1.0 + perm + council);
        }

        private void OnCeremonyClosed()
        {
            // Reset ready flag after the overlay fully closes.
            _resultReady = false;
        }

        // ---- Enrichment: Reveal panel ----

        /// <summary>Adds hairlines above/below the reveal title block and a lineage-
        /// delta bar track + fill below the deltaLine text.</summary>
        private void EnrichReveal(Transform screenRoot)
        {
            // The ceremony root holds _revealTitle baked by SceneBuilder.
            // Try both the serialized-field name form and the common display name.
            var revealTitle = UiBuilder.FindDeep(screenRoot, "_revealTitle")
                           ?? UiBuilder.FindDeep(screenRoot, "RevealTitle");
            if (revealTitle == null) return;

            Transform titleParent = revealTitle.parent;

            // Hairline above the reveal title block.
            _revealHairlineAbove = UiBuilder.NewChildImage(titleParent, "RevealHairlineAbove");
            _revealHairlineAbove.sprite = _dotSprite;
            var haRt = _revealHairlineAbove.rectTransform;
            haRt.anchorMin = new Vector2(0.05f, 1f);
            haRt.anchorMax = new Vector2(0.95f, 1f);
            haRt.pivot     = new Vector2(0.5f, 0f);
            haRt.sizeDelta = new Vector2(0f, 1.5f);
            haRt.anchoredPosition = new Vector2(0f, 4f);
            _revealHairlineAbove.color = Palette.AseGold.WithAlpha(0.25f);

            // Hairline below the title block.
            _revealHairlineBelow = UiBuilder.NewChildImage(titleParent, "RevealHairlineBelow");
            _revealHairlineBelow.sprite = _dotSprite;
            var hbRt = _revealHairlineBelow.rectTransform;
            hbRt.anchorMin = new Vector2(0.05f, 0f);
            hbRt.anchorMax = new Vector2(0.95f, 0f);
            hbRt.pivot     = new Vector2(0.5f, 1f);
            hbRt.sizeDelta = new Vector2(0f, 1.5f);
            hbRt.anchoredPosition = new Vector2(0f, -4f);
            _revealHairlineBelow.color = Palette.AseGold.WithAlpha(0.25f);

            // Lineage-delta bar: anchored below the deltaLine text (or the title
            // parent when deltaLine is not found).
            var deltaLine = UiBuilder.FindDeep(screenRoot, "_deltaLine")
                         ?? UiBuilder.FindDeep(screenRoot, "DeltaLine");
            Transform barParent = deltaLine != null ? deltaLine.parent : titleParent;

            // Track (background).
            var barTrack = UiBuilder.NewChildImage(barParent, "LineageDeltaTrack");
            barTrack.sprite = _dotSprite;
            var btRt = barTrack.rectTransform;
            btRt.anchorMin = new Vector2(0.08f, 0f);
            btRt.anchorMax = new Vector2(0.92f, 0f);
            btRt.pivot     = new Vector2(0.5f, 1f);
            btRt.sizeDelta = new Vector2(0f, 6f);
            btRt.anchoredPosition = new Vector2(0f, -12f);
            barTrack.color = Palette.WarmEcru.WithAlpha(0.08f);

            // Fill (driven from lineageFactorAfter once the result arrives).
            _lineageFillImage = UiBuilder.NewChildImage(btRt, "LineageDeltaFill");
            _lineageFillImage.sprite = _dotSprite;
            _lineageFillImage.type = Image.Type.Filled;
            _lineageFillImage.fillMethod = Image.FillMethod.Horizontal;
            _lineageFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            _lineageFillImage.fillAmount = 0f;
            var bfRt = _lineageFillImage.rectTransform;
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = bfRt.offsetMax = Vector2.zero;
            _lineageFillImage.color = Palette.AseGold.WithAlpha(0.75f);
        }

        // ---- Enrichment: Ancestor card ----

        /// <summary>Adds a path-motif dot (top-right of the card) and a subtle inner
        /// glow behind the card frame.</summary>
        private void EnrichAncestorCard(Transform screenRoot)
        {
            // _cardRoot baked as "CardRoot" by SceneBuilder.
            var cardRoot = UiBuilder.FindDeep(screenRoot, "_cardRoot")
                        ?? UiBuilder.FindDeep(screenRoot, "CardRoot");
            if (cardRoot == null) return;

            // Inner glow: a soft dot behind the frame, path-coloured at low alpha.
            // Sized slightly larger than the card via a uniform outset offset.
            _cardInnerGlow = UiBuilder.NewChildImage(cardRoot, "CardInnerGlow");
            _cardInnerGlow.sprite = _dotSprite;
            var igRt = _cardInnerGlow.rectTransform;
            igRt.anchorMin = Vector2.zero;
            igRt.anchorMax = Vector2.one;
            igRt.offsetMin = new Vector2(-12f, -12f);
            igRt.offsetMax = new Vector2(12f, 12f);
            _cardInnerGlow.color = PathMotif.Neutral.WithAlpha(0.06f);
            igRt.SetSiblingIndex(0); // behind the card frame

            // Path-motif dot: top-right corner, 16×16, alpha 0.7.
            _cardPathDot = UiBuilder.NewChildImage(cardRoot, "CardPathDot");
            _cardPathDot.sprite = _dotSprite;
            var pdRt = _cardPathDot.rectTransform;
            pdRt.anchorMin = pdRt.anchorMax = new Vector2(1f, 1f);
            pdRt.pivot     = new Vector2(1f, 1f);
            pdRt.sizeDelta = new Vector2(16f, 16f);
            pdRt.anchoredPosition = new Vector2(-8f, -8f);
            _cardPathDot.color = PathMotif.Neutral.WithAlpha(0.70f);
        }

        // ---- Enrichment: Summary screen ----

        /// <summary>Adds a thin top border above the stats block.</summary>
        private void EnrichSummary(Transform screenRoot)
        {
            var summaryStats = UiBuilder.FindDeep(screenRoot, "_summaryStats")
                            ?? UiBuilder.FindDeep(screenRoot, "SummaryStats");
            if (summaryStats == null) return;

            var topBorder = UiBuilder.NewChildImage(summaryStats.parent, "SummaryTopBorder");
            topBorder.sprite = _dotSprite;
            var tbRt = topBorder.rectTransform;
            tbRt.anchorMin = new Vector2(0.04f, 1f);
            tbRt.anchorMax = new Vector2(0.96f, 1f);
            tbRt.pivot     = new Vector2(0.5f, 0f);
            tbRt.sizeDelta = new Vector2(0f, 1.5f);
            tbRt.anchoredPosition = new Vector2(0f, 6f);
            topBorder.color = Palette.AseGold.WithAlpha(0.15f);
        }

        // ---- Color application ----

        /// <summary>Updates all outcome-tinted elements with the correct colour for
        /// the stashed result. Called from OnTribulationComplete.</summary>
        private void RefreshOutcomeColors(int pathIndex)
        {
            // Hairlines: gold for ascend, ember for fall.
            Color hairlineColor = _didAscend
                ? Palette.AseGold.WithAlpha(0.25f)
                : Palette.EmberWarm.WithAlpha(0.25f);
            if (_revealHairlineAbove != null) _revealHairlineAbove.color = hairlineColor;
            if (_revealHairlineBelow != null) _revealHairlineBelow.color = hairlineColor;

            // Bar fill: gold gradient for ascend, ember for fall.
            if (_lineageFillImage != null)
            {
                _lineageFillImage.color = _didAscend
                    ? Palette.AseGold.WithAlpha(0.75f)
                    : Palette.EmberWarm.WithAlpha(0.75f);
                _lineageFillImage.fillAmount =
                    Mathf.Clamp01(_lineageFactorAfter / MaxExpectedFactor);
            }

            // Card dot + inner glow: tinted to the ancestor's path color.
            Color pathColor = PathMotif.ColorOf(pathIndex);
            if (_cardInnerGlow != null) _cardInnerGlow.color = pathColor.WithAlpha(0.06f);
            if (_cardPathDot   != null) _cardPathDot.color   = pathColor.WithAlpha(0.70f);
        }
    }
}
