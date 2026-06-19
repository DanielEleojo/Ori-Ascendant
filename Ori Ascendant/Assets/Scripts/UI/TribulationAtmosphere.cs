using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure math for the tribulation buildup atmosphere (ART_BIBLE §5.5,
    /// GAMEPLAY §3.5). All fns accept a pre-computed fraction (0..1) and
    /// return presentational values — no MonoBehaviour, no service access,
    /// headlessly testable on Linux. Canonical trigger points must match
    /// TribulationConfig.ambientFractions = { 0.50f, 0.80f, 1.00f }.
    /// </summary>
    public static class TribulationAtmosphere
    {
        // Visual trigger fractions (ART_BIBLE §5.5 — "sky tint" / "storm vignette").
        public const float FractionSkyTint = 0.50f;
        public const float FractionStormVignette = 0.80f;

        // Warm storm-amber shadow: a rich amber-dark overtone, never a neutral grey
        // (ART_BIBLE §5.5 "majestic awe, not menace" — warm red dominates).
        private static readonly Color s_stormTint = new Color(0.28f, 0.12f, 0.04f);

        /// <summary>Alpha for the fullscreen storm-sky overlay. Step function at the
        /// two canonical fractions; held constant past the second trigger so the sky
        /// reaches max darkness before the CTA arms (both happen at or before 80%).</summary>
        public static float VignetteAlpha(double fraction)
        {
            if (fraction < FractionSkyTint)       return 0f;
            if (fraction >= FractionStormVignette) return 0.35f;
            return 0.15f;
        }

        /// <summary>Fullscreen sky overlay colour: transparent below 50%, lerps from
        /// a faint warm shadow (0.10 alpha at 50%) to a deeper storm-amber (0.28 at
        /// 80%+). Both the hue and the alpha are warm — reds exceed blues throughout
        /// to honour the "majestic awe" brief.</summary>
        public static Color SkyOverlayColor(double fraction)
        {
            if (fraction < FractionSkyTint) return new Color(0, 0, 0, 0);

            float t = (float)System.Math.Min(
                1.0,
                (fraction - FractionSkyTint) / (FractionStormVignette - FractionSkyTint));

            float alpha = Mathf.Lerp(0.10f, 0.28f, t);
            return new Color(s_stormTint.r, s_stormTint.g, s_stormTint.b, alpha);
        }
    }
}
