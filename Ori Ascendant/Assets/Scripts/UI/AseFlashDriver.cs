namespace OriAscendant.UI
{
    /// <summary>
    /// Hero Àṣẹ-counter flash state (issue #24). Watches a change-key string; when the key
    /// changes, restarts a warm flash whose alpha follows MotionHelper.FlashAlpha. Pure —
    /// owns the flash clock and the last-seen value, headlessly testable. The caller flips
    /// the key on meaningful events (channel/advance/collect), so the driver stays generic.
    ///
    /// The caller lerps the counter colour (gold → core) by the returned alpha. The first
    /// observed value never flashes (the driver starts settled), matching the original
    /// "large initial elapsed" behaviour.
    /// </summary>
    public struct AseFlashDriver
    {
        /// <summary>Duration of one number-change flash (0 → 1 → 0 sine arch).</summary>
        public const float Duration = 0.5f;

        private float _elapsed;
        private string _lastValue;

        /// <summary>Advances the flash clock and returns the flash alpha for this frame.
        /// A change in <paramref name="currentValue"/> after the first observed value
        /// restarts the flash; the first observed value primes the watcher without flashing.</summary>
        public float Tick(float dt, string currentValue, bool reduceMotion)
        {
            if (_lastValue == null)
            {
                // First observed value — prime without flashing (start settled).
                _lastValue = currentValue;
                _elapsed = Duration;
                return MotionHelper.FlashAlpha(_elapsed, Duration, reduceMotion);
            }

            if (currentValue != _lastValue)
                _elapsed = 0f;       // value just changed — start a fresh flash
            else
                _elapsed += dt;      // settle out otherwise

            _lastValue = currentValue;
            return MotionHelper.FlashAlpha(_elapsed, Duration, reduceMotion);
        }
    }
}
