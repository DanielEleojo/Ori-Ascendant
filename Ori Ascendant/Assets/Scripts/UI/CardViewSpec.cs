using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Single source of truth for all selectable card views:
    /// OriCardView, CrossroadsOptionView, and PathCardView.
    ///
    /// These idle/selected pairs were previously hardcoded identically in each
    /// of those three views. Unit 3 of the UI-cohesion pass will repoint each
    /// view to read from here instead.
    ///
    /// <c>Color</c> cannot be <c>const</c>, so panel fills use <c>static readonly</c>.
    /// Size and scale values remain <c>const float</c> for headless assertability.
    /// </summary>
    public static class CardViewSpec
    {
        // ---- Panel backgrounds ----

        /// <summary>Consolidated idle card panel — deep navy, replaces duplicated literals.</summary>
        public static readonly Color Idle         = Palette.Hex(0x1A1F29);

        /// <summary>Gold-tinted selected card panel — warm amber.</summary>
        public static readonly Color Selected     = Palette.Hex(0x3B311A);

        // ---- Ring / text ----

        /// <summary>Selection-ring colour — Àṣẹ brass, same as the hero accent.</summary>
        public static readonly Color SelectedRing = Palette.AseGold;

        /// <summary>Label colour in idle state — cool muted lilac-grey.</summary>
        public static readonly Color IdleText     = Palette.TextSecondary;

        /// <summary>Label colour in selected state — warm bone-white.</summary>
        public static readonly Color SelectedText = Palette.TextPrimary;

        // ---- Typography ----

        /// <summary>Card name / option title — Body size per TypographicScale.</summary>
        public const float NameSize = TypographicScale.Body;

        /// <summary>Card sub-label / hint — Caption size per TypographicScale.</summary>
        public const float SubSize  = TypographicScale.Caption;

        // ---- Selection affordance ----

        /// <summary>
        /// Scale applied to the selected card's RectTransform localScale.
        /// Subtle grow makes the active card visually "lifted" without layout shift.
        /// </summary>
        public const float SelectedScale = 1.02f;
    }
}
