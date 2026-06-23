using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Procedural cold-open launch beat (issue #32, CONTEXT.md "Cold open").
    /// Shows the silhouette kindling from darkness, the game title "Ori Ascendant"
    /// (display/serif voice), the framing proverb, and a single tap-to-enter before
    /// the main screen is visible. Skippable at any point.
    /// Honours Reduce Motion (plain alpha fade instead of the kindling arc).
    ///
    /// Bootstraps via RuntimeInitializeOnLoadMethod — no scene wiring required.
    /// Degrades silently if scene setup fails (skin-pass discipline, ADR-0001).
    /// Seen/skip state is stored in PlayerPrefs via ColdOpenPrefs (NOT SaveData).
    /// </summary>
    public sealed class ColdOpenSkin : MonoBehaviour
    {
        // ---- Layout constants (canvas reference 390 × 844 matching CanvasScaler) ----
        private const float CanvasW = 390f;
        private const float CanvasH = 844f;

        // Silhouette: soft radial bust glow positioned center-upper
        private const float BustCentreX = 0.50f;
        private const float BustCentreY = 0.55f;
        private const float BustSize    = 280f;    // canvas-px diameter of the glow

        // Aura: larger, dimmer halo behind the bust
        private const float AuraCentreX = BustCentreX;
        private const float AuraCentreY = BustCentreY;
        private const float AuraSize    = 420f;

        // Aura secondary alpha multiplier (ponytail: was inline 0.45f literal)
        private const float AuraAlphaMultiplier = 0.45f;

        // Close-out: plain fade after skip()
        private const float CloseDuration = 0.25f;

        // ---- State ----
        private ColdOpenBeat _beat;
        private Image _silhouette;
        private Image _aura;
        private CanvasGroup _titleGroup;    // drives game title alone
        private CanvasGroup _proverbGroup;  // drives proverb + prompt together
        private CanvasGroup _group;         // drives the whole overlay for close-out
        private float _closeElapsed = -1f;  // -1 = not closing yet

        // ---- Bootstrap ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (ColdOpenPrefs.HasSeen) return;

            // Only run when a main canvas is present (not in test scenes).
            if (!HasRootCanvas()) return;

            try
            {
                Build();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ColdOpenSkin] bootstrap failed, skipping cold open: {e.Message}");
            }
        }

        private static bool HasRootCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.isRootCanvas) return true;
            }
            return false;
        }

        private static void Build()
        {
            // Standalone canvas on top of everything (Sort Order 100 is above the
            // default main canvas at 0 but leaves room for system overlays).
            var go = new GameObject("ColdOpenCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<CanvasGroup>();

            // Ensure an EventSystem exists so the tap hits register.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                DontDestroyOnLoad(esGo);
            }

            go.AddComponent<ColdOpenSkin>();
        }

        // ---- Lifecycle ----

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            try
            {
                BuildUI();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ColdOpenSkin] UI build failed, closing: {e.Message}");
                FinishAndDestroy();
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (_closeElapsed >= 0f)
            {
                // Closing: fade the whole overlay to 0 then destroy.
                _closeElapsed += dt;
                float alpha = Mathf.Lerp(1f, 0f,
                    MotionHelper.EaseOut(Mathf.Clamp01(_closeElapsed / CloseDuration)));
                if (_group != null) _group.alpha = alpha;
                if (_closeElapsed >= CloseDuration) FinishAndDestroy();
                return;
            }

            bool reduceMotion = MotionHelper.IsReduceMotion();
            var (silhouette, title, proverb, _) = _beat.Tick(dt, reduceMotion);

            if (_silhouette != null)
                _silhouette.color = Palette.AseGold.WithAlpha(silhouette);
            if (_aura != null)
                _aura.color = Palette.AseDeep.WithAlpha(silhouette * AuraAlphaMultiplier);
            if (_titleGroup != null)
                _titleGroup.alpha = title;
            if (_proverbGroup != null)
                _proverbGroup.alpha = proverb;

            if (_beat.IsDone) BeginClose();
        }

        // ---- Tap-to-enter ----

        internal void HandleTap()
        {
            if (_closeElapsed >= 0f) return; // already closing
            _beat.Skip();
            BeginClose();
        }

        // ---- Internals ----

        private void BeginClose()
        {
            if (_closeElapsed >= 0f) return;
            _closeElapsed = 0f;
        }

        private void FinishAndDestroy()
        {
            ColdOpenPrefs.HasSeen = true;
            PlayerPrefs.Save();
            Destroy(gameObject);
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var dot = ProceduralSprites.BuildDot(128);

            // Two-voice fonts: display (serif) with body-font null-fallback (FontRoleSpec).
            var displayFont = Resources.Load<TMP_FontAsset>(FontRoleSpec.DisplayFontResourcePath);
            var bodyFont    = Resources.Load<TMP_FontAsset>(FontRoleSpec.BodyFontResourcePath);
            var titleFont   = displayFont != null ? displayFont : bodyFont; // ponytail: null-fallback

            // ---- Dark backdrop ----
            var bg = UiBuilder.NewStretchImage("Background", transform);
            bg.color = Palette.IndigoNight;
            bg.raycastTarget = true; // the whole surface is the tap target
            UiBuilder.Stretch(bg.rectTransform);

            // Tap detection lives on the background image via this component.
            bg.gameObject.AddComponent<ColdOpenTapForwarder>().Owner = this;

            // ---- Silhouette: aura (behind) then bust glow ----
            _aura = UiBuilder.NewStretchImage("SilhouetteAura", transform);
            _aura.sprite = dot;
            _aura.raycastTarget = false;
            _aura.color = Color.clear;
            UiBuilder.PlaceAt(_aura.rectTransform, AuraCentreX, AuraCentreY, AuraSize, AuraSize);

            _silhouette = UiBuilder.NewStretchImage("SilhouetteGlow", transform);
            _silhouette.sprite = dot;
            _silhouette.raycastTarget = false;
            _silhouette.color = Color.clear;
            UiBuilder.PlaceAt(_silhouette.rectTransform, BustCentreX, BustCentreY, BustSize, BustSize);

            // ---- Title group: "Ori Ascendant" (display/serif, driven by titleAlpha) ----
            var titleRoot = new GameObject("TitleGroup",
                typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            titleRoot.SetParent(transform, false);
            UiBuilder.Stretch(titleRoot);
            _titleGroup = titleRoot.GetComponent<CanvasGroup>();
            _titleGroup.alpha = 0f;
            _titleGroup.interactable = false;
            _titleGroup.blocksRaycasts = false;

            var title = NewText("GameTitle", "Ori Ascendant",
                TypographicScale.Hero, titleRoot, titleFont);
            title.alignment = TextAlignmentOptions.Center;
            title.color = Palette.AseCore;
            title.characterSpacing = FontRoleSpec.HeroLetterSpacing;
            UiBuilder.SetBand(title.rectTransform, 0.80f, 0.90f); // upper band, clear of the silhouette glow

            // ---- Proverb + prompt group (alpha driven together) ----
            var proverbRoot = new GameObject("ProverbGroup",
                typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            proverbRoot.SetParent(transform, false);
            UiBuilder.Stretch(proverbRoot);
            _proverbGroup = proverbRoot.GetComponent<CanvasGroup>();
            _proverbGroup.alpha = 0f;
            _proverbGroup.interactable = false;
            _proverbGroup.blocksRaycasts = false;

            // Proverb text: body font, Display size, TextSecondary colour, proverb spacing.
            var proverb = NewText("ProverbText",
                "Ayé l'ọjà, ọ̀run nilé\n<size=70%>The world is a marketplace; ọ̀run is home.</size>",
                TypographicScale.Body, proverbRoot, bodyFont);
            proverb.alignment = TextAlignmentOptions.Center;
            proverb.color = Palette.TextSecondary;
            proverb.characterSpacing = FontRoleSpec.ProverbCharacterSpacing;
            UiBuilder.SetBand(proverb.rectTransform, 0.30f, 0.42f);

            // Tap prompt: body font, BodySm size, muted TextPrimary.
            var prompt = NewText("TapPrompt", "Touch to enter",
                TypographicScale.BodySm, proverbRoot, bodyFont);
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.color = Palette.TextPrimary.WithAlpha(0.6f); // quiet but legible (≈WCAG AA on IndigoNight)
            UiBuilder.SetBand(prompt.rectTransform, 0.10f, 0.18f);
        }

        // ---- UGUI helpers ----

        private static TMP_Text NewText(string name, string text, float size,
            RectTransform parent, TMP_FontAsset font)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TMP_Text>();
            t.text = text;
            t.fontSize = size;
            if (font != null) t.font = font;
            t.raycastTarget = false;
            return t;
        }
    }

    /// <summary>
    /// Sits on the backdrop Image and routes pointer clicks to ColdOpenSkin.
    /// Needed because UGUI routes events to the topmost raycast target (the
    /// background Image), not to the Canvas root where ColdOpenSkin lives.
    /// </summary>
    internal sealed class ColdOpenTapForwarder : MonoBehaviour, IPointerClickHandler
    {
        internal ColdOpenSkin Owner;

        public void OnPointerClick(PointerEventData _) => Owner?.HandleTap();
    }
}
