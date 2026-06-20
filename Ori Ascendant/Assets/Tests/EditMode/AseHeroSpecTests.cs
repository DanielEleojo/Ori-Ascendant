using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Headless gate for the minimalist chrome discipline (issue #30, PRD W4).
    /// No scene, no MonoBehaviour — AseHeroSpec is pure constants and predicates.
    ///
    /// These tests pin the discipline values: if any constant drifts out of its
    /// design range, the chrome pass silently reverts to the heavy look it replaces.
    /// </summary>
    public class AseHeroSpecTests
    {
        // ---- Hero counter identification ----

        [Test]
        public void IsHeroCounter_LargestFont_ReturnsTrue() =>
            Assert.IsTrue(AseHeroSpec.IsHeroCounter(52f, 52f),
                "The element with the largest font size must qualify as the hero counter");

        [Test]
        public void IsHeroCounter_SmallerFont_ReturnsFalse() =>
            Assert.IsFalse(AseHeroSpec.IsHeroCounter(24f, 52f),
                "A font size below the canvas maximum must not qualify as hero");

        [Test]
        public void IsHeroCounter_ZeroMax_ReturnsFalse() =>
            Assert.IsFalse(AseHeroSpec.IsHeroCounter(0f, 0f),
                "When no text is active (max=0), IsHeroCounter must return false");

        [Test]
        public void IsHeroCounter_TiedForLargest_ReturnsTrue() =>
            Assert.IsTrue(AseHeroSpec.IsHeroCounter(48f, 48f),
                "An element tied for the maximum font size qualifies as hero");

        // ---- Hero glow alpha: faint, not heavy ----

        [Test]
        public void HeroGlowAlpha_IsAboveZero() =>
            Assert.Greater(AseHeroSpec.HeroGlowAlpha, 0f,
                "Hero glow must be visible — alpha must be above zero");

        [Test]
        public void HeroGlowAlpha_IsBelowHeavyThreshold() =>
            Assert.Less(AseHeroSpec.HeroGlowAlpha, 0.25f,
                "Hero glow must be faint — a value ≥ 0.25 reads as a visible disc, not a whisper");

        // ---- Chrome panel alpha: transparent ----

        [Test]
        public void ChromePanelAlpha_IsExactlyZero() =>
            Assert.AreEqual(0f, AseHeroSpec.ChromePanelAlpha, 0.001f,
                "Chrome panel backgrounds must be fully transparent — flat, never filled");

        // ---- Hairline border alpha: calmer than the hero glow ----

        [Test]
        public void HairlineBorderAlpha_IsAboveZero() =>
            Assert.Greater(AseHeroSpec.HairlineBorderAlpha, 0f,
                "Hairline borders must be visible — alpha must be above zero");

        [Test]
        public void HairlineBorderAlpha_IsLessThanHeroGlow() =>
            Assert.Less(AseHeroSpec.HairlineBorderAlpha, AseHeroSpec.HeroGlowAlpha,
                "Hairline borders must be calmer than the hero glow — hierarchy must hold");

        // ---- IsPanelHeavy predicate ----

        [Test]
        public void IsPanelHeavy_AboveHeroGlow_IsHeavy() =>
            Assert.IsTrue(AseHeroSpec.IsPanelHeavy(0.30f),
                "A panel fill above HeroGlowAlpha competes with the hero and must be flagged heavy");

        [Test]
        public void IsPanelHeavy_AtZero_IsNotHeavy() =>
            Assert.IsFalse(AseHeroSpec.IsPanelHeavy(0f),
                "A transparent panel is not heavy");

        [Test]
        public void IsPanelHeavy_ExactlyHeroGlow_IsNotHeavy() =>
            Assert.IsFalse(AseHeroSpec.IsPanelHeavy(AseHeroSpec.HeroGlowAlpha),
                "Exactly at HeroGlowAlpha the panel is at the boundary and not yet heavy");

        // ---- Chrome hierarchy: glow > hairline > panel ----

        [Test]
        public void ChromeHierarchy_GlowExceedsBorder() =>
            Assert.Greater(AseHeroSpec.HeroGlowAlpha, AseHeroSpec.HairlineBorderAlpha,
                "Hero glow must be more prominent than hairline borders");

        [Test]
        public void ChromeHierarchy_BorderExceedsPanel() =>
            Assert.Greater(AseHeroSpec.HairlineBorderAlpha, AseHeroSpec.ChromePanelAlpha,
                "Hairline borders must be more prominent than chrome panel fills");
    }
}
