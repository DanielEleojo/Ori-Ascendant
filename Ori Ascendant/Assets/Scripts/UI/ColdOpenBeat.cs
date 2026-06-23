using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure-math animation state for the cold-open launch beat (issue #32).
    /// No MonoBehaviour, no scene — headlessly testable on Linux.
    ///
    /// Normal mode sequence (all delays are cumulative from t=0):
    ///   [0 .. KindleDuration]           — silhouette kindles from darkness (0→1)
    ///   [TitleRevealDelay .. +TitleRevealDuration] — game title fades in
    ///   [RevealDelay      .. +RevealDuration]      — proverb + tap-prompt fade in
    ///
    /// A tap at any point calls Skip(), which flags IsDone so the skin can start
    /// its own close-transition.
    ///
    /// Reduce Motion: all four elements fade in together over ReduceMotionFadeDuration
    /// (no kinetic "emerging" motion — plain alpha per iOS RM guidelines).
    /// </summary>
    public struct ColdOpenBeat
    {
        /// <summary>Seconds for the silhouette to kindle from darkness to full light.</summary>
        public const float KindleDuration = 1.2f;

        /// <summary>Seconds after start when the game title begins to appear.</summary>
        public const float TitleRevealDelay = 0.6f;

        /// <summary>Seconds for the game title to fade from 0 to 1.</summary>
        public const float TitleRevealDuration = 0.5f;

        /// <summary>Seconds after start when the proverb and tap-prompt begin to appear.</summary>
        public const float RevealDelay = 1.0f;

        /// <summary>Seconds for the proverb and tap-prompt to fade from 0 to 1.</summary>
        public const float RevealDuration = 0.6f;

        /// <summary>Reduce Motion fade: all elements appear together in this many seconds.</summary>
        public const float ReduceMotionFadeDuration = 0.3f;

        private float _elapsed;
        private bool _done;

        /// <summary>True after Skip() has been called. The skin should start its close transition.</summary>
        public bool IsDone => _done;

        /// <summary>Mark the beat as done (player tapped to enter, or first-launch gate passed).</summary>
        public void Skip() => _done = true;

        /// <summary>
        /// Advances the beat by <paramref name="dt"/> seconds and returns the current
        /// (silhouetteAlpha, titleAlpha, proverbAlpha, promptAlpha) to apply this frame.
        /// All values are in [0, 1]. When IsDone, returns (0, 0, 0, 0) — the skin
        /// should stop applying beat values and run its own close transition instead.
        /// </summary>
        public (float silhouette, float title, float proverb, float prompt) Tick(float dt, bool reduceMotion)
        {
            if (_done) return (0f, 0f, 0f, 0f);

            _elapsed += dt;

            if (reduceMotion)
            {
                float t = MotionHelper.EaseOut(Mathf.Clamp01(_elapsed / ReduceMotionFadeDuration));
                return (t, t, t, t);
            }

            float silhouette = MotionHelper.EaseOut(Mathf.Clamp01(_elapsed / KindleDuration));

            float titleElapsed = _elapsed - TitleRevealDelay;
            float title = titleElapsed <= 0f
                ? 0f
                : MotionHelper.EaseOut(Mathf.Clamp01(titleElapsed / TitleRevealDuration));

            float revealElapsed = _elapsed - RevealDelay;
            float reveal = revealElapsed <= 0f
                ? 0f
                : MotionHelper.EaseOut(Mathf.Clamp01(revealElapsed / RevealDuration));

            return (silhouette, title, reveal, reveal);
        }
    }
}
