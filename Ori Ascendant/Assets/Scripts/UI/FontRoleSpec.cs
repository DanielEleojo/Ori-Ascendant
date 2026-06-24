namespace OriAscendant.UI
{
    /// <summary>
    /// Two-voice typography discipline (ART_BIBLE §7.9 + Wave A):
    ///   Display voice — Noto Serif SDF for sacred/ceremonial moments
    ///   Body voice    — Noto Sans SDF for all functional UI copy
    ///
    /// The display font asset is not yet imported; every caller must null-check
    /// Resources.Load(DisplayFontResourcePath) and fall back to the body font.
    /// Host-free: pure constants so the rules are verifiable headlessly.
    /// </summary>
    public static class FontRoleSpec
    {
        public const string DisplayFontResourcePath = "Fonts/NotoSerif-Regular SDF";
        public const string BodyFontResourcePath    = "Fonts/NotoSans-Regular SDF";

        public const float HeroLetterSpacing       = 2.5f;
        public const float ProverbCharacterSpacing = 1.8f;
        public const float StageTitleLetterSpacing = 3.5f;
        public const float BodyCharacterSpacing    = 0f;
    }
}
