using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Fade-scale transition for overlay screens (issue #23 / ADR-0005).
    /// 200 ms ease-out open: alpha 0→1, scale ScaleFrom→1.
    /// Reverse on close: alpha 1→0, scale 1→ScaleFrom.
    /// Reduce Motion collapses to a plain alpha fade — scale stays at 1.
    ///
    /// Use <see cref="Tick"/> for raw (alpha, scale) values when callers want to apply
    /// them themselves; use <see cref="TickAndApply"/> for the common case of writing
    /// straight to a CanvasGroup and Transform. Check IsFullyClosed after either to
    /// know when to call SetActive(false).
    /// </summary>
    public struct OverlayTransition
    {
        /// <summary>Duration of the open or close animation in seconds.</summary>
        public const float Duration = 0.2f;

        /// <summary>Starting scale on open (and ending scale on close): 0.97→1.</summary>
        public const float ScaleFrom = 0.97f;

        private enum Phase { Closed, Opening, Open, Closing }

        private Phase _phase;
        private float _elapsed;

        /// <summary>True only when the overlay is fully closed (not animating).</summary>
        public bool IsFullyClosed => _phase == Phase.Closed;

        /// <summary>Request an open. Resets elapsed so each call is a clean start.</summary>
        public void Open()
        {
            _phase = Phase.Opening;
            _elapsed = 0f;
        }

        /// <summary>Request a close. No-op when already closed.</summary>
        public void Close()
        {
            if (_phase == Phase.Closed) return;
            _phase = Phase.Closing;
            _elapsed = 0f;
        }

        /// <summary>
        /// Advances the transition by <paramref name="dt"/> seconds and returns
        /// the (alpha, scale) to apply this frame.
        /// When <paramref name="reduceMotion"/> is true, scale always returns 1.
        /// </summary>
        public (float alpha, float scale) Tick(float dt, bool reduceMotion)
        {
            switch (_phase)
            {
                case Phase.Opening:
                {
                    _elapsed += dt;
                    float t = MotionHelper.EaseOut(Mathf.Clamp01(_elapsed / Duration));
                    if (_elapsed >= Duration) { _phase = Phase.Open; t = 1f; }
                    float s = reduceMotion ? 1f : Mathf.Lerp(ScaleFrom, 1f, t);
                    return (t, s);
                }
                case Phase.Open:
                    return (1f, 1f);

                case Phase.Closing:
                {
                    _elapsed += dt;
                    float t = MotionHelper.EaseOut(Mathf.Clamp01(_elapsed / Duration));
                    if (_elapsed >= Duration) { _phase = Phase.Closed; t = 1f; }
                    float s = reduceMotion ? 1f : Mathf.Lerp(1f, ScaleFrom, t);
                    return (1f - t, s);
                }
                default: // Closed
                    return (0f, 1f);
            }
        }

        /// <summary>
        /// Convenience: ticks the transition and writes the resulting alpha onto
        /// <paramref name="canvasGroup"/> and a uniform scale onto <paramref name="transform"/>.
        /// Both targets may be null; missing writes are skipped. Returns IsFullyClosed
        /// so callers can chain <c>if (… ) _root.SetActive(false)</c>.
        /// </summary>
        public bool TickAndApply(CanvasGroup canvasGroup, Transform transform, float dt, bool reduceMotion)
        {
            var (alpha, scale) = Tick(dt, reduceMotion);
            if (canvasGroup != null) canvasGroup.alpha = alpha;
            if (transform != null) transform.localScale = new Vector3(scale, scale, 1f);
            return IsFullyClosed;
        }
    }
}
