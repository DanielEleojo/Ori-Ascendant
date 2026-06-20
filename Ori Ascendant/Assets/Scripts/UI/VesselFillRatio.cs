using System;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure fill-ratio function for the vessel silhouette (issue #25, PRD W2).
    ///
    /// fill = (stage + clamp01(progressFraction)) / totalStages
    ///
    /// Monotonic guarantee: advancing one stage increments the numerator by exactly
    /// 1, which equals the previous stage's clamped-to-1 progress term. No drop
    /// at any stage boundary. Stage 0 at 0 Àṣẹ ≈ 0; final stage at full Àṣẹ = 1.
    /// </summary>
    public static class VesselFillRatio
    {
        /// <summary>
        /// Returns a fill value in [0, 1].
        /// </summary>
        /// <param name="stage">0-based current stage index.</param>
        /// <param name="progressFraction">currentAse / stageTarget, clamped internally to [0, 1].</param>
        /// <param name="totalStages">Total stage count (e.g. 6 for MVP).</param>
        public static float Compute(int stage, double progressFraction, int totalStages)
        {
            if (totalStages <= 0) return 0f;
            double clamped = Math.Max(0.0, Math.Min(1.0, progressFraction));
            double fill = (stage + clamped) / totalStages;
            return (float)Math.Max(0.0, Math.Min(1.0, fill));
        }
    }
}
