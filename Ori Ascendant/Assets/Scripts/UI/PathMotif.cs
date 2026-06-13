using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Ancestor identity derivation (GAMEPLAY §5.4): no names in MVP — title from
    /// path, abstract motif COLORS only (earth/stars, lightning/flame, river/light;
    /// never masks or regalia per the §7.3 red line), radiance/ember by outcome.
    /// </summary>
    public static class PathMotif
    {
        public static readonly Color Radiance = new Color(0.851f, 0.643f, 0.255f); // gold
        public static readonly Color Ember = new Color(0.804f, 0.412f, 0.227f);    // warm ember — never grey/red
        public static readonly Color Neutral = new Color(0.165f, 0.192f, 0.251f);

        private static readonly Color Earth = new Color(0.478f, 0.604f, 0.396f);   // Ane — earth/stars
        private static readonly Color Thunder = new Color(0.878f, 0.482f, 0.224f); // Sango — lightning/flame
        private static readonly Color River = new Color(0.357f, 0.659f, 0.788f);   // Osun — river/light

        public static Color ColorOf(int pathIndex) => pathIndex switch
        {
            0 => Earth,
            1 => Thunder,
            2 => River,
            _ => Neutral,
        };

        /// <summary>"Aṣẹ́gun of {…}" card title fragment.</summary>
        public static string TitleOf(int pathIndex) => pathIndex switch
        {
            0 => "Earth",
            1 => "Thunder",
            2 => "the River",
            _ => "the First Road", // gen with no path recorded (defensive)
        };

        /// <summary>Strip/card tint: fallen ancestors render dimmed (0.4 alpha
        /// treatment per GAMEPLAY §3.2) — present, honored, softer.</summary>
        public static Color AncestorTint(int pathIndex, bool didAscend)
        {
            Color c = ColorOf(pathIndex);
            c.a = didAscend ? 1.0f : 0.4f;
            return c;
        }
    }
}
