using OriAscendant.Core;
using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One cultivation stage (GAMEPLAY §2.2). Config only — never written at
    /// runtime. The assets in /Resources/StageConfigs/ carry playtest-locked
    /// balance values: changes there require playtesting, not just code review
    /// (CLAUDE.md off-limits rule).
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfig", menuName = "Ori Ascendant/Cultivation Stage Config")]
    public class CultivationStageConfig : ScriptableObject
    {
        [Tooltip("Display name, with full Yoruba diacritics (e.g. \"Aláàṣẹ\").")]
        public string stageName;

        [TextArea]
        public string stageDescription;

        [Tooltip("Cumulative Àṣẹ required to advance OUT of this stage into the next " +
                 "(mantissa × 10^exponent). Àṣẹ is never spent on advancement. " +
                 "Unused on the final stage — TribulationConfig gates it instead.")]
        public double aseThresholdMantissa;
        public int aseThresholdExponent;

        [Tooltip("Stage bonus to Àṣẹ/s (multiplies baseRate).")]
        public double productionMultiplier = 1.0;

        [Tooltip("Static per-stage portrait (art lands Phase D; placeholder until then).")]
        public Sprite portrait;

        [Tooltip("0 = Ayé (stages 1–3), 1 = Ọ̀run (stages 4–6).")]
        public int tier;

        public BigNumber GetAdvanceThreshold() => new BigNumber(aseThresholdMantissa, aseThresholdExponent);
    }
}
