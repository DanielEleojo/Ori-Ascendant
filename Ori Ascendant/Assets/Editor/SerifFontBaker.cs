using UnityEditor;
using UnityEngine;
using TMPro;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// Bakes the Noto Serif "display voice" TMP SDF asset (ART_BIBLE §7.9 two-voice
    /// typography; consumed via FontRoleSpec.DisplayFontResourcePath). Mirrors
    /// SceneBuilder.EnsureNotoFallback, but:
    ///   • writes to the Resources path the runtime actually Resources.Load()s, and
    ///   • does NOT register the serif as a global fallback — it is the DISPLAY font
    ///     (sacred/ceremonial moments), not the body fallback. Its own missing glyphs
    ///     fall back to the already-registered Noto Sans.
    ///
    /// Prereq: NotoSerif-Regular.ttf must be in Assets/Fonts/ (extracted from the
    /// Google Fonts zip) and imported by Unity. Run from a Mac/Editor — TMP SDF baking
    /// is an Editor-only operation.
    ///
    /// Menu: Ori Ascendant → Bake Serif Display Font   (idempotent — skips if present).
    /// </summary>
    public static class SerifFontBaker
    {
        private const string SerifTtfPath = "Assets/Fonts/NotoSerif-Regular.ttf";
        // MUST equal "Assets/Resources/" + FontRoleSpec.DisplayFontResourcePath + ".asset".
        private const string SerifAssetPath = "Assets/Resources/Fonts/NotoSerif-Regular SDF.asset";

        // The Yoruba set the display voice must carry (Àṣẹ, Aṣẹ́gun, …) + hero-numeral
        // glyphs. The dynamic atlas adds anything else on demand at runtime.
        private const string PrebakeChars =
            "ÀàáÁèéìÌíòóọỌẹẸṣṢ̀́ńÒ" + "0123456789.,+- KMBT";

        [MenuItem("Ori Ascendant/Bake Serif Display Font")]
        public static void Bake()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SerifAssetPath);
            if (existing != null)
            {
                Debug.Log($"[SerifFontBaker] {SerifAssetPath} already exists — the display voice is live. Delete it to re-bake.");
                return;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SerifTtfPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[SerifFontBaker] {SerifTtfPath} not found. Extract NotoSerif-Regular.ttf into Assets/Fonts/, let Unity import it, then re-run.");
                return;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Fonts");

            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 6,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic);
            fontAsset.name = "NotoSerif-Regular SDF";
            AssetDatabase.CreateAsset(fontAsset, SerifAssetPath);
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);

            // Pre-bake the display set (also verifies Noto Serif carries the diacritics).
            if (!fontAsset.TryAddCharacters(PrebakeChars, out string missing) &&
                !string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[SerifFontBaker] Noto Serif lacks glyphs: {missing} — these fall back to Noto Sans at runtime.");
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SerifFontBaker] Baked {SerifAssetPath}. Re-enter Play mode — the Àṣẹ numeral + titles now render in serif.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
