namespace OriAscendant.UI
{
    /// <summary>
    /// Motion timing tokens — the one place to tune global UI "feel" (ADR-0005 companion).
    /// Mirrors <see cref="TypographicScale"/>/<see cref="SpacingScale"/>/<see cref="OpacitySpec"/>:
    /// the repo's "no magic numbers — tokens for shared roles" rule applied to motion timing.
    ///
    /// Scope is deliberately narrow: durations and the ambient pulse frequencies — the knobs a
    /// "snappier vs. statelier" pass would turn. Per-motion CHARACTER (dip target 0.96, modal
    /// scale-from 0.97, breathing amplitude) stays local to its driver — those are identity, not
    /// timing. Reduce-Motion is enforced by the drivers via <see cref="MotionHelper.IsReduceMotion"/>,
    /// not here.
    /// </summary>
    public static class MotionScale
    {
        // ---- Micro-feedback durations (seconds) ----

        /// <summary>Press-dip release ease-back (ButtonPressDip).</summary>
        public const float PressDipRecover = 0.12f;

        /// <summary>Modal/screen open+close fade-scale (OverlayTransition).</summary>
        public const float OverlayTransition = 0.2f;

        /// <summary>One number-change flash arch (AseFlashDriver).</summary>
        public const float NumberFlash = 0.5f;

        // ---- Floating "+N" feedback ----

        /// <summary>Lifetime of a channel "+N" before it fades out (FloatingText).</summary>
        public const float FloatingTextLifetime = 0.8f;

        /// <summary>Vertical rise of a "+N" over its lifetime, in canvas px (FloatingText).</summary>
        public const float FloatingTextRisePixels = 70f;

        // ---- Ambient / hero pulse frequencies ----

        /// <summary>Idle hero breathing period — ~0.24 Hz, below the 0.5 Hz distraction line
        /// (BreathingDriver).</summary>
        public const float BreathingPeriod = 4.2f;

        /// <summary>CTA glow + vessel-waterline breath sine frequency, rad/s (~0.35 Hz)
        /// (MainScreenSkin).</summary>
        public const float CtaPulseFrequency = 2.2f;

        /// <summary>Ascension-FX overlay pulse sine frequency, multiplied by π
        /// (TribulationScreen).</summary>
        public const float AscensionPulseFrequency = 1.2f;
    }
}
