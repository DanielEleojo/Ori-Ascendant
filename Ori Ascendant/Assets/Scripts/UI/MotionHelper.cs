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
