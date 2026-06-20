using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure mapping from council/chronicle data to constellation-star visual state.
    /// No MonoBehaviour — testable in EditMode without a scene.
    ///
    /// Three council states per slot:
    ///   Ascended  → path colour, full brightness  (bright near star)
    ///   Fallen    → path colour, dimmed (low ember — present, honoured, softer)
    ///   Empty     → neutral gold, very faint (unlit point, hints the slot exists)
    ///
    /// Deep-field (retired ancestors, issue #26):
    ///   All use neutral Àṣẹ gold — path data is absent in the chronicle.
    ///   Ascended retired → dimmer than council ascended
    ///   Fallen retired   → dimmer still, but never invisible
    ///
    /// Path -1 (no path chosen yet) → neutral Àṣẹ gold (the issue specifies
    /// "neutral gold if path-less", not the indigo chip-neutral).
    /// </summary>
    public static class ConstellationStarMapper
    {
        private const float AscendedAlpha          = 1.00f;
        private const float FallenAlpha            = 0.45f;
        private const float EmptyAlpha             = 0.18f;
        private const float DeepFieldAscendedAlpha = 0.28f;
        private const float DeepFieldFallenAlpha   = 0.13f;

        /// <summary>Star colour for an active council ancestor.</summary>
        public static Color StarColor(AncestorData ancestor)
        {
            Color pathColor = ancestor.path < 0 ? Palette.AseGold : PathMotif.ColorOf(ancestor.path);
            float alpha = ancestor.didAscend ? AscendedAlpha : FallenAlpha;
            return pathColor.WithAlpha(alpha);
        }

        /// <summary>Colour for an empty council seat — a faint unlit point.</summary>
        public static Color EmptySeatColor() => Palette.AseCore.WithAlpha(EmptyAlpha);

        /// <summary>How many retired-ancestor stars belong in the deep field.
        /// = chronicle.Count − active council.Count (clamped to zero).
        /// Returns 0 for Gen 1 (empty chronicle → near-empty sky).</summary>
        public static int DeepFieldStarCount(SaveData save)
        {
            if (save == null) return 0;
            int chronicle = save.chronicle != null ? save.chronicle.Count : 0;
            int council   = save.council   != null ? save.council.Count   : 0;
            return System.Math.Max(0, chronicle - council);
        }

        /// <summary>Colour for a retired ancestor in the deep field.
        /// All deep-field stars use neutral Àṣẹ gold — path data is not stored
        /// in the chronicle. Ascended retired ancestors outshine fallen ones,
        /// but both are notably dimmer than their council counterparts.</summary>
        public static Color DeepFieldStarColor(bool didAscend)
        {
            float alpha = didAscend ? DeepFieldAscendedAlpha : DeepFieldFallenAlpha;
            return Palette.AseGold.WithAlpha(alpha);
        }
    }
}
