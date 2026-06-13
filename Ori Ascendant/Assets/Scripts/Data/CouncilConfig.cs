using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// Ancestral Council constants (GAMEPLAY §2.5). Authored in Phase B
    /// (config-first), consumed by AncestralCouncilSystem in Phase C. Config only.
    /// </summary>
    [CreateAssetMenu(fileName = "CouncilConfig", menuName = "Ori Ascendant/Council Config")]
    public class CouncilConfig : ScriptableObject
    {
        [Tooltip("W — per-ancestor additive weight. Each active ancestor contributes " +
                 "W × bonusMultiplier inside the lineage term (ascended +25%, fallen +10%).")]
        public double ancestorBaseBonus = 0.25;

        [Tooltip("Maximum active council size; a 6th ancestor retires the oldest " +
                 "Àṣẹ-neutrally into lineage.permanentAseBonus.")]
        public int maxCouncil = 5;
    }
}
