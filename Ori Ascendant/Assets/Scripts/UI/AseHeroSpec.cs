namespace OriAscendant.UI
{
    /// <summary>
    /// Chrome-discipline rules for the minimalist polish pass (issue #30, PRD W4).
    /// One hero number radiates gold; all other chrome is flat and calm.
    ///
    /// Host-free — pure constants and predicates — so every rule is verifiable
    /// headlessly without a scene. MainScreenSkin reads these values to apply the
    /// discipline to the live canvas at runtime.
    ///
    /// Hierarchy (most prominent → least):
    ///   HeroGlowAlpha > HairlineBorderAlpha > ChromePanelAlpha (zero)
    ///
    /// The single luminous element is the Àṣẹ counter (identified as the largest
    /// active TMP_Text on the canvas). It receives a faint gold glow — luminous
    /// but not heavy. Everything else is flat: transparent fills, hairline borders.
    /// </summary>
    public static class AseHeroSpec
    {
        /// <summary>
        /// Returns true when this font size belongs to the hero Àṣẹ counter —
        /// i.e. it is the largest active text element visible on the canvas.
        /// </summary>
        public static bool IsHeroCounter(float fontSize, float maxFontSizeOnCanvas) =>
            maxFontSizeOnCanvas > 0f && fontSize >= maxFontSizeOnCanvas;

        /// <summary>
        /// Resting alpha for the faint glow image behind the hero counter.
        /// Luminous but not heavy: strictly less than 0.25 so the number reads
        /// as self-lit, not spotlit by a visible disc.
        /// </summary>
        public const float HeroGlowAlpha = 0.14f;

        /// <summary>
        /// Expansion (in local layout units) beyond the counter rect when sizing
        /// the glow image. The dot-sprite falloff means the glow is softest at
        /// the edges — a larger pad spreads it gently outward.
        /// </summary>
        public const float HeroGlowPadding = 28f;

        /// <summary>
        /// Fill alpha for chrome panel backgrounds.
        /// Zero = flat and transparent; panels never assert their presence.
        /// </summary>
        public const float ChromePanelAlpha = 0f;

        /// <summary>
        /// Alpha for hairline borders separating chrome zones.
        /// Restrained — strictly less than HeroGlowAlpha so borders read
        /// calmer than the hero glow in the visual hierarchy.
        /// </summary>
        public const float HairlineBorderAlpha = 0.10f;

        /// <summary>
        /// Returns true when a panel fill alpha would read as "heavy" — too
        /// prominent for the minimalist chrome discipline.
        /// Any alpha strictly above HeroGlowAlpha competes with the hero number.
        /// </summary>
        public static bool IsPanelHeavy(float panelFillAlpha) =>
            panelFillAlpha > HeroGlowAlpha;
    }
}
