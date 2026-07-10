using NUnit.Framework;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate Wave 3 / Track A: pure-math contracts for TribulationAtmosphere
    /// (ART_BIBLE §5.5 "50% sky tint, 80% storm vignette, majestic awe not menace").
    /// </summary>
    public class TribulationAtmosphereTests
    {
        // ---- VignetteAlpha ----

        [Test]
        public void VignetteAlpha_Zero_BelowFirstTrigger()
        {
            Assert.AreEqual(0f, TribulationAtmosphere.VignetteAlpha(0.0), 0.001f);
            Assert.AreEqual(0f, TribulationAtmosphere.VignetteAlpha(0.49), 0.001f);
        }

        [Test]
        public void VignetteAlpha_Low_AtFirstTrigger()
        {
            float a = TribulationAtmosphere.VignetteAlpha(TribulationAtmosphere.FractionSkyTint);
            Assert.Greater(a, 0f, "Must have nonzero vignette at the first trigger");
            Assert.Less(a, 0.30f, "Must stay below the second-trigger level");
        }

        [Test]
        public void VignetteAlpha_Higher_AtSecondTrigger()
        {
            float a50 = TribulationAtmosphere.VignetteAlpha(TribulationAtmosphere.FractionSkyTint);
            float a80 = TribulationAtmosphere.VignetteAlpha(TribulationAtmosphere.FractionStormVignette);
            Assert.Greater(a80, a50, "Second trigger must be darker than first");
        }

        [Test]
        public void VignetteAlpha_DoesNotGrow_PastSecondTrigger()
        {
            float a80 = TribulationAtmosphere.VignetteAlpha(TribulationAtmosphere.FractionStormVignette);
            float a100 = TribulationAtmosphere.VignetteAlpha(1.0);
            Assert.AreEqual(a80, a100, 0.001f, "No further darkening past the second trigger");
        }

        // ---- SkyOverlayColor ----

        [Test]
        public void SkyOverlayColor_Transparent_BelowFirstTrigger()
        {
            Assert.AreEqual(0f, TribulationAtmosphere.SkyOverlayColor(0.0).a, 0.001f);
            Assert.AreEqual(0f, TribulationAtmosphere.SkyOverlayColor(0.499).a, 0.001f);
        }

        [Test]
        public void SkyOverlayColor_HasAlpha_AboveFirstTrigger()
        {
            Assert.Greater(TribulationAtmosphere.SkyOverlayColor(0.55).a, 0f);
        }

        [Test]
        public void SkyOverlayColor_DeeperAlpha_TowardSecondTrigger()
        {
            float a55 = TribulationAtmosphere.SkyOverlayColor(0.55).a;
            float a80 = TribulationAtmosphere.SkyOverlayColor(0.80).a;
            Assert.Greater(a80, a55, "Tint must deepen as fraction approaches 80%");
        }

        [Test]
        public void SkyOverlayColor_IsWarm_NotNeutralGrey()
        {
            // ART_BIBLE §5.5 — storm is "majestic awe not menace": warm red channel,
            // NOT a neutral grey darkening.
            Color c = TribulationAtmosphere.SkyOverlayColor(0.9);
            Assert.Greater(c.r, c.b + 0.05f,
                "Storm tint must be warm (red > blue) — never a cool or neutral grey");
        }

        // ---- TensionLevel (BGM storm pressure) ----

        [Test]
        public void TensionLevel_Zero_BelowFirstTrigger()
        {
            Assert.AreEqual(0f, TribulationAtmosphere.TensionLevel(0.0), 0.001f);
            Assert.AreEqual(0f, TribulationAtmosphere.TensionLevel(0.49), 0.001f);
        }

        [Test]
        public void TensionLevel_Ramps_BetweenTriggers()
        {
            float t55 = TribulationAtmosphere.TensionLevel(0.55);
            float t70 = TribulationAtmosphere.TensionLevel(0.70);
            Assert.Greater(t55, 0f, "Tension must engage past the sky-tint fraction");
            Assert.Greater(t70, t55, "Tension must escalate as the fraction rises");
            Assert.Less(t70, 1f, "Full tension is reserved for the vignette fraction");
        }

        [Test]
        public void TensionLevel_Full_AtSecondTrigger_AndClampedPast()
        {
            Assert.AreEqual(1f,
                TribulationAtmosphere.TensionLevel(TribulationAtmosphere.FractionStormVignette), 0.001f);
            Assert.AreEqual(1f, TribulationAtmosphere.TensionLevel(1.5), 0.001f,
                "Clamped — never overshoots past the vignette fraction");
        }

        // ---- Cohesion (Unit 6) ----

        [Test]
        public void SkyOverlayColor_Hue_MatchesPaletteStormTint()
        {
            // Palette.StormTint is the single source of truth for the storm hue (Unit 1).
            // TribulationAtmosphere.SkyOverlayColor must use that hue — not a private literal.
            Color sky = TribulationAtmosphere.SkyOverlayColor(1.0); // full fraction → peak alpha
            Color tint = Palette.StormTint;
            // Only the RGB channels define the hue; alpha is driven independently by the lerp.
            Assert.AreEqual(tint.r, sky.r, 0.001f, "red channel must match Palette.StormTint");
            Assert.AreEqual(tint.g, sky.g, 0.001f, "green channel must match Palette.StormTint");
            Assert.AreEqual(tint.b, sky.b, 0.001f, "blue channel must match Palette.StormTint");
        }

        [Test]
        public void SkyOverlayColor_Alpha_IsIndependentOfPaletteStormTintAlpha()
        {
            // The alpha is driven by the lerp, not by Palette.StormTint.a (which is 1.0).
            // At full fraction the alpha must be 0.28 (the peak atmosphere value), never 1.0.
            Color sky = TribulationAtmosphere.SkyOverlayColor(1.0);
            Assert.Less(sky.a, 0.30f,
                "Sky overlay alpha at full fraction must remain below 0.30 — not the palette opaque value");
            Assert.Greater(sky.a, 0f, "Sky overlay must be visible at full fraction");
        }
    }
}
