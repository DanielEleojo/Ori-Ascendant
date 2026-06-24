namespace OriAscendant.UI
{
    /// <summary>
    /// Named opacity values for shared UI surfaces — scrims, path accents,
    /// field tints, and waterline pulse bounds.
    ///
    /// NOTE: hero-glow alpha and chrome-hairline alpha live in
    /// <see cref="AseHeroSpec"/> (<c>HeroGlowAlpha = 0.14f</c>,
    /// <c>HairlineBorderAlpha = 0.10f</c>). Do NOT redefine them here;
    /// reference AseHeroSpec directly for those two values.
    ///
    /// Host-free: pure constants, verifiable headlessly.
    /// </summary>
    public static class OpacitySpec
    {
        /// <summary>
        /// Background-dimming scrim behind modals and overlays.
        /// Heavy enough to direct focus; not so heavy it hides atmosphere.
        /// </summary>
        public const float Scrim = 0.62f;

        /// <summary>
        /// Faint path-colour wash behind active path-relevant panels.
        /// Seasoning, not a fill — stays below Scrim and HeroGlow.
        /// </summary>
        public const float PathAccent = 0.18f;

        /// <summary>
        /// Stronger hairline variant for card-edge separators in contexts
        /// where <see cref="AseHeroSpec.HairlineBorderAlpha"/> (0.10) is
        /// too subtle (e.g. selected-ring contrast on dark backgrounds).
        /// </summary>
        public const float HairlineStrong = 0.45f;

        /// <summary>
        /// Deep-field atmosphere tint — subtle colour layer behind the
        /// background grid/stars; keeps the scene from reading as flat.
        /// </summary>
        public const float DeepField = 0.18f;

        /// <summary>Vessel waterline pulse — lower bound (resting, slow breath).</summary>
        public const float WaterlinePulseMin = 0.30f;

        /// <summary>Vessel waterline pulse — upper bound (active channel tap peak).</summary>
        public const float WaterlinePulseMax = 0.55f;
    }
}
