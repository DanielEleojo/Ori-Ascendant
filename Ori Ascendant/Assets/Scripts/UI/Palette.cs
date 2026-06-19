using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Single source of truth for the "Painterly Cosmic Myth" colour system
    /// (ART_BIBLE §1). The procedural skin reads every colour from here — never
    /// from literals scattered through views — so the whole look retunes from one
    /// file. Indigo + Àṣẹ-gold is the spine of every screen; path accents are
    /// seasoning, used only on path-relevant UI.
    /// </summary>
    public static class Palette
    {
        // ---- Base: deep indigo night (the constant ground of the game) ----
        public static readonly Color IndigoNight = Hex(0x0E1330); // darkest; vignette edges
        public static readonly Color IndigoBase = Hex(0x1B2150);  // primary background field
        public static readonly Color IndigoLift = Hex(0x2C3470);  // mid sky; atmospheric distance
        public static readonly Color DuskViolet = Hex(0x3E3A6E);  // where indigo warms toward horizon
        public static readonly Color WarmEcru = Hex(0xEDE3CE);    // undyed-adire ground; cloth

        // ---- Àṣẹ gold: the single sacred light (always the brightest, always warm) ----
        public static readonly Color AseCore = Hex(0xFFE6A8); // hottest highlight; near-white gold
        public static readonly Color AseGold = Hex(0xF4C14E); // the signature — counters, primary glow
        public static readonly Color AseDeep = Hex(0xC98A2B); // gold in shadow; brass edges

        // ---- Text ----
        public static readonly Color TextPrimary = Hex(0xF3EEDD);   // warm bone-white (never pure white)
        public static readonly Color TextSecondary = Hex(0xB9B7C8); // cool muted lilac-grey

        // ---- Ember (fallen ancestors): warm-dim dignity, NEVER grey, NEVER red-as-failure ----
        public static readonly Color EmberWarm = Hex(0xD2742F);
        public static readonly Color EmberDeep = Hex(0x7A2E1E);

        // ---- Path accents (used ONLY on path-relevant UI; never bleed across paths) ----
        // Ane — Igala earth, endurance
        public static readonly Color AneEarthGreen = Hex(0x5C7A4A);
        public static readonly Color AneOchre = Hex(0xC28A3A);
        public static readonly Color AneClay = Hex(0x6E4B2E);
        public static readonly Color AneMaize = Hex(0xE8C24A);
        // Sango — Yoruba thunder, sudden force (reds appear ONLY for Sango)
        public static readonly Color SangoStormAmber = Hex(0xF2A33C);
        public static readonly Color SangoThunderRed = Hex(0xC8412B);
        public static readonly Color SangoHotWhite = Hex(0xFFF3D6);
        // Osun — Yoruba river, lineage/flow
        public static readonly Color OsunRiverTeal = Hex(0x2E8C8C);
        public static readonly Color OsunBrass = Hex(0xC9A24B);
        public static readonly Color OsunPale = Hex(0xBFE6DD);

        /// <summary>0xRRGGBB → Color, with optional alpha. Keeps the §1 table readable.</summary>
        public static Color Hex(uint rgb, float a = 1f) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f,
            a);

        /// <summary>Same colour at a new alpha — for glows, scrims, and rim tints.</summary>
        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
