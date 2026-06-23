namespace OriAscendant.UI
{
    /// <summary>
    /// Named corner-radius values for ProceduralSprites-generated shapes.
    /// Centralises the radii currently passed as inline literals to
    /// <c>ProceduralSprites.RoundedRect</c> / <c>RoundedRectOutline</c>
    /// across view builders.
    ///
    /// Host-free: pure constants, verifiable headlessly.
    /// </summary>
    public static class CornerRadiusSpec
    {
        /// <summary>Standard selectable card panel (Path/Ori/Crossroads/Council).</summary>
        public const float Card         = 16f;

        /// <summary>Compact chip / badge / tag background.</summary>
        public const float Chip         =  8f;

        /// <summary>Pill-shaped confirm button and long-press fill bar.</summary>
        public const float Pill         = 24f;

        /// <summary>
        /// Border-stroke width passed as the outline thickness argument alongside
        /// the above radii. Kept here so thickness + radius always travel together.
        /// </summary>
        public const float BorderStroke =  3.5f;
    }
}
