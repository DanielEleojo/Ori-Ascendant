using NUnit.Framework;
using OriAscendant.Save;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Unit 4 — gating tests for the first-launch how-to-play overlay and
    /// the SeenFlags.HowToPlay constant. Mirror of ChannelHintDecisionTests in style.
    ///
    /// Pure decision tests: no scene, no MonoBehaviour.
    /// </summary>
    public class HowToPlayGateTests
    {
        // ---- SeenFlags: constant value + uniqueness ----

        [Test]
        public void HowToPlay_ConstantIs8()
        {
            Assert.AreEqual(8, SeenFlags.HowToPlay);
        }

        [Test]
        public void HowToPlay_IsPowerOfTwo()
        {
            int v = SeenFlags.HowToPlay;
            Assert.IsTrue(v > 0 && (v & (v - 1)) == 0,
                "SeenFlags.HowToPlay must be a power-of-two bit");
        }

        [Test]
        public void HowToPlay_DoesNotCollideWithChannelHint()
        {
            Assert.AreEqual(0, SeenFlags.HowToPlay & SeenFlags.ChannelHint,
                "HowToPlay and ChannelHint must not share bits");
        }

        [Test]
        public void HowToPlay_DoesNotCollideWithAscendCeremony()
        {
            Assert.AreEqual(0, SeenFlags.HowToPlay & SeenFlags.AscendCeremony,
                "HowToPlay and AscendCeremony must not share bits");
        }

        [Test]
        public void HowToPlay_DoesNotCollideWithFallCeremony()
        {
            Assert.AreEqual(0, SeenFlags.HowToPlay & SeenFlags.FallCeremony,
                "HowToPlay and FallCeremony must not share bits");
        }

        [Test]
        public void AllKnownSeenFlags_AreUniqueAndPowerOfTwo()
        {
            // Belt-and-suspenders: every known flag is power-of-two and they are pairwise distinct.
            int[] flags = { SeenFlags.ChannelHint, SeenFlags.AscendCeremony,
                            SeenFlags.FallCeremony, SeenFlags.HowToPlay };
            int combined = 0;
            foreach (int f in flags)
            {
                Assert.IsTrue(f > 0 && (f & (f - 1)) == 0,
                    $"{f} is not a power-of-two flag");
                Assert.AreEqual(0, combined & f,
                    $"Flag {f} collides with a previously checked flag");
                combined |= f;
            }
        }

        // ---- HowToPlayDecision: pure show/hide gate ----

        [Test]
        public void ShouldShow_WhenHowToPlayNotSeen()
        {
            // A fresh save has seenFlags = 0 — overlay must show.
            Assert.IsTrue(HowToPlayDecision.ShouldShow(seenFlags: 0));
        }

        [Test]
        public void ShouldHide_WhenHowToPlaySeen()
        {
            int seen = SeenFlags.HowToPlay;
            Assert.IsFalse(HowToPlayDecision.ShouldShow(seenFlags: seen));
        }

        [Test]
        public void ShouldHide_WhenMultipleFlagsSetIncludingHowToPlay()
        {
            // Other flags set too — as long as HowToPlay is in there, hide.
            int seen = SeenFlags.ChannelHint | SeenFlags.HowToPlay;
            Assert.IsFalse(HowToPlayDecision.ShouldShow(seenFlags: seen));
        }

        [Test]
        public void ShouldShow_WhenOtherFlagsSetButNotHowToPlay()
        {
            // Other things seen, but the player hasn't dismissed the overlay yet.
            int seen = SeenFlags.ChannelHint | SeenFlags.AscendCeremony;
            Assert.IsTrue(HowToPlayDecision.ShouldShow(seenFlags: seen));
        }

        [Test]
        public void ShouldHide_WhenAllFlagsSet()
        {
            int seen = SeenFlags.ChannelHint | SeenFlags.AscendCeremony
                     | SeenFlags.FallCeremony | SeenFlags.HowToPlay;
            Assert.IsFalse(HowToPlayDecision.ShouldShow(seenFlags: seen));
        }

        // ---- SaveData round-trip: MarkSeen + HasSeen ----

        [Test]
        public void MarkSeen_HowToPlay_PersistsViaHasSeen()
        {
            var save = new SaveData();
            Assert.IsFalse(save.HasSeen(SeenFlags.HowToPlay),
                "Fresh save must not have HowToPlay seen");

            save.MarkSeen(SeenFlags.HowToPlay);
            Assert.IsTrue(save.HasSeen(SeenFlags.HowToPlay),
                "After MarkSeen, HasSeen must return true");
        }

        [Test]
        public void MarkSeen_HowToPlay_DoesNotClobberChannelHint()
        {
            var save = new SaveData();
            save.MarkSeen(SeenFlags.ChannelHint);
            save.MarkSeen(SeenFlags.HowToPlay);
            Assert.IsTrue(save.HasSeen(SeenFlags.ChannelHint),
                "MarkSeen(HowToPlay) must not clobber ChannelHint bit");
        }

        // ---- ProceduralSprites.BuildGrain ----

        [Test]
        public void BuildGrain_ReturnsNonNullSprite()
        {
            // Constructing a texture is allowed in EditMode — no scene needed.
            var sprite = ProceduralSprites.BuildGrain(64);
            Assert.IsNotNull(sprite, "BuildGrain must return a non-null Sprite");
        }

        [Test]
        public void BuildGrain_ReturnsExpectedDimensions()
        {
            var sprite = ProceduralSprites.BuildGrain(32);
            Assert.IsNotNull(sprite);
            Assert.AreEqual(32, (int)sprite.rect.width);
            Assert.AreEqual(32, (int)sprite.rect.height);
        }
    }
}
