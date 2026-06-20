using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Procedural cold-open launch beat (issue #32, CONTEXT.md "Cold open").
    /// Shows the silhouette kindling from darkness, the framing proverb, and a
    /// single tap-to-enter before the main screen is visible. Skippable at any
    /// point. Honours Reduce Motion (plain alpha fade instead of the kindling arc).
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
        private const float BustSize = 280f;    // canvas-px diameter of the glow

        // Aura: larger, dimmer halo behind the bust
        private const float AuraCentreX = BustCentreX;
        private const float AuraCentreY = BustCentreY;
        private const float AuraSize = 420f;

        // Close-out: plain fade after skip()
        private const float CloseDuration = 0.25f;

        // ---- State ----
        private ColdOpenBeat _beat;
        private Image _silhouette;
        private Image _aura;
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
            var (silhouette, proverb, _) = _beat.Tick(dt, reduceMotion);

            if (_silhouette != null)
                _silhouette.color = Palette.AseGold.WithAlpha(silhouette);
            if (_aura != null)
                _aura.color = Palette.AseDeep.WithAlpha(silhouette * 0.45f);
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

            var dot = BuildDotSprite(128);
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-Regular SDF");

            // ---- Dark backdrop ----
            var bg = NewImage("Background", transform);
            bg.color = Palette.IndigoNight;
            bg.raycastTarget = true; // the whole surface is the tap target
            Stretch(bg.rectTransform);

            // Tap detection lives on the background image via this component.
            bg.gameObject.AddComponent<ColdOpenTapForwarder>().Owner = this;

            // ---- Silhouette: aura (behind) then bust glow ----
            _aura = NewImage("SilhouetteAura", transform);
            _aura.sprite = dot;
            _aura.raycastTarget = false;
            _aura.color = Color.clear;
            PlaceAt(_aura.rectTransform, AuraCentreX, AuraCentreY, AuraSize, AuraSize);

            _silhouette = NewImage("SilhouetteGlow", transform);
            _silhouette.sprite = dot;
            _silhouette.raycastTarget = false;
            _silhouette.color = Color.clear;
            PlaceAt(_silhouette.rectTransform, BustCentreX, BustCentreY, BustSize, BustSize);

            // ---- Proverb + prompt group (alpha driven together) ----
            var proverbRoot = new GameObject("ProverbGroup",
                typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            proverbRoot.SetParent(transform, false);
            Stretch(proverbRoot);
            _proverbGroup = proverbRoot.GetComponent<CanvasGroup>();
            _proverbGroup.alpha = 0f;
            _proverbGroup.interactable = false;
            _proverbGroup.blocksRaycasts = false;

            var proverb = NewText("ProverbText",
                "Ayé l'ọjà, ọ̀run nilé\n<size=70%>The world is a marketplace; ọ̀run is home.</size>",
                16f, proverbRoot, font);
            proverb.alignment = TextAlignmentOptions.Center;
            proverb.color = Palette.TextSecondary;
            SetBand(proverb.rectTransform, 0.30f, 0.42f);

            var prompt = NewText("TapPrompt", "Touch to enter",
                15f, proverbRoot, font);
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.color = Palette.TextPrimary.WithAlpha(0.6f);
            SetBand(prompt.rectTransform, 0.10f, 0.18f);
        }

        // ---- UGUI helpers ----

        private static Image NewImage(string name, Transform parent)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

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

        /// <summary>Anchor + size a rect at a normalised screen position.</summary>
        private static void PlaceAt(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Horizontal-band anchor: full width, clamped y-band.</summary>
        private static void SetBand(RectTransform rt, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(0.05f, yMin);
            rt.anchorMax = new Vector2(0.95f, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Soft radial dot sprite (mirrors TitleScreenSkin.BuildDotSprite).</summary>
        private static Sprite BuildDotSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / r);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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
