using System;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure math for the Crossing ceremony — vessel light rises to ignite a new star
    /// (issue #34, PRD W3).
    ///
    /// When the tribulation resolves, the overflow column (CrossingColumnSpec) fades
    /// out while a new constellation star flashes up at the column apex. Ascended =
    /// bright in path colour; fallen = ember (never invisible — no dead end).
    ///
    /// No MonoBehaviour, no UnityEngine references — headlessly testable on Linux.
    /// MainScreenSkin drives both the column exit and the star ignition each frame.
    /// </summary>
    public static class CrossingCeremonySpec
    {
        /// <summary>Crossing ceremony fires when the column reaches its apex —
        /// aligns with tribulation eligibility (CrossingColumnSpec.ApexFraction = 1.0).</summary>
        public const double TriggerFraction = CrossingColumnSpec.ApexFraction;

        /// <summary>Duration of the new-star flash (column-apex → star settled), in seconds.</summary>
        public const float StarIgnitionSeconds = 1.2f;

        /// <summary>Duration of the overflow column fade-out during the ceremony, in seconds.</summary>
        public const float ColumnFadeSeconds = 0.6f;

        /// <summary>Final alpha of the new star after a successful Crossing (full brightness).</summary>
        public const float AscendedStarAlpha = 1.0f;

        /// <summary>Final alpha of the new star after a fall (warm ember — present, honoured, softer).
        /// Matches ConstellationStarMapper.FallenAlpha so the ceremony settles into the
        /// permanent deep-field appearance without a visible snap.</summary>
        public const float EmberStarAlpha = 0.45f;

        /// <summary>How much the ignition flash overshoots the settled alpha at its peak.
        /// The kindling rises to (settle + this) — capped at 1.0 — before easing back to settle.
        /// Most visible for fallen cultivators, where the ember settles well below 1.</summary>
        public const float StarIgnitionPeakBoost = 0.55f;

        /// <summary>Returns the settled alpha the new star holds after the ceremony ends.</summary>
        public static float NewStarAlpha(bool didAscend) =>
            didAscend ? AscendedStarAlpha : EmberStarAlpha;

        /// <summary>
        /// Star ignition flash alpha: 0 → peak → settle at NewStarAlpha(didAscend).
        /// The flash reads as "kindling" — it rises to peak brightness before
        /// settling, most visible for fallen cultivators where the ember settles below 1.
        /// Returns NewStarAlpha when elapsed &gt;= duration (ceremony settled).
        /// </summary>
        public static float StarIgnitionAlpha(float elapsed, float duration, bool didAscend)
        {
            float settle = NewStarAlpha(didAscend);
            if (duration <= 0f || elapsed >= duration) return settle;
            double t = Math.Max(0.0, Math.Min(1.0, elapsed / (double)duration));
            float peak = (float)Math.Min(1.0, settle + StarIgnitionPeakBoost);
            return t < 0.5
                ? Lerp(0f, peak, (float)(t * 2.0))
                : Lerp(peak, settle, (float)((t - 0.5) * 2.0));
        }

        /// <summary>
        /// Column exit alpha: 1 → 0 over ColumnFadeSeconds.
        /// The column fades out as the ceremony fires, handing its light to the new star.
        /// Returns 0 when elapsed &gt;= duration.
        /// </summary>
        public static float ColumnExitAlpha(float elapsed, float duration)
        {
            if (duration <= 0f || elapsed >= duration) return 0f;
            float t = (float)Math.Max(0.0, Math.Min(1.0, elapsed / (double)duration));
            return Lerp(1f, 0f, t);
        }

        /// <summary>Returns true while the star ignition is still playing.</summary>
        public static bool IsActive(float elapsed) => elapsed < StarIgnitionSeconds;

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
