using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Visual state for a single ancestor row in the Council shrine (issue #29).
    /// </summary>
    public struct ShrineAncestorRow
    {
        /// <summary>Path colour at radiance (ascended, alpha 1.0) or ember (fallen, alpha 0.45).</summary>
        public Color SilhouetteColor;
        /// <summary>"Gen N — Aṣẹ́gun of {Path}" + "(ember)" mark for fallen.</summary>
        public string Title;
        /// <summary>The cultivator's earned epithet or title; empty when not yet set.</summary>
        public string Remembrance;
    }

    /// <summary>
    /// Pure mapping: AncestorData → shrine row visual state (issue #29).
    /// No MonoBehaviour — testable in EditMode headlessly.
    ///
    /// Two shrine states per ancestor:
    ///   Ascended → path colour, full alpha   (radiance)
    ///   Fallen   → path colour, dimmed alpha  (ember — present, honoured, softer)
    ///
    /// Path −1 (no path recorded) → neutral Àṣẹ gold, consistent with
    /// ConstellationStarMapper's treatment of path-less ancestors.
    /// </summary>
    public static class ShrineAncestorPresenter
    {
        private const float AscendedAlpha = 1.00f;
        private const float FallenAlpha   = 0.45f;
        private const float EmptyAlpha    = 0.18f;

        public static readonly ShrineAncestorRow EmptySeat = new ShrineAncestorRow
        {
            SilhouetteColor = Palette.AseCore.WithAlpha(EmptyAlpha),
            Title           = "An empty seat awaits",
            Remembrance     = string.Empty,
        };

        /// <summary>
        /// Maps one council ancestor to its shrine row visual state.
        /// <paramref name="generationNumber"/> is the 1-based lineage generation number for
        /// the title label and is computed by the caller (CouncilScreenView.Refresh).
        /// </summary>
        public static ShrineAncestorRow Map(AncestorData ancestor, int generationNumber)
        {
            Color pathColor = ancestor.path < 0 ? Palette.AseGold : PathMotif.ColorOf(ancestor.path);
            float alpha     = ancestor.didAscend ? AscendedAlpha : FallenAlpha;

            string emberMark = ancestor.didAscend ? string.Empty : "  (ember)";
            string title     = $"Gen {generationNumber} — Aṣẹ́gun of {PathMotif.TitleOf(ancestor.path)}{emberMark}";
            string remembrance = ancestor.remembrance ?? string.Empty;

            return new ShrineAncestorRow
            {
                SilhouetteColor = pathColor.WithAlpha(alpha),
                Title           = title,
                Remembrance     = remembrance,
            };
        }
    }
}
