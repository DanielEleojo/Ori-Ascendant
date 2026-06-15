using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// The curated words a Crossing draws on to remember a cultivator (CONTEXT.md:
    /// Title / Nickname). An ascended Title pairs the peak-stage honorific with a
    /// <see cref="personalNames"/> entry; a faithful fall — a life that never strayed
    /// yet still fell — shares the single <see cref="faithfulFallLine"/>. The
    /// per-Crossroads fallen epithet lives on the deck beat, not here, so one §7.10
    /// review covers all Crossroads content. Seed content is PLACEHOLDER, pre-§7.10
    /// native-speaker review (slice #10).
    /// </summary>
    [CreateAssetMenu(fileName = "RemembranceConfig", menuName = "Ori Ascendant/Remembrance Config")]
    public class RemembranceConfig : ScriptableObject
    {
        [Tooltip("Curated personal-name pool an ascended Title draws from (e.g. \"Adé\" → " +
                 "\"Aṣẹ́gun Adé\"). Drawn by chance at the Crossing. PLACEHOLDER pre-§7.10.")]
        public string[] personalNames;

        [Tooltip("The one dignified Nickname for a life that held its vow at every Crossroads " +
                 "yet still fell — no Defining Deed to name. PLACEHOLDER pre-§7.10.")]
        [TextArea]
        public string faithfulFallLine;
    }
}
