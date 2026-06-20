using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for ColdOpenBeat (issue #32).
    /// No scene, no MonoBehaviour — the struct is pure math over elapsed time.
    /// </summary>
    public class ColdOpenBeatTests
    {
        // ---- initial state ----

        [Test]
        public void NewBeat_IsNotDone()
        {
            var b = new ColdOpenBeat();
            Assert.IsFalse(b.IsDone, "A fresh ColdOpenBeat must not be done");
        }

        [Test]
        public void NewBeat_Tick_SilhouetteAlphaIsZero()
        {
            var b = new ColdOpenBeat();
            var (s, _, _) = b.Tick(0f, false);
            Assert.AreEqual(0f, s, 0.001f, "Silhouette alpha must be 0 at elapsed=0");
        }

        [Test]
        public void NewBeat_Tick_ProverbAlphaIsZero()
        {
            var b = new ColdOpenBeat();
            var (_, p, _) = b.Tick(0f, false);
            Assert.AreEqual(0f, p, 0.001f, "Proverb alpha must be 0 at elapsed=0");
        }

        // ---- skip ----

        [Test]
        public void Skip_MakesDone()
        {
            var b = new ColdOpenBeat();
            b.Skip();
            Assert.IsTrue(b.IsDone, "Skip() must set IsDone");
        }

        [Test]
        public void AfterSkip_Tick_ReturnsAllZero()
        {
            var b = new ColdOpenBeat();
            b.Skip();
            var (s, p, prompt) = b.Tick(0.5f, false);
            Assert.AreEqual(0f, s, 0.001f, "Silhouette must be 0 after skip");
            Assert.AreEqual(0f, p, 0.001f, "Proverb must be 0 after skip");
            Assert.AreEqual(0f, prompt, 0.001f, "Prompt must be 0 after skip");
        }

        // ---- silhouette kindles in ----

        [Test]
        public void Tick_AtKindleDuration_SilhouetteIsOne()
        {
            var b = new ColdOpenBeat();
            var (s, _, _) = b.Tick(ColdOpenBeat.KindleDuration, false);
            Assert.AreEqual(1f, s, 0.001f, "Silhouette must reach 1 by KindleDuration");
        }

        [Test]
        public void Tick_MidKindleDuration_SilhouetteEasedOutPastLinear()
        {
            var b = new ColdOpenBeat();
            var (s, _, _) = b.Tick(ColdOpenBeat.KindleDuration * 0.5f, false);
            Assert.Greater(s, 0.5f, "Ease-out: silhouette at half-duration must exceed 0.5");
        }

        [Test]
        public void Tick_SilhouetteAlwaysInZeroToOne()
        {
            for (float t = 0f; t <= ColdOpenBeat.KindleDuration + 0.5f; t += 0.1f)
            {
                var b = new ColdOpenBeat();
                var (s, _, _) = b.Tick(t, false);
                Assert.GreaterOrEqual(s, 0f, $"Silhouette below 0 at t={t}");
                Assert.LessOrEqual(s, 1.001f, $"Silhouette above 1 at t={t}");
            }
        }

        // ---- proverb and prompt reveal ----

        [Test]
        public void Tick_BeforeRevealDelay_ProverbIsZero()
        {
            var b = new ColdOpenBeat();
            // tick to just before the delay without accumulating time across calls
            var (_, p, _) = b.Tick(ColdOpenBeat.RevealDelay - 0.05f, false);
            Assert.AreEqual(0f, p, 0.001f, "Proverb must be 0 before RevealDelay");
        }

        [Test]
        public void Tick_AfterRevealDelayPlusDuration_ProverbIsOne()
        {
            var b = new ColdOpenBeat();
            var (_, p, _) = b.Tick(ColdOpenBeat.RevealDelay + ColdOpenBeat.RevealDuration, false);
            Assert.AreEqual(1f, p, 0.001f, "Proverb must reach 1 after RevealDelay + RevealDuration");
        }

        [Test]
        public void Tick_ProverbAndPromptAlwaysEqual()
        {
            for (float t = 0f; t <= ColdOpenBeat.RevealDelay + ColdOpenBeat.RevealDuration + 0.5f; t += 0.1f)
            {
                var b = new ColdOpenBeat();
                var (_, p, prompt) = b.Tick(t, false);
                Assert.AreEqual(p, prompt, 0.001f, $"Proverb and prompt alpha must match at t={t}");
            }
        }

        // ---- reduce motion ----

        [Test]
        public void ReduceMotion_AllAlphasEqualAtAllTimes()
        {
            for (float t = 0f; t <= ColdOpenBeat.ReduceMotionFadeDuration + 0.5f; t += 0.05f)
            {
                var b = new ColdOpenBeat();
                var (s, p, prompt) = b.Tick(t, true);
                Assert.AreEqual(s, p, 0.001f, $"ReduceMotion: silhouette and proverb must match at t={t}");
                Assert.AreEqual(p, prompt, 0.001f, $"ReduceMotion: proverb and prompt must match at t={t}");
            }
        }

        [Test]
        public void ReduceMotion_AtFadeDuration_AllAlphasAreOne()
        {
            var b = new ColdOpenBeat();
            var (s, p, prompt) = b.Tick(ColdOpenBeat.ReduceMotionFadeDuration, true);
            Assert.AreEqual(1f, s, 0.001f, "ReduceMotion: silhouette must reach 1 at ReduceMotionFadeDuration");
            Assert.AreEqual(1f, p, 0.001f, "ReduceMotion: proverb must reach 1 at ReduceMotionFadeDuration");
            Assert.AreEqual(1f, prompt, 0.001f, "ReduceMotion: prompt must reach 1 at ReduceMotionFadeDuration");
        }

        [Test]
        public void ReduceMotion_AtZero_AllAlphasAreZero()
        {
            var b = new ColdOpenBeat();
            var (s, p, prompt) = b.Tick(0f, true);
            Assert.AreEqual(0f, s, 0.001f, "ReduceMotion: silhouette alpha is 0 at elapsed=0");
            Assert.AreEqual(0f, p, 0.001f, "ReduceMotion: proverb alpha is 0 at elapsed=0");
            Assert.AreEqual(0f, prompt, 0.001f, "ReduceMotion: prompt alpha is 0 at elapsed=0");
        }

        [Test]
        public void ReduceMotion_BeyondFadeDuration_AllAlphasClampAtOne()
        {
            var b = new ColdOpenBeat();
            var (s, p, _) = b.Tick(ColdOpenBeat.ReduceMotionFadeDuration * 3f, true);
            Assert.AreEqual(1f, s, 0.001f, "ReduceMotion: alpha must clamp at 1 beyond fade duration");
            Assert.AreEqual(1f, p, 0.001f, "ReduceMotion: proverb must clamp at 1 beyond fade duration");
        }

        // ---- elapsed accumulates across calls ----

        [Test]
        public void Tick_AccumulatesElapsedAcrossCalls()
        {
            var b = new ColdOpenBeat();
            b.Tick(ColdOpenBeat.RevealDelay * 0.5f, false);
            var (_, p, _) = b.Tick(ColdOpenBeat.RevealDelay * 0.5f + ColdOpenBeat.RevealDuration, false);
            Assert.AreEqual(1f, p, 0.001f,
                "Elapsed must accumulate: two ticks summing to RevealDelay+RevealDuration must yield proverb=1");
        }

        // ---- silhouette stays fully lit after kindling complete ----

        [Test]
        public void Tick_AfterKindleDuration_SilhouetteRemainsOne()
        {
            var b = new ColdOpenBeat();
            var (s, _, _) = b.Tick(ColdOpenBeat.KindleDuration + 2f, false);
            Assert.AreEqual(1f, s, 0.001f, "Silhouette must stay at 1 past KindleDuration");
        }
    }
}
