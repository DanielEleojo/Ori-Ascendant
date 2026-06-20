using System.Collections.Generic;
using OriAscendant.Core;
using OriAscendant.Save;
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
    ///   • The progress bar — rounded indigo trough, vertical-gradient "liquid gold"
    ///     fill, a bright leading edge that tracks the fill front.
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
        // Both read _barFill.fillAmount (controller keeps it live) and stage ≥ 5.
        private Image _stormSkyTint;
        private Image _stormEdgeVignette; // reference to SceneBuilder's StormVignette

        // SceneBuilder's placeholder palette — the three roles we role-map FROM.
        private static readonly Color SceneGold = new Color(0.851f, 0.643f, 0.255f); // #D9A441
        private static readonly Color SceneDim = new Color(0.604f, 0.639f, 0.698f);  // #9AA3B2

        // ---- Wave 2: per-path atmospheric re-theme (ADR 0002) ------------------

        private enum MoteStyle { Neutral, Storm, River, Earth }

        private struct PathTheme
        {
            public Color Horizon, Mote, BarBot, BarTop, Aura;
            public MoteStyle Style;
            public PathTheme(Color h, Color m, Color bb, Color bt, Color a, MoteStyle s)
            { Horizon = h; Mote = m; BarBot = bb; BarTop = bt; Aura = a; Style = s; }
        }

        // Index = currentPath + 1 so -1 (neutral) maps to [0].
        private static readonly PathTheme[] _themes = new PathTheme[]
        {
            // [0] Neutral (no path yet): gold ascent signature
            new PathTheme(Palette.AseGold,        Palette.AseCore,       Palette.AseDeep,         Palette.AseCore,       Palette.AseGold,        MoteStyle.Neutral),
            // [1] Ane (earth/endurance): warm ochre ground, slow ember motes
            new PathTheme(Palette.AneOchre,        Palette.AneMaize,      Palette.AneClay,         Palette.AneOchre,      Palette.AneEarthGreen,  MoteStyle.Earth),
            // [2] Sango (thunder/force): hot amber storm, fast crackling motes
            new PathTheme(Palette.SangoStormAmber, Palette.SangoHotWhite, Palette.SangoThunderRed, Palette.SangoStormAmber, Palette.SangoStormAmber, MoteStyle.Storm),
            // [3] Osun (river/flow): cool teal current, gentle flowing motes
            new PathTheme(Palette.OsunRiverTeal,   Palette.OsunPale,      Palette.OsunRiverTeal,   Palette.OsunPale,      Palette.OsunRiverTeal,  MoteStyle.River),
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
        private Sprite _roundedSmall;    // r8 rounded rect (9-sliced) — bar trough
        private Sprite _ring;            // r16 rounded border (9-sliced) — council rims

        // Animated references (driven in Update).
        private float _pulseT;
        private Button _advanceButton;
        private Image _advanceGlow;
        private Image _barFill;
        private Image _barLeadingEdge;

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
        private Texture2D _barFillTex;  // tracked so it can be destroyed on re-theme
        private Color _themeAccent = Palette.AseGold; // drives advance glow + leading edge

        private void Start()
        {
            // A decorative skin must NEVER break gameplay or the boot smoke test —
            // degrade silently to the base UI on any failure (cf. the cloud-save
            // "always fall through" rule).
            try
            {
                _dotSprite = BuildDotSprite(64);
                _roundedBig = RoundedRectSprite(48, 16f);
                _roundedSmall = RoundedRectSprite(24, 8f);
                _ring = RoundedBorderSprite(48, 16f, 3.5f);

                ThemeText();
                BuildBackground();
                SkinForeground();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainScreenSkin] skin pass failed, leaving base UI: {e.Message}");
            }
        }

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

            // CTA glow breathes only while advancing is actually possible.
            if (_advanceGlow != null)
            {
                bool ready = _advanceButton == null || _advanceButton.interactable;
                float a = ready ? 0.16f + 0.22f * pulse : 0f;
                _advanceGlow.color = _themeAccent.WithAlpha(a);
            }

            // Bright leading edge rides the fill front (liquid-gold wavefront).
            if (_barLeadingEdge != null && _barFill != null)
            {
                float f = _barFill.fillAmount;
                var rt = _barLeadingEdge.rectTransform;
                rt.anchorMin = new Vector2(f, 0f);
                rt.anchorMax = new Vector2(f, 1f);
                rt.anchoredPosition = Vector2.zero;
                float vis = (f > 0.015f && f < 0.995f) ? 1f : 0f;
                _barLeadingEdge.color = _themeAccent.WithAlpha(vis * (0.55f + 0.35f * pulse));
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
            if (hero != null) hero.color = Palette.AseGold;
        }

        private static bool Approx(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.06f && Mathf.Abs(a.g - b.g) < 0.06f && Mathf.Abs(a.b - b.b) < 0.06f;

        private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        // ================= background + motes =================

        private void BuildBackground()
        {
            var canvas = FindRootCanvas();
            if (canvas == null) return;

            var sky = NewStretchImage("SkyBackground", canvas.transform);
            sky.sprite = BuildSkySprite(180, 360);
            sky.color = Color.white;
            sky.rectTransform.SetSiblingIndex(0); // behind every existing UI element

            // Path-accent horizon bloom — a soft elliptical glow at the bottom of the
            // sky that tints per path (gold neutral / ochre earth / amber storm / teal river).
            _pathOverlay = NewStretchImage("PathOverlay", canvas.transform);
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

            // Drifting Àṣẹ motes: soft gold points above the sky, below the UI.
            var moteLayer = NewStretchImage("MoteLayer", canvas.transform);
            moteLayer.color = Color.clear;
            moteLayer.rectTransform.SetSiblingIndex(2);

            // Wave 3 — storm-sky tint: a warm amber-dark overlay, transparent until
            // the tribulation fraction exceeds 50% at the final stage.
            _stormSkyTint = NewStretchImage("StormSkyTint", canvas.transform);
            _stormSkyTint.sprite = _dotSprite; // soft radial falloff; stretched full-screen
            _stormSkyTint.color = Color.clear; // RefreshTribulationAtmosphere() drives this
            _stormSkyTint.rectTransform.SetSiblingIndex(3); // above motes, below all UI zones

            // Wave 3 — edge vignette: the skin takes over the SceneBuilder-built
            // StormVignette Image so TribulationAtmosphere.VignetteAlpha() drives
            // it (same step values as the controller stub, now from the named fn).
            _stormEdgeVignette = FindComp<Image>(canvas.transform, "StormVignette");
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
            SkinBar(root);
            SkinCouncil(root);
        }

        /// <summary>The portrait Image is a transparent raycast-only hit-area; all
        /// visible light (aura, silhouette, motes) comes from child Images.</summary>
        private void SkinPortrait(Transform root)
        {
            var portrait = FindComp<Image>(root, "PortraitImage");
            if (portrait == null) return;

            // Path-accent aura behind the bust — a soft glow that tints per path.
            _silhouetteAura = NewChildImage(portrait.rectTransform, "SilhouetteAura");
            var art = _silhouetteAura.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(310f, 310f); // slightly larger than the bust
            art.anchoredPosition = Vector2.zero;
            _silhouetteAura.sprite = _dotSprite;
            _silhouetteAura.color = Color.clear; // ApplyPathTheme() sets this

            _silhouette = NewChildImage(portrait.rectTransform, "SilhouetteOfLight");
            var srt = _silhouette.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(250f, 250f); // SQUARE → the bust texture isn't vertically stretched
            srt.anchoredPosition = new Vector2(0f, 0f);

            BuildConstellation(srt); // elder crown — toggled on at the final stage
            BuildStaff(srt);         // elder staff — toggled on at the elder tiers
            // The bust sprite itself is built on the first Update tick, once the real
            // cultivation stage is known — avoids a stage-0 flash before the save loads.
        }

        /// <summary>Flat amber rectangle → rounded gold face, dark label, soft glow.</summary>
        private void SkinAdvance(Transform root)
        {
            var advance = FindDeep(root, "AdvanceButton");
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
            _advanceGlow = NewChildImage(advance.parent, "AdvanceGlow");
            _advanceGlow.sprite = _dotSprite;
            var grt = _advanceGlow.rectTransform;
            grt.anchorMin = brt.anchorMin;
            grt.anchorMax = brt.anchorMax;
            grt.offsetMin = brt.offsetMin + new Vector2(-18f, -16f);
            grt.offsetMax = brt.offsetMax + new Vector2(18f, 16f);
            _advanceGlow.color = Palette.AseGold.WithAlpha(0.2f);
            _advanceGlow.rectTransform.SetSiblingIndex(0);
        }

        /// <summary>Flat gold pill → rounded indigo trough + liquid-gold fill + a
        /// bright leading edge that tracks the fill front.</summary>
        private void SkinBar(Transform root)
        {
            var trough = FindComp<Image>(root, "BarBackground");
            if (trough == null) return;
            trough.sprite = _roundedSmall;
            trough.type = Image.Type.Sliced;
            trough.color = Palette.IndigoNight.WithAlpha(0.9f);

            _barFill = FindComp<Image>(root, "BarFill");
            if (_barFill != null)
            {
                _barFill.sprite = VGradientSprite(64, Palette.AseDeep, Palette.AseCore);
                _barFillTex = _barFill.sprite.texture; // tracked for destruction on re-theme
                _barFill.type = Image.Type.Filled;            // controller drives fillAmount
                _barFill.fillMethod = Image.FillMethod.Horizontal;
                _barFill.color = Color.white;                 // show the gradient's true colours
                _barFill.rectTransform.offsetMin = new Vector2(3f, 3f); // sit inside the rounded trough
                _barFill.rectTransform.offsetMax = new Vector2(-3f, -3f);

                _barLeadingEdge = NewChildImage(trough.rectTransform, "BarLeadingEdge");
                _barLeadingEdge.sprite = _dotSprite;
                var lrt = _barLeadingEdge.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.sizeDelta = new Vector2(16f, 12f);
                _barLeadingEdge.color = Palette.AseCore.WithAlpha(0f);
            }
        }

        /// <summary>Five flat squares → rounded chips with persistent gold rims. The
        /// strip view recolours the fill (.color) at runtime; the rounded sprite and
        /// the rim child are untouched, so the craft survives every refresh.</summary>
        private void SkinCouncil(Transform root)
        {
            for (int i = 1; i <= 5; i++)
            {
                var slot = FindComp<Image>(root, $"CouncilSlot{i}");
                if (slot == null) continue;
                slot.sprite = _roundedBig;
                slot.type = Image.Type.Sliced;

                var rim = NewChildImage(slot.rectTransform, "SlotRim");
                rim.sprite = _ring;
                rim.type = Image.Type.Sliced;
                rim.color = Palette.AseDeep;
            }
        }

        // ================= Wave 3: tribulation atmosphere =================

        /// <summary>Drives both storm atmosphere layers from the tribulation fraction.
        /// Fraction sourced from _barFill.fillAmount (controller keeps it live).
        /// At stages 0-4 fraction stays 0 so both layers remain transparent.</summary>
        private void RefreshTribulationAtmosphere()
        {
            double fraction = CurrentStage() >= 5 && _barFill != null
                ? _barFill.fillAmount
                : 0.0;

            if (_stormSkyTint != null)
                _stormSkyTint.color = TribulationAtmosphere.SkyOverlayColor(fraction);

            if (_stormEdgeVignette != null)
            {
                var c = _stormEdgeVignette.color;
                c.a = TribulationAtmosphere.VignetteAlpha(fraction);
                _stormEdgeVignette.color = c;
            }
        }

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

            if (_barFill != null)
            {
                var oldSprite = _barFill.sprite;
                var oldTex = _barFillTex;
                _barFill.sprite = VGradientSprite(64, t.BarBot, t.BarTop);
                _barFillTex = _barFill.sprite.texture;
                if (oldSprite != null) Destroy(oldSprite);
                if (oldTex != null) Destroy(oldTex);
            }

            _themeAccent = t.Aura;

            for (int i = 0; i < _motes.Count; i++)
                _motes[i].SetTheme(t.Mote, t.Style);
        }

        private void RebuildSilhouette(int stage)
        {
            var sprite = BuildBustSprite(256, ProfileForStage(stage));
            var old = _silhouette.sprite;
            _silhouette.sprite = sprite;
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
            var img = NewChildImage(parent, "CStar");
            img.sprite = _dotSprite;
            img.color = Palette.AseCore;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8f, 8f);
            rt.anchoredPosition = pos;
        }

        private void AddConstellationLine(RectTransform parent, Vector2 a, Vector2 b)
        {
            var img = NewChildImage(parent, "CLine");
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
            var img = NewChildImage(parent, "Staff");
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

        // ================= UGUI builders =================

        private static Image NewStretchImage(string name, Transform parent)
        {
            var img = NewChildImage(parent, name);
            return img;
        }

        private static Image NewChildImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false; // decorative — never eat touches
            return img;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static T FindComp<T>(Transform root, string name) where T : Component
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<T>() : null;
        }

        // ================= procedural textures =================

        private static Sprite BuildSkySprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < h; y++)
            {
                float vy = y / (h - 1f); // 0 = bottom (horizon), 1 = top
                Color sky = vy > 0.5f
                    ? Color.Lerp(Palette.IndigoBase, Palette.IndigoNight, (vy - 0.5f) / 0.5f)
                    : Color.Lerp(Palette.DuskViolet, Palette.IndigoBase, vy / 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float gx = (x / (w - 1f)) - 0.5f;
                    // Warm horizon-glow: a soft ellipse hottest at bottom-centre.
                    float d = Mathf.Sqrt(gx * gx * 1.7f + vy * vy * 2.6f);
                    float glow = Mathf.Clamp01(1f - d);
                    glow *= glow;
                    Color c = sky + Palette.AseGold * (glow * 0.85f) + Palette.AseCore * (glow * glow * glow * 0.5f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

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
                float dx = x + 0.5f - r;
                float dy = y + 0.5f - r;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / r);
                a *= a; // smooth falloff to a soft glow
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>9-sliced rounded rectangle (solid white; tint via Image.color).</summary>
        private static Sprite RoundedRectSprite(int size, float radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            float half = size * 0.5f;
            float bx = half - radius, by = half - radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundedDist(x + 0.5f - half, y + 0.5f - half, bx, by, radius);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        /// <summary>9-sliced rounded border (hollow centre) for rims.</summary>
        private static Sprite RoundedBorderSprite(int size, float radius, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            float half = size * 0.5f;
            float bx = half - radius, by = half - radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = RoundedDist(x + 0.5f - half, y + 0.5f - half, bx, by, radius);
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + thickness));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner)));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // Signed distance to a rounded box centred at origin (negative inside).
        private static float RoundedDist(float px, float py, float bx, float by, float radius)
        {
            float qx = Mathf.Abs(px) - bx;
            float qy = Mathf.Abs(py) - by;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>Thin vertical gradient (stretched horizontally → uniform sheen).</summary>
        private static Sprite VGradientSprite(int h, Color bottom, Color top)
        {
            const int w = 8;
            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (h - 1f));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Per-stage proportions of the bust. Child → elder, sampled at
        /// stage/5: the figure grows taller and broader, the head shrinks in
        /// proportion, and the light goes from a faint spark to "made of light"
        /// (ART_BIBLE §4 ascent arc). All coords are normalised (0..1, y up).</summary>
        private struct BustProfile
        {
            public float HeadRx, HeadRy, HeadCy;
            public float NeckHalf, NeckTop, NeckBot;
            public float BodyHalfW, BodyTop, ShoulderR; // body is ONE rounded-shouldered shape
            public float LightCy, CoreBright, BodyAlpha, Halo;
        }

        private static BustProfile ProfileForStage(int stage)
        {
            float t = Mathf.Clamp01(stage / 5f); // 0 = Ọmọ Ayé (child), 1 = Aṣẹ́gun (elder)
            return new BustProfile
            {
                HeadRx = Mathf.Lerp(0.100f, 0.128f, t),
                HeadRy = Mathf.Lerp(0.115f, 0.145f, t),
                HeadCy = Mathf.Lerp(0.640f, 0.800f, t),
                NeckHalf = Mathf.Lerp(0.046f, 0.064f, t),
                NeckTop = Mathf.Lerp(0.540f, 0.680f, t),
                NeckBot = Mathf.Lerp(0.420f, 0.580f, t),
                BodyHalfW = Mathf.Lerp(0.160f, 0.290f, t),
                BodyTop = Mathf.Lerp(0.450f, 0.630f, t),
                ShoulderR = Mathf.Lerp(0.085f, 0.150f, t),
                LightCy = Mathf.Lerp(0.250f, 0.310f, t),
                CoreBright = Mathf.Lerp(0.60f, 1.00f, t), // faint spark → made of light
                BodyAlpha = Mathf.Lerp(0.66f, 0.97f, t),
                Halo = Mathf.Lerp(0.16f, 0.40f, t),
            };
        }

        /// <summary>The silhouette of light: a head-and-shoulders bust filled with
        /// gold that glows from the chest, wrapped in a soft halo, edged by a bright
        /// lit rim. Two passes — coverage (supersampled), then shading + rim from the
        /// coverage gradient — so the outline reads as light, not a cutout.</summary>
        private static Sprite BuildBustSprite(int size, BustProfile p)
        {
            int n = size;
            var cov = new float[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float c = 0f;
                for (int sy = 0; sy < 2; sy++)
                for (int sx = 0; sx < 2; sx++)
                {
                    float nx = (x + (sx == 0 ? 0.25f : 0.75f)) / n;
                    float ny = (y + (sy == 0 ? 0.25f : 0.75f)) / n;
                    if (InsideBust(nx, ny, p)) c += 0.25f;
                }
                cov[y * n + x] = c;
            }

            var px = new Color[n * n];
            const int k = 2; // rim sampling distance (px)
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int idx = y * n + x;
                float c = cov[idx];

                float nxc = (x + 0.5f) / n, nyc = (y + 0.5f) / n;
                float cx = nxc - 0.5f, dyl = nyc - p.LightCy;
                float dist = Mathf.Sqrt(cx * cx + dyl * dyl);
                float gB = Mathf.Clamp01(1f - dist / 0.70f);
                float core = gB * p.CoreBright;
                float bottomFade = Mathf.Clamp01(nyc / 0.10f);

                Color bustCol = Color.Lerp(Palette.AseDeep, Palette.AseCore, 0.22f + 0.78f * core);
                float bustA = c * p.BodyAlpha * (0.80f + 0.20f * gB) * bottomFade;

                float halo = Mathf.Clamp01(1f - dist / 0.62f);
                halo = halo * halo * p.Halo;

                float baseA = bustA + halo * (1f - bustA);
                Color baseCol = Color.Lerp(Palette.AseGold, bustCol, bustA);

                // Lit rim: a bright edge where coverage falls off (the light outline).
                float up = cov[Mathf.Min(y + k, n - 1) * n + x];
                float dn = cov[Mathf.Max(y - k, 0) * n + x];
                float lf = cov[y * n + Mathf.Max(x - k, 0)];
                float rg = cov[y * n + Mathf.Min(x + k, n - 1)];
                float edge = Mathf.Clamp01((c - (up + dn + lf + rg) * 0.25f) * 2.2f) * bottomFade;
                float rimA = edge * 0.75f;

                Color outCol = Color.Lerp(baseCol, Palette.AseCore, rimA);
                float outA = Mathf.Clamp01(baseA + rimA * (1f - baseA));
                px[idx] = new Color(outCol.r, outCol.g, outCol.b, outA);
            }

            var tex = new Texture2D(n, n, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }

        // A single clean silhouette: one oval head, a narrow neck, and ONE
        // rounded-shouldered body (a rectangle with rounded top corners), cut off at
        // the bottom by the frame. No overlapping sub-shapes → no visible seams.
        private static bool InsideBust(float nx, float ny, BustProfile p)
        {
            float cx = nx - 0.5f, acx = Mathf.Abs(cx);
            // Head (slightly tall oval).
            if (Oval(cx, ny, 0f, p.HeadCy, p.HeadRx, p.HeadRy)) return true;
            // Neck.
            if (acx <= p.NeckHalf && ny >= p.NeckBot && ny <= p.NeckTop) return true;
            // Body: one rounded-shouldered shape. Inside the straight rect, except the
            // two top corners which are rounded off (the shoulders).
            if (acx <= p.BodyHalfW && ny >= 0f && ny <= p.BodyTop)
            {
                float cornerX = p.BodyHalfW - p.ShoulderR;
                float cornerY = p.BodyTop - p.ShoulderR;
                if (acx > cornerX && ny > cornerY)
                {
                    float dx = acx - cornerX, dy = ny - cornerY;
                    return dx * dx + dy * dy <= p.ShoulderR * p.ShoulderR;
                }
                return true;
            }
            return false;
        }

        private static bool Oval(float x, float y, float cxc, float cyc, float rx, float ry)
        {
            float dx = (x - cxc) / rx, dy = (y - cyc) / ry;
            return dx * dx + dy * dy <= 1f;
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
