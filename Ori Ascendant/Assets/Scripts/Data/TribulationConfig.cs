using OriAscendant.Core;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// Tribulation constants (GAMEPLAY §2.5, §3.5). Authored in Phase B
    /// (config-first), consumed by TribulationSystem in Phase C. Config only.
    /// </summary>
    [CreateAssetMenu(fileName = "TribulationConfig", menuName = "Ori Ascendant/Tribulation Config")]
    public class TribulationConfig : ScriptableObject
    {
        [Tooltip("Documented midpoint anchor (ADR-0004): the steadfastness curve's " +
                 "floor/ceiling bracket this. Retained from the old flat 0.60; NOT a " +
                 "direct input to the linear curve.")]
        public double baseAscendChance = 0.60;

        [Tooltip("Ascend chance for a fully-wavering life (steadfastness 0) and the " +
                 "trials==0 default — ADR-0004 floor. Balance-sim tuned (#12).")]
        public double ascendFloor = 0.25;

        [Tooltip("Ascend chance for a fully-steadfast life (steadfastness 1) — ADR-0004 " +
                 "ceiling; the steadfast are rewarded but never certain. Balance-sim tuned (#12).")]
        public double ascendCeiling = 0.90;

        [Tooltip("Àṣẹ required at the final stage to face the Tribulation " +
                 "(mantissa × 10^exponent). 25M per the verified balance table.")]
        public double aseThresholdMantissa = 25.0;
        public int aseThresholdExponent = 6;

        [Tooltip("Threshold fractions for ambient escalation beats (sky tint / storm vignette / eligible).")]
        public float[] ambientFractions = { 0.5f, 0.8f, 1.0f };

        [Tooltip("Hold-to-begin ring duration on the confirm sheet (seconds).")]
        public double holdToConfirmSeconds = 0.8;

        [Header("Line-legacy compounding (issue #8)")]
        [Tooltip("Bonus added to AscendChance per consecutive generation that held the same Ori as the current life. Bounded by lineLegacyMaxBonus.")]
        public double lineLegacyBonusPerGen = 0.05;

        [Tooltip("Maximum total line-legacy bonus (caps lineLegacyBonusPerGen × consecutiveCount). Keeps compounding bounded (ADR-0005).")]
        public double lineLegacyMaxBonus = 0.15;

        [Header("Ceremony beats (GAMEPLAY §3.5 timing table)")]
        public float transitionSeconds = 2.0f;
        public int stormWaveCount = 3;
        public float stormWaveIntervalSeconds = 1.0f;
        public float silenceHoldSeconds = 1.5f;
        public float revealSeconds = 2.5f;
        public float ancestorCardSeconds = 2.5f;
        public float finalBeatSeconds = 2.0f;

        [Header("Crowned Ascended reveal (Phase 6 — appearance-0, funded + §7.10-gated)")]
        [Tooltip("Bespoke crowned portrait for the ascension reveal beat (appearance-0). " +
                 "Null = gold-FX fallback (slice 6). Art ships when funded and §7.10-cleared. " +
                 "Per-appearance crowns ride with the Appearance pool (ADR-0001).")]
        public Sprite crownedAscendedRevealPortrait;

        public BigNumber GetAseThreshold() => new BigNumber(aseThresholdMantissa, aseThresholdExponent);

        /// <summary>
        /// Returns the portrait sprite to show during the reveal beat.
        /// Uses the bespoke crowned portrait (appearance-0) when it is set and the
        /// outcome is ascend; falls back to the humble Stage-6 Victor portrait otherwise
        /// (the committed gold-FX fallback path).
        /// </summary>
        public Sprite RevealSprite(bool ascended, Sprite victoryPortrait) =>
            ascended && crownedAscendedRevealPortrait != null
                ? crownedAscendedRevealPortrait
                : victoryPortrait;
    }
}
