namespace OriAscendant.UI
{
    /// <summary>
    /// Idle hero-breathing animation state (ADR-0005). Owns the breath clock and returns
    /// the (scale, brightness) to apply to the silhouette of light each frame. Pure — no
    /// MonoBehaviour, headlessly testable like ColdOpenBeat/OverlayTransition.
    ///
    /// The caller composes in the separate tap-pulse scale (1.0 when idle) and applies the
    /// brightness to the silhouette + vessel fill. Reduce Motion silences the sine
    /// (MotionHelper.BreathingSine returns 0), leaving a perfectly still bust.
    /// </summary>
    public struct BreathingDriver
    {
        /// <summary>Breathing period — ~0.24 Hz, below the 0.5 Hz distraction threshold.</summary>
        public const float PeriodSeconds = 4.2f;

        /// <summary>Scale oscillation amplitude (±1.2%).</summary>
        public const float ScaleAmp = 0.012f;

        /// <summary>Brightness oscillation amplitude (±7%).</summary>
        public const float BrightAmp = 0.07f;

        private float _time;

        /// <summary>Advances the breath clock and returns the (scale, brightness) to apply.
        /// <paramref name="tapPulseScale"/> is the separate tap-response scale (1.0 when idle),
        /// multiplied into the scale so both motions compose without clamping.</summary>
        public (float scale, float bright) Tick(float dt, float tapPulseScale, bool reduceMotion)
        {
            _time += dt;
            float breathe = MotionHelper.BreathingSine(_time, PeriodSeconds, reduceMotion);
            float scale = (1f + breathe * ScaleAmp) * tapPulseScale;
            float bright = 1f + breathe * BrightAmp;
            return (scale, bright);
        }
    }
}
