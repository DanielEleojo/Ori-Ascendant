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
        [Tooltip("Flat ascend probability — LOCKED at 0.60, identical on every path, " +
                 "every generation. Disclosed to the player in the odds panel.")]
        public double baseAscendChance = 0.60;

        [Tooltip("Àṣẹ required at the final stage to face the Tribulation " +
                 "(mantissa × 10^exponent). 25M per the verified balance table.")]
        public double aseThresholdMantissa = 25.0;
        public int aseThresholdExponent = 6;

        [Tooltip("Threshold fractions for ambient escalation beats (sky tint / storm vignette / eligible).")]
        public float[] ambientFractions = { 0.5f, 0.8f, 1.0f };

        [Tooltip("Hold-to-begin ring duration on the confirm sheet (seconds).")]
        public double holdToConfirmSeconds = 0.8;

        [Header("Ceremony beats (GAMEPLAY §3.5 timing table)")]
        public float transitionSeconds = 2.0f;
        public int stormWaveCount = 3;
        public float stormWaveIntervalSeconds = 1.0f;
        public float silenceHoldSeconds = 1.5f;
        public float revealSeconds = 2.5f;
        public float ancestorCardSeconds = 2.5f;
        public float finalBeatSeconds = 2.0f;

        public BigNumber GetAseThreshold() => new BigNumber(aseThresholdMantissa, aseThresholdExponent);
    }
}
