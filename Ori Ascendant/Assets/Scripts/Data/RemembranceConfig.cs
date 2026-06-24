using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// Curated content for remembrance derivation at the Crossing (slice 4a).
    /// personalNames seeds the Title pool for ascended cultivators; faithfulFallLine
    /// is the dignified shared line for a fallen cultivator who never strayed from
    /// their Ori. Pre-§7.10 placeholder — final copy lands at Phase 5 / issue #10.
    /// Config only — never written at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "RemembranceConfig", menuName = "Ori Ascendant/Remembrance Config")]
    public class RemembranceConfig : ScriptableObject
    {
        [Tooltip("Pool of personal names appended after the honorific for an ascended Title. " +
                 "Index is drawn at random at the Crossing; must be non-empty.")]
        public string[] personalNames;

        [Tooltip("Shared Nickname line for a fallen cultivator who never strayed from their Ori vow.")]
        [TextArea]
        public string faithfulFallLine;
    }
}
