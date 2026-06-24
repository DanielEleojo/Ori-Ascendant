namespace OriAscendant.UI
{
    /// <summary>
    /// 4-pixel base spacing scale at the 390×844 reference resolution.
    /// All values are layout pixels — multiply by canvas scale factor for
    /// physical pixels. Use these names everywhere padding, gap, or margin
    /// appears in procedural UI builders (UiBuilder, MainScreenSkin, etc.)
    /// rather than inline literals.
    ///
    /// Host-free: pure constants, verifiable headlessly.
    /// </summary>
    public static class SpacingScale
    {
        /// <summary>4 px — icon inset, hairline gap.</summary>
        public const float Xxs = 4f;
        /// <summary>8 px — tight component gap.</summary>
        public const float Xs  = 8f;
        /// <summary>12 px — within-group gap.</summary>
        public const float Sm  = 12f;
        /// <summary>16 px — standard component padding.</summary>
        public const float Md  = 16f;
        /// <summary>24 px — section gap, panel padding.</summary>
        public const float Lg  = 24f;
        /// <summary>32 px — major layout section gap.</summary>
        public const float Xl  = 32f;
        /// <summary>48 px — between top-level layout zones.</summary>
        public const float Xxl = 48f;
    }
}
