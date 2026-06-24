namespace OriAscendant.UI
{
    /// <summary>
    /// Modular type-size scale at the 390×844 reference resolution (iPhone 12 min).
    /// All values are TMP point sizes — layout-independent of density.
    ///
    /// Two-voice rule (see FontRoleSpec):
    ///   Serif (NotoSerif) → Display/Hero — sacred and ceremonial moments only.
    ///   Sans  (NotoSans)  → H1 and below — all functional / body copy.
    ///
    /// Letter-spacing and font-asset paths live in FontRoleSpec; this class owns
    /// sizes only so the two concerns stay separately testable.
    ///
    /// Host-free: pure constants, verifiable headlessly.
    /// </summary>
    public static class TypographicScale
    {
        /// <summary>
        /// Full-bleed ceremonial title — the intended maximum type size.
        /// NOTE: the Àṣẹ counter in MainScreenSkin is currently geometry-pinned to 38f
        /// to fit the 41px CounterZone band; repointing it to Hero (44f) will overflow
        /// that band and re-introduce eyebrow bleed. Adjust the band anchors first.
        /// </summary>
        public const float Hero    = 44f;
        /// <summary>Stage-name display / proverb — serif ceremonial.</summary>
        public const float Display = 30f;
        /// <summary>Section heading — largest functional sans heading.</summary>
        public const float H1      = 22f;
        /// <summary>Sub-section heading / card title.</summary>
        public const float H2      = 18f;
        /// <summary>Primary body copy, council-strip labels, option names.</summary>
        public const float Body    = 16f;
        /// <summary>Secondary body / supplementary info.</summary>
        public const float BodySm  = 14f;
        /// <summary>Chip labels, tab labels, compact metadata.</summary>
        public const float Label   = 13f;
        /// <summary>Timestamps, fine-print, sub-caption.</summary>
        public const float Caption = 11f;
    }
}
