using OriAscendant.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Wave 3 procedural dress for the first-launch title screen (ART_BIBLE §5.1):
    /// "a single thread of Àṣẹ gold rising from the bottom up toward a faint
    /// constellation." Self-activates after scene load; inert in any scene that
    /// carries no TitleScreen component (EditMode / PlayMode test scenes).
    ///
    /// Typography and the sky gradient beneath are handled by MainScreenSkin. This
    /// skin adds only the thread + constellation that are specific to the title moment:
    ///   • 24 thin gold Image segments tracing TitleArc.ThreadPoint (0..1)
    ///   • 5 star dots at TitleArc.ConstellationPoints(), apex brightest
    ///
    /// Elements are injected into a "ThreadLayer" child of TitleRoot, sitting above
    /// the TitleBackground but below the text. The layer auto-hides when TitleRoot
    /// hides (they share the same parent). Degrades silently on any failure.
    /// </summary>
    public sealed class TitleScreenSkin : MonoBehaviour
    {
        // Canvas reference resolution matching CanvasScaler (GAMEPLAY §3).
        private const float CanvasW = 390f;
        private const float CanvasH = 844f;

        private const int ThreadSegments = 24;
        private const float ThreadPxHeight = SpacingScale.Xxs + 1f; // 5 canvas-units — close to Xxs (4px) // ponytail:

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var screens = Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None);
            if (screens == null || screens.Length == 0) return;
            new GameObject(nameof(TitleScreenSkin)).AddComponent<TitleScreenSkin>();
        }

        private void Start()
        {
            try
            {
                var screens = Object.FindObjectsByType<TitleScreen>(FindObjectsSortMode.None);
                if (screens == null || screens.Length == 0) return;

                var titleRoot = UiBuilder.FindDeep(screens[0].transform, "TitleRoot");
                if (titleRoot == null) return;

                var dot = ProceduralSprites.BuildDot(64);

                // ThreadLayer sits above TitleBackground (index 0) and below all text.
                var layer = new GameObject("ThreadLayer", typeof(RectTransform)).transform;
                layer.SetParent(titleRoot, false);
                var layerRt = (RectTransform)layer;
                layerRt.anchorMin = Vector2.zero;
                layerRt.anchorMax = Vector2.one;
                layerRt.offsetMin = layerRt.offsetMax = Vector2.zero;
                layer.SetSiblingIndex(1);

                DrawThread(layer, dot);
                DrawConstellation(layer, dot);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TitleScreenSkin] skin pass failed, leaving base UI: {e.Message}");
            }
        }

        // ---- thread ----

        private static void DrawThread(Transform parent, Sprite dot)
        {
            for (int i = 0; i < ThreadSegments; i++)
            {
                float t0 = i / (float)ThreadSegments;
                float t1 = (i + 1) / (float)ThreadSegments;
                Vector2 a = TitleArc.ThreadPoint(t0);
                Vector2 b = TitleArc.ThreadPoint(t1);

                // Alpha ramps up to peak at t≈0.55, then eases off as the thread
                // dissolves into the constellation bloom.
                float mid = (t0 + t1) * 0.5f;
                // Thread-arc brightness: specific Èwà-thread values (ART_BIBLE §5.1),
                // not shared opacity roles — keep as literals so the arc reads at full glow.
                float alpha = mid < 0.55f
                    ? Mathf.Lerp(0.12f, 0.72f, mid / 0.55f)
                    : Mathf.Lerp(0.72f, 0.30f, (mid - 0.55f) / 0.45f);

                var img = UiBuilder.NewChildImage(parent, "Thread");
                img.sprite = dot; // soft circular glow; stretched thin → glowing filament
                img.raycastTarget = false;
                img.color = Palette.AseGold.WithAlpha(alpha);

                // Convert normalised coords → canvas-unit coords for length + angle.
                Vector2 aPx = new Vector2(a.x * CanvasW, a.y * CanvasH);
                Vector2 bPx = new Vector2(b.x * CanvasW, b.y * CanvasH);
                Vector2 delta = bPx - aPx;

                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = (a + b) * 0.5f;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(delta.magnitude, ThreadPxHeight);
                rt.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        // ---- constellation ----

        private static void DrawConstellation(Transform parent, Sprite dot)
        {
            foreach (var pt in TitleArc.ConstellationPoints())
            {
                var img = UiBuilder.NewChildImage(parent, "CStar");
                img.sprite = dot;
                img.raycastTarget = false;
                img.color = Palette.AseCore.WithAlpha(pt.Alpha);

                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = pt.Pos;
                rt.anchoredPosition = Vector2.zero;
                // pt.Size is normalised; multiply by canvas height for consistent px size.
                float px = pt.Size * CanvasH;
                rt.sizeDelta = new Vector2(px, px);
            }
        }
    }
}
