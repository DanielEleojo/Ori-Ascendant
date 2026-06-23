using System.IO;
using UnityEditor;
using UnityEngine;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// Bakes a procedural 1024×1024 app icon (Ori crown motif) and configures
    /// iOS PlayerSettings. Run before an iOS export from a Mac — the pixel work
    /// runs on any platform, but iOSLaunchScreen settings only affect iOS builds.
    ///
    /// Menu: Ori Ascendant → Bake App Assets
    /// Headless: -executeMethod OriAscendant.EditorTools.AppAssetBaker.BakeAll
    /// </summary>
    public static class AppAssetBaker
    {
        private const int    IconSize       = 1024;
        private const string IconOutputPath = "Assets/AppIcon_1024.png";

        // ---- Colour palette (Editor-only copy — no runtime Palette dependency) ----
        private static Color IndigoNight => HexColor(0x07091A);
        private static Color IndigoBase  => HexColor(0x0F1430);
        // IndigoLift used only if needed for future layers
        private static Color AseCore     => HexColor(0xFFE6A8);
        private static Color AseGold     => HexColor(0xE8C77A);
        private static Color AseDeep     => HexColor(0xC9A24B);

        // ---- Entry point ----

        [MenuItem("Ori Ascendant/Bake App Assets")]
        public static void BakeAll()
        {
            BakeIcon();
            ConfigureIosPlayerSettings();
            AssetDatabase.Refresh();
            Debug.Log("[AppAssetBaker] App assets baked. Run on Mac before iOS export.");
        }

        // ---- Icon baking ----

        private static void BakeIcon()
        {
            int   size   = IconSize;
            var   tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var   colors = new Color[size * size];

            float half      = size * 0.5f;
            float maxRadius = half * Mathf.Sqrt(2f); // corner distance — used to normalise gradient

            // Cardinal (0°, 90°, 180°, 270°) and diagonal (45°, 135°, 225°, 315°) ray angles in radians
            // Precomputed for the 8 rays: cardinal = AseGold, diagonal = AseDeep
            float[] rayAngles = new float[8];
            for (int r = 0; r < 8; r++)
                rayAngles[r] = r * Mathf.PI / 4f; // 0°, 45°, 90° … 315° in radians

            float raySweep    = 3f * Mathf.Deg2Rad; // ±3°
            float rayMaxR     = half * 0.44f;
            float rayMinR     = half * 0.08f;        // rays start just outside inner glow

            float outerRingR  = half * 0.42f;
            float outerRingW  = 4f;                  // pixel width

            float crownR      = half * 0.18f;
            float crownFeather = 2f;

            float glowR       = half * 0.10f;
            float glowFeather = 2f;

            // Star field: 12 dots at 30° steps, radius 38% of half
            float starOrbitR = half * 0.38f;
            float starDotR   = 2f;
            var   starCentres = new Vector2[12];
            for (int s = 0; s < 12; s++)
            {
                float ang = s * 30f * Mathf.Deg2Rad;
                starCentres[s] = new Vector2(
                    half + Mathf.Cos(ang) * starOrbitR,
                    half + Mathf.Sin(ang) * starOrbitR);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var   px      = new Vector2(x + 0.5f, y + 0.5f);
                    float dx      = px.x - half;
                    float dy      = px.y - half;
                    float dist    = Mathf.Sqrt(dx * dx + dy * dy);

                    // ---- Layer 0: Radial background gradient ----
                    float t    = Mathf.Clamp01(dist / maxRadius);
                    Color col  = Color.Lerp(IndigoBase, IndigoNight, t);

                    // ---- Layer 1: Outer decorative ring (AseDeep, alpha 0.6) ----
                    float ringDist = Mathf.Abs(dist - outerRingR);
                    if (ringDist <= outerRingW)
                    {
                        float ringMask = Mathf.Clamp01(1f - ringDist / outerRingW);
                        col = Color.Lerp(col, AseDeep, 0.6f * ringMask);
                    }

                    // ---- Layer 2: 8 rays from center ----
                    if (dist >= rayMinR && dist <= rayMaxR)
                    {
                        // atan2 → [−π, π]; shift to [0, 2π]
                        float angle = Mathf.Atan2(dy, dx);
                        if (angle < 0f) angle += 2f * Mathf.PI;

                        for (int r = 0; r < 8; r++)
                        {
                            float refAng  = rayAngles[r];
                            float diff    = Mathf.Abs(Mathf.DeltaAngle(
                                angle      * Mathf.Rad2Deg,
                                refAng     * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

                            if (diff <= raySweep)
                            {
                                // Soft edge along angular sweep
                                float angMask  = Mathf.SmoothStep(1f, 0f, diff / raySweep);
                                // Soft fade at ray tip
                                float radFade  = Mathf.SmoothStep(1f, 0f,
                                    Mathf.Clamp01((dist - rayMaxR * 0.85f) / (rayMaxR * 0.15f)));
                                float mask     = angMask * radFade;

                                // Cardinal (0, 2, 4, 6 → 0°, 90°, 180°, 270°) = AseGold;
                                // Diagonal (1, 3, 5, 7 → 45°, 135°, 225°, 315°) = AseDeep
                                Color rayCol = (r % 2 == 0) ? AseGold : AseDeep;
                                col = Color.Lerp(col, rayCol, mask * 0.85f);
                            }
                        }
                    }

                    // ---- Layer 3: Crown circle (AseGold, soft edge) ----
                    if (dist <= crownR + crownFeather)
                    {
                        float crownMask = 1f - Mathf.SmoothStep(
                            crownR - crownFeather,
                            crownR + crownFeather,
                            dist);
                        crownMask = Mathf.Clamp01(crownMask);
                        col = Color.Lerp(col, AseGold, crownMask);
                    }

                    // ---- Layer 4: Inner glow (AseCore, soft edge) ----
                    if (dist <= glowR + glowFeather)
                    {
                        float glowMask = 1f - Mathf.SmoothStep(
                            glowR - glowFeather,
                            glowR + glowFeather,
                            dist);
                        glowMask = Mathf.Clamp01(glowMask);
                        col = Color.Lerp(col, AseCore, glowMask);
                    }

                    // ---- Layer 5: Star field dots (AseGold, alpha 0.5) ----
                    for (int s = 0; s < 12; s++)
                    {
                        float sd = Vector2.Distance(px, starCentres[s]);
                        if (sd <= starDotR + 1f)
                        {
                            float starMask = Mathf.Clamp01(1f - Mathf.SmoothStep(
                                starDotR - 1f, starDotR + 1f, sd));
                            col = Color.Lerp(col, AseGold, 0.5f * starMask);
                        }
                    }

                    colors[y * size + x] = col;
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            // Ensure parent folder exists (should already; belt-and-braces)
            string absPath = Path.Combine(Application.dataPath, "..", IconOutputPath);
            absPath = Path.GetFullPath(absPath);
            File.WriteAllBytes(absPath, tex.EncodeToPNG());

            Object.DestroyImmediate(tex);

            Debug.Log($"[AppAssetBaker] Icon written → {IconOutputPath}");
        }

        // ---- iOS PlayerSettings ----

        private static void ConfigureIosPlayerSettings()
        {
            // Identity — keep in sync with BuildConfigurator.BundleId
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.iOS,
                BuildConfigurator.BundleId);
            PlayerSettings.productName  = BuildConfigurator.ProductName;
            PlayerSettings.companyName  = BuildConfigurator.ProductName;

            // Icon
            AssetDatabase.ImportAsset(IconOutputPath);
            var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconOutputPath);
            if (iconTex != null)
            {
                // Unity 6 uses NamedBuildTarget overloads; fall back to BuildTargetGroup
                // for the legacy SetIconsForTargetGroup which is still present and works.
                int[] sizes = { 1024, 180, 167, 152, 120, 87, 80, 76, 58, 40, 29, 20 };
                var   icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++) icons[i] = iconTex;

#pragma warning disable 618 // SetIconsForTargetGroup is not obsolete in Unity 6 but suppress if future-flagged
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);
#pragma warning restore 618
                Debug.Log("[AppAssetBaker] iOS icons assigned.");
            }
            else
            {
                Debug.LogWarning("[AppAssetBaker] Could not load baked icon — iOS icon slot not set.");
            }

            // Splash screen: solid indigo bg; hide Unity logo — ColdOpenSkin is the opening beat.
            // (iOS launch screen type is set in Player Settings → iOS → Splash Image in the Inspector.)
            var bgColor = new Color(0x07 / 255f, 0x09 / 255f, 0x1A / 255f, 1f);
            PlayerSettings.SplashScreen.backgroundColor = bgColor;
            PlayerSettings.SplashScreen.show            = false;

            AssetDatabase.SaveAssets();
            Debug.Log("[AppAssetBaker] iOS PlayerSettings configured.");
        }

        // ---- Colour helpers (pure math, no runtime dependency) ----

        private static Color HexColor(uint rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >>  8) & 0xFF) / 255f,
            ( rgb        & 0xFF) / 255f,
            1f);

    }
}
