using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Pure-seam tests for ColdOpenBeat (issue #32).
    /// No scene, no MonoBehaviour — the struct is pure math over elapsed time.
    ///
    /// Tick() returns (silhouette, title, proverb, prompt); tests updated from the
    /// original 3-tuple after ColdOpenBeat gained the title element (Unit 5).
    /// </summary>
    public class ColdOpenBeatTests
    {
        // ColdOpenPrefs.HasSeen writes live PlayerPrefs — preserve & restore around
        // each test so cold-open gate state never leaks between tests (repo convention).
        private bool _coldOpenSeen;

        [SetUp]
        public void SetUp() => _coldOpenSeen = ColdOpenPrefs.HasSeen;

        [TearDown]
        public void TearDown() => ColdOpenPrefs.HasSeen = _coldOpenSeen;

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
            var (s, _, _, _) = b.Tick(0f, false);
            Assert.AreEqual(0f, s, 0.001f, "Silhouette alpha must be 0 at elapsed=0");
        }

        [Test]
        public void NewBeat_Tick_TitleAlphaIsZero()
        {
            var b = new ColdOpenBeat();
            var (_, t, _, _) = b.Tick(0f, false);
            Assert.AreEqual(0f, t, 0.001f, "Title alpha must be 0 at elapsed=0");
        }

        [Test]
        public void NewBeat_Tick_ProverbAlphaIsZero()
        {
            var b = new ColdOpenBeat();
            var (_, _, p, _) = b.Tick(0f, false);
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
            var (s, title, p, prompt) = b.Tick(0.5f, false);
            Assert.AreEqual(0f, s,      0.001f, "Silhouette must be 0 after skip");
            Assert.AreEqual(0f, title,  0.001f, "Title must be 0 after skip");
            Assert.AreEqual(0f, p,      0.001f, "Proverb must be 0 after skip");
            Assert.AreEqual(0f, prompt, 0.001f, "Prompt must be 0 after skip");
        }

        // ---- silhouette kindles in ----

        [Test]
        public void Tick_AtKindleDuration_SilhouetteIsOne()
        {
            var b = new ColdOpenBeat();
            var (s, _, _, _) = b.Tick(ColdOpenBeat.KindleDuration, false);
            Assert.AreEqual(1f, s, 0.001f, "Silhouette must reach 1 by KindleDuration");
        }

        [Test]
        public void Tick_MidKindleDuration_SilhouetteEasedOutPastLinear()
        {
            var b = new ColdOpenBeat();
            var (s, _, _, _) = b.Tick(ColdOpenBeat.KindleDuration * 0.5f, false);
            Assert.Greater(s, 0.5f, "Ease-out: silhouette at half-duration must exceed 0.5");
        }

        [Test]
        public void Tick_SilhouetteAlwaysInZeroToOne()
        {
            for (float t = 0f; t <= ColdOpenBeat.KindleDuration + 0.5f; t += 0.1f)
            {
                var b = new ColdOpenBeat();
                var (s, _, _, _) = b.Tick(t, false);
                Assert.GreaterOrEqual(s, 0f,     $"Silhouette below 0 at t={t}");
                Assert.LessOrEqual(s,   1.001f,  $"Silhouette above 1 at t={t}");
            }
        }

        // ---- title reveal ----

        [Test]
        public void Tick_BeforeTitleRevealDelay_TitleIsZero()
        {
            var b = new ColdOpenBeat();
            var (_, title, _, _) = b.Tick(ColdOpenBeat.TitleRevealDelay - 0.05f, false);
            Assert.AreEqual(0f, title, 0.001f, "Title must be 0 before TitleRevealDelay");
        }

        [Test]
        public void Tick_AfterTitleRevealDelayPlusDuration_TitleIsOne()
        {
            var b = new ColdOpenBeat();
            var (_, title, _, _) = b.Tick(
                ColdOpenBeat.TitleRevealDelay + ColdOpenBeat.TitleRevealDuration, false);
            Assert.AreEqual(1f, title, 0.001f,
                "Title must reach 1 after TitleRevealDelay + TitleRevealDuration");
        }

        [Test]
        public void Tick_TitleAlwaysInZeroToOne()
        {
            float end = ColdOpenBeat.TitleRevealDelay + ColdOpenBeat.TitleRevealDuration + 0.5f;
            for (float t = 0f; t <= end; t += 0.1f)
            {
                var b = new ColdOpenBeat();
                var (_, title, _, _) = b.Tick(t, false);
                Assert.GreaterOrEqual(title, 0f,    $"Title below 0 at t={t}");
                Assert.LessOrEqual(title,   1.001f, $"Title above 1 at t={t}");
            }
        }

        [Test]
        public void TitleRevealDelay_LessThan_RevealDelay()
        {
            Assert.Less(ColdOpenBeat.TitleRevealDelay, ColdOpenBeat.RevealDelay,
                "Title must begin appearing before the proverb");
        }

        [Test]
        public void AllDurations_ArePositive()
        {
            Assert.Greater(ColdOpenBeat.KindleDuration,         0f, "KindleDuration > 0");
            Assert.Greater(ColdOpenBeat.TitleRevealDelay,       0f, "TitleRevealDelay > 0");
            Assert.Greater(ColdOpenBeat.TitleRevealDuration,    0f, "TitleRevealDuration > 0");
            Assert.Greater(ColdOpenBeat.RevealDelay,            0f, "RevealDelay > 0");
            Assert.Greater(ColdOpenBeat.RevealDuration,         0f, "RevealDuration > 0");
            Assert.Greater(ColdOpenBeat.ReduceMotionFadeDuration, 0f, "ReduceMotionFadeDuration > 0");
        }

        // ---- proverb and prompt reveal ----

        [Test]
        public void Tick_BeforeRevealDelay_ProverbIsZero()
        {
            var b = new ColdOpenBeat();
            // tick to just before the delay without accumulating time across calls
            var (_, _, p, _) = b.Tick(ColdOpenBeat.RevealDelay - 0.05f, false);
            Assert.AreEqual(0f, p, 0.001f, "Proverb must be 0 before RevealDelay");
        }

        [Test]
        public void Tick_AfterRevealDelayPlusDuration_ProverbIsOne()
        {
            var b = new ColdOpenBeat();
            var (_, _, p, _) = b.Tick(ColdOpenBeat.RevealDelay + ColdOpenBeat.RevealDuration, false);
            Assert.AreEqual(1f, p, 0.001f, "Proverb must reach 1 after RevealDelay + RevealDuration");
        }

        [Test]
        public void Tick_ProverbAndPromptAlwaysEqual()
        {
            for (float t = 0f; t <= ColdOpenBeat.RevealDelay + ColdOpenBeat.RevealDuration + 0.5f; t += 0.1f)
            {
                var b = new ColdOpenBeat();
                var (_, _, p, prompt) = b.Tick(t, false);
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
                var (s, title, p, prompt) = b.Tick(t, true);
                Assert.AreEqual(s,     title,  0.001f, $"ReduceMotion: silhouette and title must match at t={t}");
                Assert.AreEqual(title, p,      0.001f, $"ReduceMotion: title and proverb must match at t={t}");
                Assert.AreEqual(p,     prompt, 0.001f, $"ReduceMotion: proverb and prompt must match at t={t}");
            }
        }

        [Test]
        public void ReduceMotion_AtFadeDuration_AllAlphasAreOne()
        {
            var b = new ColdOpenBeat();
            var (s, title, p, prompt) = b.Tick(ColdOpenBeat.ReduceMotionFadeDuration, true);
            Assert.AreEqual(1f, s,      0.001f, "ReduceMotion: silhouette must reach 1 at ReduceMotionFadeDuration");
            Assert.AreEqual(1f, title,  0.001f, "ReduceMotion: title must reach 1 at ReduceMotionFadeDuration");
            Assert.AreEqual(1f, p,      0.001f, "ReduceMotion: proverb must reach 1 at ReduceMotionFadeDuration");
            Assert.AreEqual(1f, prompt, 0.001f, "ReduceMotion: prompt must reach 1 at ReduceMotionFadeDuration");
        }

        [Test]
        public void ReduceMotion_AtZero_AllAlphasAreZero()
        {
            var b = new ColdOpenBeat();
            var (s, title, p, prompt) = b.Tick(0f, true);
            Assert.AreEqual(0f, s,      0.001f, "ReduceMotion: silhouette alpha is 0 at elapsed=0");
            Assert.AreEqual(0f, title,  0.001f, "ReduceMotion: title alpha is 0 at elapsed=0");
            Assert.AreEqual(0f, p,      0.001f, "ReduceMotion: proverb alpha is 0 at elapsed=0");
            Assert.AreEqual(0f, prompt, 0.001f, "ReduceMotion: prompt alpha is 0 at elapsed=0");
        }

        [Test]
        public void ReduceMotion_BeyondFadeDuration_AllAlphasClampAtOne()
        {
            var b = new ColdOpenBeat();
            var (s, title, p, _) = b.Tick(ColdOpenBeat.ReduceMotionFadeDuration * 3f, true);
            Assert.AreEqual(1f, s,     0.001f, "ReduceMotion: alpha must clamp at 1 beyond fade duration");
            Assert.AreEqual(1f, title, 0.001f, "ReduceMotion: title must clamp at 1 beyond fade duration");
            Assert.AreEqual(1f, p,     0.001f, "ReduceMotion: proverb must clamp at 1 beyond fade duration");
        }

        /// <summary>
        /// Verifies that the ReduceMotion branch produces a valid "collapsed" timeline:
        /// all four channels appear together in a single short fade (no staged sequence).
        /// A collapsed timeline is defined as: at exactly ReduceMotionFadeDuration,
        /// silhouette == title == proverb == prompt == 1.
        /// </summary>
        [Test]
        public void ReduceMotion_CollapsedTimeline_AllChannelsReadyAtFadeDuration()
        {
            var b = new ColdOpenBeat();
            var (s, title, p, prompt) = b.Tick(ColdOpenBeat.ReduceMotionFadeDuration, true);

            // All four channels must be fully on: the "collapsed" timeline is valid.
            Assert.AreEqual(1f, s,      0.001f, "collapsed timeline: silhouette");
            Assert.AreEqual(1f, title,  0.001f, "collapsed timeline: title");
            Assert.AreEqual(1f, p,      0.001f, "collapsed timeline: proverb");
            Assert.AreEqual(1f, prompt, 0.001f, "collapsed timeline: prompt");

            // Sanity: the normal timeline at the same elapsed would NOT yet show the proverb
            // (RevealDelay > ReduceMotionFadeDuration).
            Assert.Greater(ColdOpenBeat.RevealDelay, ColdOpenBeat.ReduceMotionFadeDuration,
                "ReduceMotionFadeDuration must be shorter than RevealDelay (collapsed beat premise)");
        }

        // ---- elapsed accumulates across calls ----

        [Test]
        public void Tick_AccumulatesElapsedAcrossCalls()
        {
            var b = new ColdOpenBeat();
            b.Tick(ColdOpenBeat.RevealDelay * 0.5f, false);
            var (_, _, p, _) = b.Tick(ColdOpenBeat.RevealDelay * 0.5f + ColdOpenBeat.RevealDuration, false);
            Assert.AreEqual(1f, p, 0.001f,
                "Elapsed must accumulate: two ticks summing to RevealDelay+RevealDuration must yield proverb=1");
        }

        // ---- silhouette stays fully lit after kindling complete ----

        [Test]
        public void Tick_AfterKindleDuration_SilhouetteRemainsOne()
        {
            var b = new ColdOpenBeat();
            var (s, _, _, _) = b.Tick(ColdOpenBeat.KindleDuration + 2f, false);
            Assert.AreEqual(1f, s, 0.001f, "Silhouette must stay at 1 past KindleDuration");
        }

        // ---- ColdOpenPrefs gate semantics (pure-host test) ----

        [Test]
        public void ColdOpenPrefs_HasSeen_DefaultsFalse()
        {
            // This test reads the live PlayerPrefs, which may already be set in a
            // real player environment. We verify the API contract only: resetting to
            // false then reading back must return false.
            ColdOpenPrefs.HasSeen = false;
            Assert.IsFalse(ColdOpenPrefs.HasSeen,
                "HasSeen must return false after explicitly setting false");
        }

        [Test]
        public void ColdOpenPrefs_HasSeen_RoundTrips()
        {
            ColdOpenPrefs.HasSeen = true;
            Assert.IsTrue(ColdOpenPrefs.HasSeen, "HasSeen must round-trip true");

            ColdOpenPrefs.HasSeen = false;
            Assert.IsFalse(ColdOpenPrefs.HasSeen, "HasSeen must round-trip false");
        }
    }
}
