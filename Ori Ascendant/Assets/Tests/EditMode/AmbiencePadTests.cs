using System;
using NUnit.Framework;
using OriAscendant.Audio;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate: pure PCM correctness of <see cref="AmbiencePad.Generate"/>.
    /// All tests are headless — no Unity AudioClip required.
    /// </summary>
    public class AmbiencePadTests
    {
        private const int   SampleRate  = 22050;
        private const float LoopSeconds = 14f;

        // Pre-generate once for the suite; reuse across tests.
        private static float[] _pcm;

        [OneTimeSetUp]
        public void GenerateOnce()
        {
            _pcm = AmbiencePad.Generate(SampleRate, LoopSeconds);
        }

        // ── 1. sample count ─────────────────────────────────────────────────

        [Test]
        public void SampleCount_EqualsRoundedProduct()
        {
            int expected = (int)Math.Round((double)SampleRate * LoopSeconds);
            Assert.AreEqual(expected, _pcm.Length,
                "Sample count must equal round(sampleRate * loopSeconds).");
        }

        // ── 2. amplitude safety ──────────────────────────────────────────────

        [Test]
        public void AllSamples_AreFinite_AndWithinClipRange()
        {
            for (int i = 0; i < _pcm.Length; i++)
            {
                float s = _pcm[i];
                Assert.IsTrue(float.IsFinite(s),
                    $"Sample [{i}] is not finite: {s}");
                Assert.IsTrue(s >= -1f && s <= 1f,
                    $"Sample [{i}] = {s} is outside [-1, 1].");
            }
        }

        // ── 3. non-silence ───────────────────────────────────────────────────

        [Test]
        public void Buffer_IsNotSilent()
        {
            bool anyLoud = false;
            for (int i = 0; i < _pcm.Length; i++)
            {
                if (Math.Abs(_pcm[i]) > 0.01f) { anyLoud = true; break; }
            }
            Assert.IsTrue(anyLoud, "Buffer must contain at least one sample with |v| > 0.01.");
        }

        // ── 4. loop seam continuity ──────────────────────────────────────────

        [Test]
        public void LoopSeam_FirstAndLastSamples_AreClose()
        {
            // Integer-cycle design guarantees the waveform meets itself at the seam.
            // Tolerance of 0.05 allows a single LFO-swell step at the seam.
            float first = _pcm[0];
            float last  = _pcm[_pcm.Length - 1];
            Assert.IsTrue(Math.Abs(first - last) < 0.05f,
                $"Loop seam discontinuity too large: first={first:F4} last={last:F4} " +
                $"delta={Math.Abs(first - last):F4} (must be < 0.05).");
        }

        // ── 5. determinism ───────────────────────────────────────────────────

        [Test]
        public void TwoGenerateCalls_ProduceIdenticalBuffers()
        {
            float[] a = AmbiencePad.Generate(SampleRate, LoopSeconds);
            float[] b = AmbiencePad.Generate(SampleRate, LoopSeconds);

            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i], b[i],
                    $"Sample [{i}] differs: {a[i]} vs {b[i]}. Generate must be deterministic.");
            }
        }
    }
}
