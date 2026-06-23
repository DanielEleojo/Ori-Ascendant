using NUnit.Framework;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Regression gate for the design-token layer (ADR-0007).
    /// Verifies ordering invariants, positivity, and cross-class coherence so
    /// that a future merge that silently reorders or zeroes a constant fails CI
    /// before it reaches a visual QA pass.
    /// </summary>
    public class DesignSystemTests
    {
        // ---- TypographicScale ----

        [Test]
        public void TypographicScale_AllPositive()
        {
            Assert.Greater(TypographicScale.Hero,    0f, nameof(TypographicScale.Hero));
            Assert.Greater(TypographicScale.Display, 0f, nameof(TypographicScale.Display));
            Assert.Greater(TypographicScale.H1,      0f, nameof(TypographicScale.H1));
            Assert.Greater(TypographicScale.H2,      0f, nameof(TypographicScale.H2));
            Assert.Greater(TypographicScale.Body,    0f, nameof(TypographicScale.Body));
            Assert.Greater(TypographicScale.BodySm,  0f, nameof(TypographicScale.BodySm));
            Assert.Greater(TypographicScale.Label,   0f, nameof(TypographicScale.Label));
            Assert.Greater(TypographicScale.Caption, 0f, nameof(TypographicScale.Caption));
        }

        [Test]
        public void TypographicScale_StrictlyDescending()
        {
            Assert.Greater(TypographicScale.Hero,    TypographicScale.Display, "Hero > Display");
            Assert.Greater(TypographicScale.Display, TypographicScale.H1,      "Display > H1");
            Assert.Greater(TypographicScale.H1,      TypographicScale.H2,      "H1 > H2");
            Assert.Greater(TypographicScale.H2,      TypographicScale.Body,    "H2 > Body");
            Assert.Greater(TypographicScale.Body,    TypographicScale.BodySm,  "Body > BodySm");
            Assert.Greater(TypographicScale.BodySm,  TypographicScale.Label,   "BodySm > Label");
            Assert.Greater(TypographicScale.Label,   TypographicScale.Caption, "Label > Caption");
        }

        // ---- SpacingScale ----

        [Test]
        public void SpacingScale_AllPositive()
        {
            Assert.Greater(SpacingScale.Xxs, 0f, nameof(SpacingScale.Xxs));
            Assert.Greater(SpacingScale.Xs,  0f, nameof(SpacingScale.Xs));
            Assert.Greater(SpacingScale.Sm,  0f, nameof(SpacingScale.Sm));
            Assert.Greater(SpacingScale.Md,  0f, nameof(SpacingScale.Md));
            Assert.Greater(SpacingScale.Lg,  0f, nameof(SpacingScale.Lg));
            Assert.Greater(SpacingScale.Xl,  0f, nameof(SpacingScale.Xl));
            Assert.Greater(SpacingScale.Xxl, 0f, nameof(SpacingScale.Xxl));
        }

        [Test]
        public void SpacingScale_BaseIs4px()
        {
            Assert.AreEqual(4f, SpacingScale.Xxs, "Xxs must equal the 4px base unit");
        }

        [Test]
        public void SpacingScale_StrictlyAscending()
        {
            Assert.Less(SpacingScale.Xxs, SpacingScale.Xs,  "Xxs < Xs");
            Assert.Less(SpacingScale.Xs,  SpacingScale.Sm,  "Xs < Sm");
            Assert.Less(SpacingScale.Sm,  SpacingScale.Md,  "Sm < Md");
            Assert.Less(SpacingScale.Md,  SpacingScale.Lg,  "Md < Lg");
            Assert.Less(SpacingScale.Lg,  SpacingScale.Xl,  "Lg < Xl");
            Assert.Less(SpacingScale.Xl,  SpacingScale.Xxl, "Xl < Xxl");
        }

        // ---- OpacitySpec ----

        [Test]
        public void OpacitySpec_AllInZeroOneRange()
        {
            AssertInUnitRange(OpacitySpec.Scrim,             nameof(OpacitySpec.Scrim));
            AssertInUnitRange(OpacitySpec.PathAccent,        nameof(OpacitySpec.PathAccent));
            AssertInUnitRange(OpacitySpec.HairlineStrong,    nameof(OpacitySpec.HairlineStrong));
            AssertInUnitRange(OpacitySpec.DeepField,         nameof(OpacitySpec.DeepField));
            AssertInUnitRange(OpacitySpec.WaterlinePulseMin, nameof(OpacitySpec.WaterlinePulseMin));
            AssertInUnitRange(OpacitySpec.WaterlinePulseMax, nameof(OpacitySpec.WaterlinePulseMax));
        }

        [Test]
        public void OpacitySpec_WaterlinePulseMinLessThanMax()
        {
            Assert.Less(OpacitySpec.WaterlinePulseMin, OpacitySpec.WaterlinePulseMax,
                "WaterlinePulseMin must be strictly less than WaterlinePulseMax");
        }

        // ---- CardViewSpec ----

        [Test]
        public void CardViewSpec_IdleAndSelectedColoursDiffer()
        {
            Assert.AreNotEqual(CardViewSpec.Idle, CardViewSpec.Selected,
                "Idle and Selected panel colours must differ");
        }

        [Test]
        public void CardViewSpec_SelectedScaleAboveOne()
        {
            Assert.Greater(CardViewSpec.SelectedScale, 1f,
                "SelectedScale must be > 1 to visually lift the selected card");
        }

        // ---- Palette.StormTint ----

        [Test]
        public void Palette_StormTint_ApproximatesExpectedRgb()
        {
            // 0x471F0A ≈ (0.28, 0.12, 0.04) — the warm-amber literal previously in TribulationAtmosphere.
            const float tolerance = 0.01f;
            Color c = Palette.StormTint;
            Assert.AreEqual(0.28f, c.r, tolerance, "StormTint.r");
            Assert.AreEqual(0.12f, c.g, tolerance, "StormTint.g");
            Assert.AreEqual(0.04f, c.b, tolerance, "StormTint.b");
        }

        // ---- Helpers ----

        static void AssertInUnitRange(float value, string name)
        {
            Assert.Greater(value, 0f,  $"{name} must be > 0");
            Assert.LessOrEqual(value, 1f, $"{name} must be <= 1");
        }
    }
}
