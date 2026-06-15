using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// One Ori — a virtue vowed at the start of a life (Àkùnlẹ̀yàn). Steadfastness
    /// to the chosen Ori is what the Crossing weighs (ADR-0004); the Ori itself is
    /// config only. The index into the Ori set IS its identity (SaveData.currentOri),
    /// mirroring how a path is an index into the Path set. Seed virtues are
    /// placeholder pending the §7.10 review (slice #10). See docs/DYNASTY_REDESIGN.md,
    /// CONTEXT.md (Ori).
    /// </summary>
    [CreateAssetMenu(fileName = "OriConfig", menuName = "Ori Ascendant/Ori Config")]
    public class OriConfig : ScriptableObject
    {
        [Tooltip("Display name of the virtue-vow, e.g. \"The Path of Mercy\".")]
        public string oriName;

        [TextArea]
        public string oriDescription;
    }
}
