using System.Collections.Generic;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// The procedural cultural skin (ADR 0001): turns the science-project graybox
    /// into the "Painterly Cosmic Myth" look using only engine craft — no imported
    /// art. Self-activates after scene load and decorates the live MainCanvas, so it
    /// ships in a Cloud Build with zero scene wiring.
    ///
    /// Backdrop:
    ///   • Typography — every TMP_Text repointed to NotoSans (dynamic atlas → full
    ///     Yoruba diacritics, ART_BIBLE §7.9); colours role-mapped to the palette
    ///     (designed gold accents stay gold; the Àṣẹ counter is forced gold, §1).
    ///   • Sky — a procedural indigo→gold gradient with a warm horizon glow and a
    ///     faint scatter of ancestor-stars (§5.2).
    ///   • Motes — soft gold Àṣẹ points drifting upward (UGUI Images, not particles:
    ///     particles don't render in a Screen-Space-Overlay canvas).
    ///
    /// Foreground (the graybox that the eye actually lands on):
    ///   • The silhouette of light — a luminous procedural bust in the portrait /
    ///     tap-to-channel zone (§4); the tap target flares warm on touch.
    ///   • The primary CTA — rounded gold face with a soft pulsing glow.
    ///   • The vessel waterline glow — a soft horizontal band riding the fill
    ///     front as the vessel rises (bar removed in issue #28; glow migrated here).
    ///   • The council slots — rounded chips with persistent gold rims (the strip
    ///     view recolours the fill at runtime; rims/sprites survive that).
    ///
    /// Wave 3: tribulation buildup atmosphere — the sky tints toward storm-amber when
    /// the final-stage tribulation fraction exceeds 50%; the effect deepens at 80%
    /// (ART_BIBLE §5.5 "majestic awe, not menace"). Title-screen dress lives in
    /// TitleScreenSkin (the rising Àṣẹ thread + constellation, ART_BIBLE §5.1).
    ///
    /// The one-time FindObjects pass at startup is a skin decoration step, not
    /// system-to-system wiring, so it sits outside the ServiceLocator convention.
    /// </summary>
    public sealed class MainScreenSkin : MonoBehaviour
    {
        private const string RootCanvasName = "MainCanvas";
        private const string FontResourcePath = "Fonts/NotoSans-Regular SDF";

        // ---- Wave 3: tribulation atmosphere (ART_BIBLE §5.5) ----------------
        // Two layers driven from TribulationAtmosphere pure fns:
        //   _stormSkyTint     — fullscreen warm-amber tint (SkyOverlayColor)
        //   _stormEdgeVignette — edge darkening vignette  (VignetteAlpha)
        // Both read _lastProgressFraction (TickVesselFill keeps it live) and stage ≥ 5.
        private Image _stormSkyTint;
        private Image _stormEdgeVignette; // reference to SceneBuilder's StormVignette

        // SceneBuilder's placeholder palette — the three roles we role-map FROM.
        private static readonly Color SceneGold = new Color(0.851f, 0.643f, 0.255f); // #D9A441
        private static readonly Color SceneDim = new Color(0.604f, 0.639f, 0.698f);  // #9AA3B2

        // ---- Wave 2: per-path atmospheric re-theme (ADR 0002) ------------------

        private enum MoteStyle { Neutral, Storm, River, Earth }

        private struct PathTheme
        {
            public Color Horizon, Mote, Aura;
            public MoteStyle Style;
            public PathTheme(Color h, Color m, Color a, MoteStyle s)
            { Horizon = h; Mote = m; Aura = a; Style = s; }
        }

        // Index = currentPath + 1 so -1 (neutral) maps to [0].
        private static readonly PathTheme[] _themes = new PathTheme[]
        {
            // [0] Neutral (no path yet): gold ascent signature
            new PathTheme(Palette.AseGold,        Palette.AseCore,       Palette.AseGold,        MoteStyle.Neutral),
            // [1] Ane (earth/endurance): warm ochre ground, slow ember motes
            new PathTheme(Palette.AneOchre,        Palette.AneMaize,      Palette.AneEarthGreen,  MoteStyle.Earth),
            // [2] Sango (thunder/force): hot amber storm, fast crackling motes
            new PathTheme(Palette.SangoStormAmber, Palette.SangoHotWhite, Palette.SangoStormAmber, MoteStyle.Storm),
            // [3] Osun (river/flow): cool teal current, gentle flowing motes
            new PathTheme(Palette.OsunRiverTeal,   Palette.OsunPale,      Palette.OsunRiverTeal,  MoteStyle.River),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Only skin a scene that actually carries the main canvas — keeps the
            // skin inert in EditMode/PlayMode test scenes that have no UI.
            if (FindRootCanvas() == null) return;
            new GameObject(nameof(MainScreenSkin)).AddComponent<MainScreenSkin>();
        }

        private static Canvas FindRootCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas fallback = null;
            foreach (var c in canvases)
            {
                if (c.name == RootCanvasName) return c;
                if (c.isRootCanvas && fallback == null) fallback = c;
            }
            return fallback;
        }

        private readonly List<Mote> _motes = new List<Mote>();

        // Shared generated sprites (built once at Start).
        private Sprite _dotSprite;       // soft radial dot — auras, stars, motes, leading edge
        private Sprite _roundedBig;      // r16 rounded rect (9-sliced) — button, council chips
        private Sprite _roundedSmall;    // r8 rounded rect (9-sliced) — constellation lines, staff
        private Sprite _ring;            // r16 rounded border (9-sliced) — council rims

        // Animated references (driven in Update).
        private float _pulseT;
        private Button _advanceButton;
        private Image _advanceGlow;
        private Image _vesselWaterlineGlow; // leading-edge glow on the vessel's rising waterline (issue #28)
        private Image _crossingColumn;       // overflow column above the vessel: the Crossing gauge (issue #33)

        // Crossing ceremony (issue #34, PRD W3): column resolves into a new star at the apex.
        // Ignition starts on OnCeremonyClosed (overlay fully down) — not on OnTribulationComplete
        // (which fires while the overlay is still opaque, making the flash invisible). (#4)
        // CeremonyDriver owns the ceremony clock + stashed outcome; the skin keeps the star Image.
        private Image _crossingNewStar;                              // flash image at the column apex during ceremony
        private CeremonyDriver _ceremony;

        private Button _portraitButton; // cached for OnDestroy RemoveListener (#8)

        // The silhouette of light ages with the cultivation stage (ART_BIBLE §4).
        private SaveManager _save;
        private Image _silhouette;
        private int _silhouetteStage = -1;
        private GameObject _constellation; // elder crown (final stage)
        private GameObject _staff;         // elder staff (elder tiers)

        // Per-path state — -2 so the first Update tick always applies (even path=-1).
        private int _currentPath = -2;
        private Image _pathOverlay;     // horizon bloom between sky and motes; tinted per path
        private Image _silhouetteAura;  // soft glow behind the bust; tinted per path
        private Color _themeAccent = Palette.AseGold; // drives advance glow + waterline glow

        // Vessel fill (issue #25, PRD W2): gold-light fill driven by VesselFillRatio.
        private Image _vesselFillImage;
        private CultivationSystem _cultivation;
        private AseGenerationSystem _aseGen;
        private double _lastProgressFraction; // within-stage fraction; read by RefreshTribulationAtmosphere

        // Hero idle breathing (ADR-0005): BreathingDriver owns the breath clock + constants.
        private BreathingDriver _breathing;

        // Micro-feedback motions (issue #24) + hero counter glow (issue #30)
        private TMP_Text _aseCounter;               // the hero Àṣẹ counter — AseFlashDriver watches its text
        private Image _aseCounterGlow;              // faint gold glow behind the hero number
        private AseFlashDriver _aseFlash;           // counter-change flash clock + last-seen value
        private float _silhouettePulseElapsed = float.MaxValue; // large = no active pulse (tap-response)
        private const float SilhouettePulseDuration   = 0.25f;
        private const float SilhouettePulseAmplitude  = 0.04f; // ±4% scale burst

        // Deep field (issue #26): retired ancestors recede into the sky.
        private RectTransform _deepFieldLayer;
        private readonly List<Image> _deepFieldStars = new List<Image>();
        private int _lastDeepFieldCount = -1;

        private void Start()
        {
            // A decorative skin must NEVER break gameplay or the boot smoke test —
            // degrade silently to the base UI on any failure (cf. the cloud-save
            // "always fall through" rule).
            try
            {
                _dotSprite = ProceduralSprites.BuildDot(64);
                _roundedBig = ProceduralSprites.RoundedRect(48, 16f);
                _roundedSmall = ProceduralSprites.RoundedRect(24, 8f);
                _ring = ProceduralSprites.RoundedBorder(48, 16f, 3.5f);

                ThemeText();
                BuildBackground();
                SkinForeground();

                // OnTribulationComplete fires while the tribulation overlay is still opaque.
                // Stash the result there; start the star-ignition only on OnCeremonyClosed (#4).
                if (ServiceLocator.TryGet(out TribulationSystem trib))
                    trib.OnTribulationComplete += OnCeremonyFired;
                if (ServiceLocator.TryGet(out TribulationScreen tribScreen))
                    tribScreen.OnCeremonyClosed += StartCeremony;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainScreenSkin] skin pass failed, leaving base UI: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out TribulationSystem trib))
                trib.OnTribulationComplete -= OnCeremonyFired;
            if (ServiceLocator.TryGet(out TribulationScreen tribScreen))
                tribScreen.OnCeremonyClosed -= StartCeremony;
            if (_portraitButton != null)
                _portraitButton.onClick.RemoveListener(OnPortraitTapped);
        }

        // Stash result from the tribulation event (fires while overlay is still opaque).
        private void OnCeremonyFired(bool didAscend, AncestorData ancestor)
        {
            _ceremony.Stash(didAscend, ancestor?.path ?? -1);
        }

        // Start ignition only after the overlay fully closes so the flash is visible (#4).
        private void StartCeremony() => _ceremony.Start();

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _pulseT += dt;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseT * 2.2f); // 0..1, ~0.35Hz

            // Age the silhouette when the cultivation stage changes (rare → rebuild).
            int stage = CurrentStage();
            if (stage != _silhouetteStage && _silhouette != null)
            {
                _silhouetteStage = stage;
                RebuildSilhouette(stage);
            }

            // Re-theme when the player picks a path (rare; stays -1 until the Tier-1 gate).
            int path = CurrentPath();
            if (path != _currentPath)
            {
                _currentPath = path;
                ApplyPathTheme(path);
            }

            for (int i = 0; i < _motes.Count; i++) _motes[i].Tick(dt);

            RefreshTribulationAtmosphere();
            TickMicroFeedback(dt);
            RefreshDeepField();
            TickBreathing(dt);
            TickVesselFill();
            TickCrossingColumn(pulse);
            TickCeremony(dt);

            // CTA glow breathes only while advancing is actually possible.
            if (_advanceGlow != null)
            {
                bool ready = _advanceButton == null || _advanceButton.interactable;
                float a = ready ? 0.16f + 0.22f * pulse : 0f;
                _advanceGlow.color = _themeAccent.WithAlpha(a);
            }

            // Waterline glow rides the rising fill front of the vessel (issue #28).
            if (_vesselWaterlineGlow != null && _vesselFillImage != null)
            {
                float f = _vesselFillImage.fillAmount;
                var rt = _vesselWaterlineGlow.rectTransform;
                rt.anchorMin = new Vector2(0f, f);
                rt.anchorMax = new Vector2(1f, f);
                rt.anchoredPosition = Vector2.zero;
                float vis = (f > 0.015f && f < 0.995f) ? 1f : 0f;
                _vesselWaterlineGlow.color = _themeAccent.WithAlpha(vis * (0.55f + 0.35f * pulse));
            }
        }

        // ================= typography =================

        private void ThemeText()
        {
            var font = Resources.Load<TMP_FontAsset>(FontResourcePath);
            var texts = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            TMP_Text hero = null;
            float heroSize = 0f;
            foreach (var t in texts)
            {
                if (font != null) t.font = font; // dynamic atlas renders full diacritics

                // Role-map by source colour so designed accents survive: gold stays
                // gold, dim stays dim, dark button labels stay dark (high contrast on
                // the gold CTA), everything else becomes warm bone-white.
                Color c = t.color;
                if (Approx(c, SceneGold)) t.color = Palette.AseGold;
                else if (Approx(c, SceneDim)) t.color = Palette.TextSecondary;
                else if (Luma(c) < 0.25f) t.color = Palette.IndigoNight;
                else t.color = Palette.TextPrimary;

                // The Àṣẹ counter is the largest *active* number on screen; reserve
                // the sacred gold for it regardless of its source colour. (Inactive
                // modal titles are ignored so a hidden header can't steal it.)
                if (t.gameObject.activeInHierarchy && t.fontSize > heroSize)
                {
                    heroSize = t.fontSize;
                    hero = t;
                }
            }
            if (hero != null)
            {
                hero.color = Palette.AseGold;
                AddHeroCounterGlow(hero); // faint glow behind the number (issue #30)
            }
            _aseCounter = hero; // owned by TickMicroFeedback for the flash animation
        }

        /// <summary>Adds a soft radial glow image as a sibling behind the hero
        /// Àṣẹ counter (issue #30, PRD W4). The glow is structural — set once at
        /// Start, never animated — to keep the number luminous without a heavy disc.</summary>
        private void AddHeroCounterGlow(TMP_Text counter)
        {
            if (_dotSprite == null || counter.transform.parent == null) return;
            _aseCounterGlow = UiBuilder.NewChildImage(counter.transform.parent, "AseCounterGlow");
            _aseCounterGlow.sprite = _dotSprite;
            _aseCounterGlow.color = Palette.AseGold.WithAlpha(AseHeroSpec.HeroGlowAlpha);
            // Match the counter's anchored position and expand by padding.
            var brt = counter.rectTransform;
            var grt = _aseCounterGlow.rectTransform;
            grt.anchorMin = brt.anchorMin;
            grt.anchorMax = brt.anchorMax;
            float pad = AseHeroSpec.HeroGlowPadding;
            grt.offsetMin = brt.offsetMin + new Vector2(-pad, -pad);
            grt.offsetMax = brt.offsetMax + new Vector2(pad, pad);
            // Place behind the text: insert just before the counter in sibling order.
            int idx = brt.GetSiblingIndex();
            grt.SetSiblingIndex(idx > 0 ? idx - 1 : 0);
        }

        private static bool Approx(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.06f && Mathf.Abs(a.g - b.g) < 0.06f && Mathf.Abs(a.b - b.b) < 0.06f;

        private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // ================= background + motes =================

        private void BuildBackground()
        {
            var canvas = FindRootCanvas();
            if (canvas == null) return;

            var sky = UiBuilder.NewStretchImage("SkyBackground", canvas.transform);
            sky.sprite = ProceduralSprites.BuildSky(180, 360);
            sky.color = Color.white;
            sky.rectTransform.SetSiblingIndex(0); // behind every existing UI element

            // Path-accent horizon bloom — a soft elliptical glow at the bottom of the
            // sky that tints per path (gold neutral / ochre earth / amber storm / teal river).
            _pathOverlay = UiBuilder.NewStretchImage("PathOverlay", canvas.transform);
            _pathOverlay.sprite = _dotSprite;
            var por = _pathOverlay.rectTransform;
            por.anchorMin = new Vector2(0f, 0f);
            por.anchorMax = new Vector2(1f, 0.50f);
            por.offsetMin = por.offsetMax = Vector2.zero;
            _pathOverlay.color = Color.clear; // ApplyPathTheme() sets this on first Update tick
            _pathOverlay.rectTransform.SetSiblingIndex(1); // above sky, below motes

            // Ancestor-stars: a sparse, faint scatter in the upper sky.
            AddStar(sky.rectTransform, 0.16f, 0.90f, 5f, 0.50f);
            AddStar(sky.rectTransform, 0.34f, 0.94f, 4f, 0.35f);
            AddStar(sky.rectTransform, 0.52f, 0.89f, 5f, 0.50f);
            AddStar(sky.rectTransform, 0.70f, 0.93f, 4f, 0.40f);
            AddStar(sky.rectTransform, 0.84f, 0.87f, 4f, 0.30f);
            AddStar(sky.rectTransform, 0.24f, 0.84f, 4f, 0.30f);
            AddStar(sky.rectTransform, 0.62f, 0.82f, 5f, 0.45f);
            AddStar(sky.rectTransform, 0.45f, 0.96f, 4f, 0.30f);

            // Deep-field layer — retired ancestors recede here (issue #26).
            // Stars are added dynamically in RefreshDeepField() as generations complete.
            var dfLayer = UiBuilder.NewStretchImage("DeepFieldLayer", sky.rectTransform);
            dfLayer.color = Color.clear;
            _deepFieldLayer = dfLayer.rectTransform;

            // Drifting Àṣẹ motes: soft gold points above the sky, below the UI.
            var moteLayer = UiBuilder.NewStretchImage("MoteLayer", canvas.transform);
            moteLayer.color = Color.clear;
            moteLayer.rectTransform.SetSiblingIndex(2);

            // Wave 3 — storm-sky tint: a warm amber-dark overlay, transparent until
            // the tribulation fraction exceeds 50% at the final stage.
            _stormSkyTint = UiBuilder.NewStretchImage("StormSkyTint", canvas.transform);
            _stormSkyTint.sprite = _dotSprite; // soft radial falloff; stretched full-screen
            _stormSkyTint.color = Color.clear; // RefreshTribulationAtmosphere() drives this
            _stormSkyTint.rectTransform.SetSiblingIndex(3); // above motes, below all UI zones

            // Wave 3 — edge vignette: the skin takes over the SceneBuilder-built
            // StormVignette Image so TribulationAtmosphere.VignetteAlpha() drives
            // it (same step values as the controller stub, now from the named fn).
            _stormEdgeVignette = UiBuilder.FindComp<Image>(canvas.transform, "StormVignette");
            const int count = 10;
            for (int i = 0; i < count; i++)
                _motes.Add(Mote.Create(moteLayer.rectTransform, _dotSprite, i / (float)count));
        }

        private void AddStar(RectTransform parent, float nx, float ny, float size, float alpha)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(nx, ny);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = _dotSprite;
            img.raycastTarget = false;
            img.color = Palette.WarmEcru.WithAlpha(alpha);
        }

        // ================= foreground skin =================

        private void SkinForeground()
        {
            var canvas = FindRootCanvas();
            if (canvas == null) return;
            var root = canvas.transform;

            SkinPortrait(root);
            SkinAdvance(root);
            SkinCouncil(root);
            SkinButtons(root);
            SkinChrome(root);
        }

        /// <summary>Minimalist chrome pass (issue #30, PRD W4): flatten secondary
        /// chrome elements so the Àṣẹ counter reads as the sole luminous element.
        /// Modal panels are untouched — they need their backgrounds for legibility
        /// as overlays. Only persistent main-screen chrome is flattened here.</summary>
        private static void SkinChrome(Transform root)
        {
            // Settings button: icon-only — clear the filled panel background so
            // the ⚙ glyph floats on the sky without a heavy dark rectangle behind it.
            var settingsImg = UiBuilder.FindComp<Image>(root, "SettingsButton");
            if (settingsImg != null)
                settingsImg.color = Color.clear;
        }

        /// <summary>The portrait Image is a transparent raycast-only hit-area; all
        /// visible light (aura, silhouette, motes) comes from child Images.</summary>
        private void SkinPortrait(Transform root)
        {
            var portrait = UiBuilder.FindComp<Image>(root, "PortraitImage");
            if (portrait == null) return;

            // Path-accent aura behind the bust — a soft glow that tints per path.
            _silhouetteAura = UiBuilder.NewChildImage(portrait.rectTransform, "SilhouetteAura");
            var art = _silhouetteAura.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(310f, 310f); // slightly larger than the bust
            art.anchoredPosition = Vector2.zero;
            _silhouetteAura.sprite = _dotSprite;
            _silhouetteAura.color = Color.clear; // ApplyPathTheme() sets this

            _silhouette = UiBuilder.NewChildImage(portrait.rectTransform, "SilhouetteOfLight");
            var srt = _silhouette.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(250f, 250f); // SQUARE → the bust texture isn't vertically stretched
            srt.anchoredPosition = new Vector2(0f, 0f);

            // Vessel fill: bright gold light that rises from the feet as Àṣẹ accrues.
            // Must be the first child so constellation/staff render above it.
            _vesselFillImage = UiBuilder.NewChildImage(srt, "VesselFill");
            _vesselFillImage.type = Image.Type.Filled;
            _vesselFillImage.fillMethod = Image.FillMethod.Vertical;
            _vesselFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            _vesselFillImage.fillAmount = 0f;
            _vesselFillImage.color = Palette.AseCore;

            // Waterline glow: horizontal soft-dot at the fill front, rides upward with the fill.
            // Replaces the old bar's leading-edge glow (issue #28). Positioned via anchor-Y each frame.
            _vesselWaterlineGlow = UiBuilder.NewChildImage(srt, "VesselWaterlineGlow");
            _vesselWaterlineGlow.sprite = _dotSprite;
            var wrt = _vesselWaterlineGlow.rectTransform;
            wrt.anchorMin = new Vector2(0f, 0f);
            wrt.anchorMax = new Vector2(1f, 0f);
            wrt.pivot = new Vector2(0.5f, 0.5f);
            wrt.sizeDelta = new Vector2(0f, 14f); // full-width band; height is the glow falloff
            _vesselWaterlineGlow.color = Palette.AseCore.WithAlpha(0f);

            // Overflow column: rises above the vessel at the final stage as the
            // Crossing gauge — replaces the removed bar for tribulation (issue #33, PRD W2).
            _crossingColumn = UiBuilder.NewChildImage(srt, "CrossingColumn");
            _crossingColumn.sprite = _dotSprite;
            var ccrt = _crossingColumn.rectTransform;
            ccrt.anchorMin = new Vector2(0.5f, 1f);
            ccrt.anchorMax = new Vector2(0.5f, 1f);
            ccrt.pivot = new Vector2(0.5f, 0f); // grows upward from the vessel top
            ccrt.sizeDelta = new Vector2(CrossingColumnSpec.ColumnWidth, 0f);
            _crossingColumn.color = Color.clear;

            // Ceremony new-star: flashes at the column apex when the Crossing resolves (issue #34).
            _crossingNewStar = UiBuilder.NewChildImage(srt, "CrossingNewStar");
            _crossingNewStar.sprite = _dotSprite;
            var nsrt = _crossingNewStar.rectTransform;
            nsrt.anchorMin = nsrt.anchorMax = new Vector2(0.5f, 1f); // above the silhouette top, same as column
            nsrt.pivot = new Vector2(0.5f, 0.5f);
            nsrt.sizeDelta = new Vector2(12f, 12f);
            nsrt.anchoredPosition = new Vector2(0f, CrossingColumnSpec.MaxColumnHeight); // at column apex
            _crossingNewStar.color = Color.clear;

            BuildConstellation(srt); // elder crown — toggled on at the final stage
            BuildStaff(srt);         // elder staff — toggled on at the elder tiers
            // The bust sprite itself is built on the first Update tick, once the real
            // cultivation stage is known — avoids a stage-0 flash before the save loads.

            // Wire tap-pulse: restart the pulse when the portrait button is tapped.
            _portraitButton = portrait.GetComponent<Button>();
            if (_portraitButton != null)
                _portraitButton.onClick.AddListener(OnPortraitTapped);
        }

        /// <summary>Flat amber rectangle → rounded gold face, dark label, soft glow.</summary>
        private void SkinAdvance(Transform root)
        {
            var advance = UiBuilder.FindDeep(root, "AdvanceButton");
            if (advance == null) return;

            var img = advance.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = _roundedBig;
                img.type = Image.Type.Sliced;
                img.color = Palette.AseGold;
            }
            _advanceButton = advance.GetComponent<Button>();

            var label = advance.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = Palette.IndigoNight;

            // Glow sits behind the button (earlier sibling in the same zone).
            var brt = (RectTransform)advance;
            _advanceGlow = UiBuilder.NewChildImage(advance.parent, "AdvanceGlow");
            _advanceGlow.sprite = _dotSprite;
            var grt = _advanceGlow.rectTransform;
            grt.anchorMin = brt.anchorMin;
            grt.anchorMax = brt.anchorMax;
            grt.offsetMin = brt.offsetMin + new Vector2(-18f, -16f);
            grt.offsetMax = brt.offsetMax + new Vector2(18f, 16f);
            _advanceGlow.color = Palette.AseGold.WithAlpha(0.2f);
            _advanceGlow.rectTransform.SetSiblingIndex(0);
        }

        /// <summary>Five flat squares → ancestor-star dots (issue #22). Each slot
        /// becomes a soft radial dot so the strip reads as a constellation, not a
        /// chip row. CouncilStripView drives .color at runtime via
        /// ConstellationStarMapper; the dot sprite and Simple type survive that.</summary>
        private void SkinCouncil(Transform root)
        {
            for (int i = 1; i <= 5; i++)
            {
                var slot = UiBuilder.FindComp<Image>(root, $"CouncilSlot{i}");
                if (slot == null) continue;
                slot.sprite = _dotSprite;
                slot.type = Image.Type.Simple;
                // Initial colour: empty-seat faint point; CouncilStripView overwrites
                // this each council Version tick so the star reflects real data.
                slot.color = ConstellationStarMapper.EmptySeatColor();
            }
        }

        private void OnPortraitTapped() => _silhouettePulseElapsed = 0f;

        /// <summary>Attaches ButtonPressDip to every Button in the canvas so all
        /// interactive elements share the same press-dip tactile response.</summary>
        private static void SkinButtons(Transform root)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
                if (btn.GetComponent<ButtonPressDip>() == null)
                    btn.gameObject.AddComponent<ButtonPressDip>();
        }

        // ================= deep field: growing bloodline sky (issue #26) =================

        /// <summary>Adds retiring-ancestor stars as the bloodline grows. Chronicle entries
        /// only accumulate; stars are added monotonically, never removed. Positions are
        /// deterministic (golden-ratio scatter) so no Random is needed.</summary>
        private void RefreshDeepField()
        {
            if (_save == null) ServiceLocator.TryGet(out _save);
            var save = _save?.Current;
            if (save == null || _deepFieldLayer == null || _dotSprite == null) return;

            int needed = ConstellationStarMapper.DeepFieldStarCount(save);
            if (needed == _lastDeepFieldCount) return;

            for (int i = _deepFieldStars.Count; i < needed; i++)
            {
                bool ascended = i < save.chronicle.Count && save.chronicle[i].didAscend;
                _deepFieldStars.Add(AddDeepFieldStar(_deepFieldLayer, i, ascended));
            }
            _lastDeepFieldCount = needed;
        }

        private Image AddDeepFieldStar(RectTransform parent, int index, bool didAscend)
        {
            // Golden-ratio scatter: avoids clustering without needing Random.
            float nx = Mathf.Repeat(index * 0.618034f, 1f);
            float ny = 0.72f + Mathf.Repeat(index * 0.317f, 0.22f);

            var go = new GameObject($"DeepStar{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(nx, ny);
            rt.sizeDelta = new Vector2(3f, 3f); // smaller than council stars — feels more distant
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = _dotSprite;
            img.raycastTarget = false;
            img.color = ConstellationStarMapper.DeepFieldStarColor(didAscend);
            return img;
        }

        // ================= Wave 3: tribulation atmosphere =================

        /// <summary>Drives both storm atmosphere layers from the tribulation fraction.
        /// Fraction sourced from _lastProgressFraction (TickVesselFill keeps it live).
        /// At stages 0-4 fraction stays 0 so both layers remain transparent.</summary>
        private void RefreshTribulationAtmosphere()
        {
            double fraction = CurrentStage() >= 5 ? _lastProgressFraction : 0.0;

            if (_stormSkyTint != null)
                _stormSkyTint.color = TribulationAtmosphere.SkyOverlayColor(fraction);

            if (_stormEdgeVignette != null)
            {
                var c = _stormEdgeVignette.color;
                c.a = TribulationAtmosphere.VignetteAlpha(fraction);
                _stormEdgeVignette.color = c;
            }
        }

        // ================= hero idle breathing (ADR-0005) =================

        /// <summary>Drives a slow scale + brightness sine on the silhouette of light.
        /// When iOS Reduce Motion is on, both channels are silenced (MotionHelper
        /// returns 0) so the bust is perfectly still. The tap-pulse scale (issue #24)
        /// is multiplied in so both motions compose without clamping. VesselFill is a
        /// child of the silhouette so it inherits the scale; its color is pulsed here too.</summary>
        private void TickBreathing(float dt)
        {
            if (_silhouette == null) return;
            bool rm = IsReduceMotion();
            float tapPulse = MotionHelper.TapPulseScale(
                _silhouettePulseElapsed, SilhouettePulseDuration, SilhouettePulseAmplitude, rm);
            var (scale, bright) = _breathing.Tick(dt, tapPulse, rm);
            _silhouette.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _silhouette.color = new Color(bright, bright, bright, 1f);
            // Vessel fill pulses with the same rhythm — AseCore tinted by breathing.
            if (_vesselFillImage != null)
            {
                Color fc = Palette.AseCore;
                _vesselFillImage.color = new Color(fc.r * bright, fc.g * bright, fc.b * bright, 1f);
            }
        }

        /// <summary>Ticks all micro-feedback timers and applies their visual outputs
        /// (issue #24): silhouette pulse elapsed, Àṣẹ counter flash on value change.</summary>
        private void TickMicroFeedback(float dt)
        {
            _silhouettePulseElapsed += dt; // tap-response pulse clock (consumed by TickBreathing)

            if (_aseCounter == null) return;

            float alpha = _aseFlash.Tick(dt, _aseCounter.text, IsReduceMotion());
            _aseCounter.color = Color.Lerp(Palette.AseGold, Palette.AseCore, alpha);
        }

        // ================= vessel fill (issue #25, PRD W2) =================

        /// <summary>Drives the vessel fill Image from the monotonic fill ratio.
        /// Fill rises continuously as Àṣẹ accrues and never recedes across stage
        /// boundaries (guaranteed by VesselFillRatio.Compute). Stores the within-stage
        /// fraction so RefreshTribulationAtmosphere can read it without a bar.</summary>
        private void TickVesselFill()
        {
            if (_vesselFillImage == null) return;
            if (_cultivation == null) ServiceLocator.TryGet(out _cultivation);
            if (_aseGen == null) ServiceLocator.TryGet(out _aseGen);
            if (_cultivation == null || _aseGen == null) return;

            BigNumber target = _cultivation.CurrentTarget;
            double progressFraction = target.IsZero
                ? 0.0
                : (_aseGen.CurrentAse / target).ToDouble();

            _lastProgressFraction = progressFraction;
            _vesselFillImage.fillAmount =
                VesselFillRatio.Compute(CurrentStage(), progressFraction, _cultivation.StageCount);
        }

        // ================= crossing ceremony: vessel light ignites a new star (issue #34) =================

        /// <summary>Drives the ceremony flash when the Crossing resolves. The overflow column
        /// fades out while a new star kindles at the apex — Ascended = bright path colour,
        /// fallen = warm ember. Once the flash settles, the star becomes the new council
        /// near-star (gens 1–5) or a deep-field star (gen 6+); the council strip refreshes
        /// after the ceremony so the new near-star is present. (#10)</summary>
        private void TickCeremony(float dt)
        {
            bool active = _ceremony.Tick(dt, out float starAlpha, out Color starBase, out float columnExitAlpha);

            if (_crossingNewStar == null) return;

            if (!active)
            {
                _crossingNewStar.color = Color.clear;
                return;
            }

            // New-star ignition flash; the column hands its light up into the star.
            _crossingNewStar.color = starBase.WithAlpha(starAlpha);
            if (_crossingColumn != null)
                _crossingColumn.color = _themeAccent.WithAlpha(columnExitAlpha);
        }

        // ================= crossing column: overflow gauge at final stage (issue #33) =================

        /// <summary>Drives the overflow column from the tribulation fraction. The column
        /// is invisible at stages 0-4; at stage 5 it rises from zero to full height as
        /// the Àṣẹ threshold is approached, reaching its apex at tribulation eligibility.
        /// Glow breathes with the main pulse so it reads as alive, not static.</summary>
        private void TickCrossingColumn(float pulse)
        {
            if (_crossingColumn == null) return;
            // During the ceremony, TickCeremony owns the column's exit fade.
            if (_ceremony.IsActive) return;
            if (!CrossingColumnSpec.IsActive(CurrentStage()))
            {
                _crossingColumn.color = Color.clear;
                return;
            }

            float height = CrossingColumnSpec.ColumnHeight(_lastProgressFraction);
            float alpha = CrossingColumnSpec.ColumnAlpha(_lastProgressFraction);

            var crt = _crossingColumn.rectTransform;
            crt.sizeDelta = new Vector2(CrossingColumnSpec.ColumnWidth, height);
            _crossingColumn.color = _themeAccent.WithAlpha(alpha * (0.55f + 0.45f * pulse));
        }

        private static bool IsReduceMotion() => MotionPrefs.ReduceMotionEnabled;

        // ================= silhouette aging =================

        private int CurrentStage()
        {
            if (_save == null) ServiceLocator.TryGet(out _save);
            var data = _save != null ? _save.Current : null;
            return data != null ? data.currentStage : 0;
        }

        private int CurrentPath()
        {
            if (_save == null) ServiceLocator.TryGet(out _save);
            var data = _save != null ? _save.Current : null;
            return data != null ? data.currentPath : -1;
        }

        /// <summary>Re-tints every atmospheric layer to the chosen path in one pass.
        /// Called only when the path changes — typically once per generation.</summary>
        private void ApplyPathTheme(int path)
        {
            var t = _themes[Mathf.Clamp(path + 1, 0, _themes.Length - 1)];

            if (_pathOverlay != null)
                _pathOverlay.color = t.Horizon.WithAlpha(0.30f);

            if (_silhouetteAura != null)
                _silhouetteAura.color = t.Aura.WithAlpha(0.24f);

            _themeAccent = t.Aura;

            for (int i = 0; i < _motes.Count; i++)
                _motes[i].SetTheme(t.Mote, t.Style);
        }

        private void RebuildSilhouette(int stage)
        {
            var sprite = ProceduralSprites.BuildBust(256, ProceduralSprites.ProfileForStage(stage));
            var old = _silhouette.sprite;
            _silhouette.sprite = sprite;
            // VesselFill shares the same sprite so the fill is clipped to the bust shape.
            if (_vesselFillImage != null) _vesselFillImage.sprite = sprite;
            if (old != null)
            {
                if (old.texture != null) Destroy(old.texture);
                Destroy(old);
            }
            if (_constellation != null) _constellation.SetActive(stage >= 5); // Aṣẹ́gun
            if (_staff != null) _staff.SetActive(stage >= 4);                 // Àgbà + Aṣẹ́gun
        }

        private void BuildConstellation(RectTransform parent)
        {
            var go = new GameObject("Constellation", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.99f); // just above the head
            rt.sizeDelta = new Vector2(150f, 54f);
            rt.anchoredPosition = Vector2.zero;
            _constellation = go;

            Vector2[] pts =
            {
                new Vector2(-60f, -8f),
                new Vector2(-20f, 14f),
                new Vector2(20f, 14f),
                new Vector2(60f, -8f),
            };
            for (int i = 0; i < pts.Length - 1; i++) AddConstellationLine(rt, pts[i], pts[i + 1]);
            foreach (var p in pts) AddConstellationStar(rt, p);
            go.SetActive(false);
        }

        private void AddConstellationStar(RectTransform parent, Vector2 pos)
        {
            var img = UiBuilder.NewChildImage(parent, "CStar");
            img.sprite = _dotSprite;
            img.color = Palette.AseCore;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8f, 8f);
            rt.anchoredPosition = pos;
        }

        private void AddConstellationLine(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = UiBuilder.NewChildImage(parent, "CLine");
            img.sprite = _roundedSmall;
            img.type = Image.Type.Sliced;
            img.color = Palette.AseGold.WithAlpha(0.55f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 d = b - a;
            rt.sizeDelta = new Vector2(d.magnitude, 2.5f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        private void BuildStaff(RectTransform parent)
        {
            var img = UiBuilder.NewChildImage(parent, "Staff");
            img.sprite = _roundedSmall;
            img.type = Image.Type.Sliced;
            img.color = Palette.AseDeep;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.92f, 0.40f); // right of the figure
            rt.sizeDelta = new Vector2(6f, 150f);
            rt.anchoredPosition = Vector2.zero;
            _staff = img.gameObject;
            _staff.SetActive(false);
        }

        /// <summary>One drifting Àṣẹ mote: rises through the portrait zone, fading in
        /// and out, then loops. Positioned by anchor so it is resolution-independent.</summary>
        private sealed class Mote
        {
            private RectTransform _rt;
            private Image _img;
            private float _x, _t, _speed;
            private Color _col = Palette.AseCore;
            private MoteStyle _style = MoteStyle.Neutral;

            public static Mote Create(RectTransform parent, Sprite dot, float phase)
            {
                var go = new GameObject("Mote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.sizeDelta = new Vector2(9f, 9f);
                var img = go.GetComponent<Image>();
                img.sprite = dot;
                img.raycastTarget = false;
                img.color = Palette.AseCore;
                return new Mote
                {
                    _rt = rt,
                    _img = img,
                    _x = Random.Range(0.30f, 0.70f),
                    _t = phase,
                    _speed = Random.Range(0.09f, 0.15f),
                };
            }

            public void SetTheme(Color col, MoteStyle style)
            {
                _col = col;
                _style = style;
            }

            public void Tick(float dt)
            {
                // Storm motes crackle fast; earth motes barely lift off the ground.
                float speedScale = _style == MoteStyle.Storm ? 1.7f
                                 : _style == MoteStyle.Earth ? 0.50f
                                 : 1f;
                _t += dt * _speed * speedScale;
                float frac = _t - Mathf.Floor(_t);

                // Per-path horizontal character: storm = erratic jitter, river = gentle S-drift.
                float xOff = _style == MoteStyle.Storm ? Mathf.Sin(_t * 7.1f + _x * 11f) * 0.038f
                           : _style == MoteStyle.River ? Mathf.Sin(_t * 1.7f + _x * 4.8f) * 0.018f
                           : 0f;

                // Earth barely rises; storm bursts high.
                float yMin = 0.42f;
                float yMax = _style == MoteStyle.Storm ? 0.80f
                           : _style == MoteStyle.Earth ? 0.56f
                           : 0.72f;
                float y = Mathf.Lerp(yMin, yMax, frac);

                _rt.anchorMin = _rt.anchorMax = new Vector2(Mathf.Clamp01(_x + xOff), y);
                _rt.anchoredPosition = Vector2.zero;

                float peakA = _style == MoteStyle.Earth ? 0.55f : 0.90f;
                float a = Mathf.Sin(frac * Mathf.PI) * peakA;
                _img.color = _col.WithAlpha(a);
            }
        }
    }
}
