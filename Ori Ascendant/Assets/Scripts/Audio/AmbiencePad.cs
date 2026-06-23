using System;

namespace OriAscendant.Audio
{
    /// <summary>
    /// Pure PCM generator for the default ambient pad (ADR-0001 procedural-asset
    /// ethos — no imported audio files). Generates a mono seamless-looping drone
    /// by summing a small set of sine partials. Every frequency and LFO rate is
    /// snapped to an integer number of cycles over the loop so the waveform meets
    /// itself with no click at the seam.
    ///
    /// All constants marked TUNE-BY-EAR are the values to adjust once you can
    /// hear the output on a device. The generator is deterministic — no RNG —
    /// so the same args always produce the same buffer.
    /// </summary>
    public static class AmbiencePad
    {
        // ── TUNE-BY-EAR: partial layout ─────────────────────────────────────
        // Target frequencies (Hz) before cycle-snapping.  A drone rooted around
        // A2 (110 Hz) with octave (220), fifth (165), and a soft upper air partial.
        // Lowering RootTarget makes the pad warmer / more bass-forward.
        private const double RootTarget   = 110.0; // A2  — foundation drone
        private const double OctaveTarget = 220.0; // A3  — body / warmth
        private const double FifthTarget  = 165.0; // E3  — consonant colour
        private const double AirTarget    = 440.0; // A4  — soft presence in highs

        // TUNE-BY-EAR: partial amplitudes — must sum < 1.0 before master scale.
        // Root loudest; octave & fifth supporting; air barely audible.
        private const double AmpRoot   = 0.42;
        private const double AmpOctave = 0.25;
        private const double AmpFifth  = 0.18;
        private const double AmpAir    = 0.07;

        // TUNE-BY-EAR: slow amplitude LFO (a "breathing" swell).
        // Rate ~0.07 Hz → one cycle every ~14 s; must be an integer number of
        // cycles over the loop so it, too, meets itself cleanly.
        private const double LfoTargetHz  = 0.07; // target swell rate
        private const double LfoDepth     = 0.28; // 0 = static level, 1 = silence→full swing

        // TUNE-BY-EAR: master output level — headroom below digital full-scale.
        // 0.55 leaves a comfortable safety margin; increase toward 0.75 for more
        // presence, decrease toward 0.35 for a quieter bed.
        private const double MasterLevel = 0.55;
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a seamless-looping mono PCM buffer.
        /// </summary>
        /// <param name="sampleRate">Samples per second (e.g. 22050 or 44100).</param>
        /// <param name="loopSeconds">Loop duration in seconds (e.g. 12–16 s).</param>
        /// <returns>Mono PCM floats in [-1,1], length == round(sampleRate * loopSeconds).</returns>
        public static float[] Generate(int sampleRate, float loopSeconds)
        {
            int sampleCount = (int)Math.Round((double)sampleRate * loopSeconds);
            float[] pcm = new float[sampleCount];

            double duration = (double)sampleCount / sampleRate; // exact duration from sample count

            // Snap every frequency to an integer number of cycles over the loop so
            // the partial phase at sample 0 exactly equals its phase at sample n-1+1.
            double freqRoot   = Math.Round(RootTarget   * duration) / duration;
            double freqOctave = Math.Round(OctaveTarget * duration) / duration;
            double freqFifth  = Math.Round(FifthTarget  * duration) / duration;
            double freqAir    = Math.Round(AirTarget    * duration) / duration;
            double freqLfo    = Math.Round(LfoTargetHz  * duration) / duration;

            double twoPi = 2.0 * Math.PI;
            double samplePeriod = 1.0 / sampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                double t = i * samplePeriod;

                // Amplitude LFO: cycles ≥ 1, maps to 0..1 swing.
                // LfoDepth=0.28 → level varies between (1-0.28)=0.72 and 1.0 of MasterLevel.
                double lfo = 1.0 - LfoDepth * 0.5 * (1.0 - Math.Cos(twoPi * freqLfo * t));

                // Sum partials.
                double sample = AmpRoot   * Math.Sin(twoPi * freqRoot   * t)
                              + AmpOctave * Math.Sin(twoPi * freqOctave * t)
                              + AmpFifth  * Math.Sin(twoPi * freqFifth  * t)
                              + AmpAir    * Math.Sin(twoPi * freqAir    * t);

                pcm[i] = (float)(sample * lfo * MasterLevel);
            }

            return pcm;
        }
    }
}
