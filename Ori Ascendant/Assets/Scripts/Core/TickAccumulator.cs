namespace OriAscendant.Core
{
    /// <summary>
    /// Frame-accumulator for the 1-second logical tick (TECH_DESIGN §6, amended
    /// decision). The remainder carries across frames, so long-run drift is zero —
    /// unlike InvokeRepeating (scaled time, no remainder) or naive WaitForSeconds
    /// loops (overshoot up to a frame per cycle). Plain C# core; the MonoBehaviour
    /// host feeds it Time.unscaledDeltaTime.
    /// </summary>
    public sealed class TickAccumulator
    {
        private readonly double _tickSeconds;
        private double _accumulated;

        /// <summary>Seconds carried toward the next tick (exposed for tests).</summary>
        public double Remainder => _accumulated;

        public TickAccumulator(double tickSeconds = 1.0)
        {
            _tickSeconds = tickSeconds > 0.0 ? tickSeconds : 1.0;
        }

        /// <summary>
        /// Advances by a frame delta and returns how many whole ticks elapsed.
        /// Negative or NaN deltas are ignored (defensive: clock hiccups must
        /// never produce negative progress).
        /// </summary>
        public int Advance(double deltaSeconds)
        {
            if (double.IsNaN(deltaSeconds) || deltaSeconds <= 0.0) return 0;

            _accumulated += deltaSeconds;
            int ticks = 0;
            while (_accumulated >= _tickSeconds)
            {
                _accumulated -= _tickSeconds;
                ticks++;
            }
            return ticks;
        }

        /// <summary>Drops any partial progress (used when re-syncing after resume).</summary>
        public void Reset() => _accumulated = 0.0;
    }
}
