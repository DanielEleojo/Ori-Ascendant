using System;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure math for the tribulation overflow column — the Crossing gauge (issue #33, PRD W2).
    ///
    /// At the final stage the vessel "brims" and light overflows upward into a rising
    /// column. Column height and alpha both map linearly from the tribulation fraction
    /// (0 → 1), reaching their apex when tribulation becomes eligible (fraction = 1.0).
    ///
    /// No MonoBehaviour, no UnityEngine references — headlessly testable on Linux.
    /// MainScreenSkin reads these values to position and tint the column each frame.
    /// </summary>
    public static class CrossingColumnSpec
    {
        /// <summary>Maximum column height in pixels above the vessel top.</summary>
        public const float MaxColumnHeight = 200f;

        /// <summary>Stage index at which the overflow column activates (final stage in MVP).</summary>
        public const int ActiveFromStage = 5;

        /// <summary>
        /// Tribulation fraction at which the column reaches its apex — aligns with
        /// tribulation eligibility (currentAse / aseThreshold = 1.0).
        /// </summary>
        public const double ApexFraction = 1.0;

        /// <summary>Returns column height in pixels for the given tribulation fraction.</summary>
        public static float ColumnHeight(double fraction)
        {
            double clamped = Math.Max(0.0, Math.Min(1.0, fraction));
            return (float)(clamped * MaxColumnHeight);
        }

        /// <summary>Returns column glow alpha (0 at zero progress, 1.0 at tribulation eligibility).</summary>
        public static float ColumnAlpha(double fraction)
        {
            double clamped = Math.Max(0.0, Math.Min(1.0, fraction));
            return (float)clamped;
        }

        /// <summary>Returns true when the overflow column should be shown (final stage only).</summary>
        public static bool IsActive(int stage) => stage >= ActiveFromStage;
    }
}
