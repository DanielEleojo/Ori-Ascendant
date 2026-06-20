using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure mapping from council data to constellation-star visual state (issue #22).
    /// No MonoBehaviour — testable in EditMode without a scene.
    ///
    /// Three distinct states per slot:
    ///   Ascended  → path colour, full brightness  (bright star)
    ///   Fallen    → path colour, dimmed (low ember — present, honoured, softer)
    ///   Empty     → neutral gold, very faint (unlit point, hints the slot exists)
    ///
    /// Path -1 (no path chosen yet) → neutral Àṣẹ gold (the issue specifies
    /// "neutral gold if path-less", not the indigo chip-neutral).
    /// </summary>
    public static class ConstellationStarMapper
    {
        private const float AscendedAlpha = 1.00f;
        private const float FallenAlpha   = 0.45f;
        private const float EmptyAlpha    = 0.18f;

        /// <summary>Star colour for an active council ancestor.</summary>
        public static Color StarColor(AncestorData ancestor)
        {
            Color pathColor = ancestor.path < 0 ? Palette.AseGold : PathMotif.ColorOf(ancestor.path);
            float alpha = ancestor.didAscend ? AscendedAlpha : FallenAlpha;
            return pathColor.WithAlpha(alpha);
        }

        /// <summary>Colour for an empty council seat — a faint unlit point.</summary>
        public static Color EmptySeatColor() => Palette.AseCore.WithAlpha(EmptyAlpha);
    }
}
