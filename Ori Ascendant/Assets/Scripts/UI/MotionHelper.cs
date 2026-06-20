using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure-math motion primitives for hand-rolled UI animation (ADR-0003).
    /// No MonoBehaviour, no third-party tween library — all fns are static and
    /// headlessly testable on Linux. Callers own time tracking and the reduce-motion
    /// flag; they pass both in on every call.
    ///
    /// Easing contract: quadratic ease-out — fast start, soft landing, no bounce.
    /// Reduce-motion contract: scale/position motion is silenced (returns 0 / snaps
    /// to target); alpha tweens may still run per iOS Reduce Motion guidelines.
    /// </summary>
    public static class MotionHelper
    {
        // ---- Easing ----

        /// <summary>Quadratic ease-out: fast start, soft landing, no bounce.
        /// Returns 0 at t=0 and 1 at t=1; clamped outside [0,1].</summary>
        public static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        // ---- Tweens ----

        /// <summary>Evaluates a single-value ease-out tween.
        /// When <paramref name="reduceMotion"/> is true or <paramref name="duration"/> ≤ 0,
        /// snaps immediately to <paramref name="to"/>.</summary>
        public static float Tween(float from, float to, float elapsed, float duration, bool reduceMotion)
        {
            if (reduceMotion || duration <= 0f) return to;
            return Mathf.Lerp(from, to, EaseOut(elapsed / duration));
        }

        // ---- Accessibility ----

        /// <summary>Returns true when the player has enabled Reduce Motion.
        /// Written by a future iOS native bridge (ADR-0004) via PlayerPrefs;
        /// defaults to false on all other platforms.</summary>
        public static bool IsReduceMotion() =>
            PlayerPrefs.GetInt("ReduceMotion", 0) != 0;

        // ---- Micro-feedback motions (issue #24) ----

        /// <summary>Scale for press-dip recovery: 0.96 at elapsed=0, eases to 1.0 at
        /// elapsed=duration. When <paramref name="reduceMotion"/> is true, snaps to 1.0
        /// (scale motion silenced per iOS Reduce Motion).</summary>
        public static float PressDipScale(float elapsed, float duration, bool reduceMotion) =>
            Tween(0.96f, 1.0f, elapsed, duration, reduceMotion);

        /// <summary>Alpha envelope for a warm number-flash: 0 → 1 → 0 over duration
        /// (sine arch, peaks at half-duration). Reduce Motion softens the peak to 40%
        /// — alpha tweens are permitted per iOS RM guidelines.</summary>
        public static float FlashAlpha(float elapsed, float duration, bool reduceMotion)
        {
            if (duration <= 0f) return 0f;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Sin(t * Mathf.PI);
            return reduceMotion ? alpha * 0.4f : alpha;
        }

        /// <summary>Scale factor for a brief tap-response pulse: 1.0 → (1.0+amplitude) → 1.0
        /// (sine arch, peaks at half-duration). When <paramref name="reduceMotion"/> is true,
        /// returns 1.0 (scale motion silenced per iOS Reduce Motion).</summary>
        public static float TapPulseScale(float elapsed, float duration, float amplitude, bool reduceMotion)
        {
            if (reduceMotion || duration <= 0f) return 1.0f;
            float t = Mathf.Clamp01(elapsed / duration);
            return 1.0f + amplitude * Mathf.Sin(t * Mathf.PI);
        }

        // ---- Idle breathing ----

        /// <summary>Sine value for a slow idle breathing oscillation.
        /// Returns a value in [-1, 1] — multiply by amplitude before applying.
        /// When <paramref name="reduceMotion"/> is true, always returns 0 (motion
        /// silenced per iOS Reduce Motion).</summary>
        public static float BreathingSine(float time, float periodSeconds, bool reduceMotion)
        {
            if (reduceMotion) return 0f;
            return Mathf.Sin(time * (Mathf.PI * 2f / periodSeconds));
        }
    }
}
