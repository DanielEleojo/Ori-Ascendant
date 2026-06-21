using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Shared procedural texture/sprite builders for the in-engine skins (ADR-0001).
    /// Pure construction — no scene state, no ServiceLocator — so all three skins
    /// (MainScreenSkin, ColdOpenSkin, TitleScreenSkin) draw their soft dots, rounded
    /// panels, sky gradient, and bust silhouette from ONE place instead of each carrying
    /// its own copy of BuildDotSprite. Constants here are geometry/curve math (ADR-0006:
    /// Spec, not Config).
    /// </summary>
    public static class ProceduralSprites
    {
        /// <summary>Soft radial dot — auras, stars, motes, leading-edge glows.</summary>
        public static Sprite BuildDot(int size)
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

        /// <summary>Procedural indigo→gold sky gradient with a warm horizon glow (§5.2).</summary>
        public static Sprite BuildSky(int w, int h)
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

        /// <summary>9-sliced rounded rectangle (solid white; tint via Image.color).</summary>
        public static Sprite RoundedRect(int size, float radius)
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
        public static Sprite RoundedBorder(int size, float radius, float thickness)
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

        /// <summary>Per-stage proportions of the bust. Child → elder, sampled at
        /// stage/5: the figure grows taller and broader, the head shrinks in
        /// proportion, and the light goes from a faint spark to "made of light"
        /// (ART_BIBLE §4 ascent arc). All coords are normalised (0..1, y up).</summary>
        public struct BustProfile
        {
            public float HeadRx, HeadRy, HeadCy;
            public float NeckHalf, NeckTop, NeckBot;
            public float BodyHalfW, BodyTop, ShoulderR; // body is ONE rounded-shouldered shape
            public float LightCy, CoreBright, BodyAlpha, Halo;
        }

        public static BustProfile ProfileForStage(int stage)
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
        public static Sprite BuildBust(int size, BustProfile p)
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
    }
}
