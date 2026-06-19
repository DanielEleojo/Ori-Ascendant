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
    }
}
