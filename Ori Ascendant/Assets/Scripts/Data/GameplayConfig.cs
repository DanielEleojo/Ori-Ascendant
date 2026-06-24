using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// Global gameplay tuning values (GAMEPLAY §2.5). ScriptableObjects are
    /// config only — never written at runtime. All "magic numbers" for the core
    /// loop live here per the no-magic-numbers convention.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Ori Ascendant/Gameplay Config")]
    public class GameplayConfig : ScriptableObject
    {
        [Tooltip("Àṣẹ per second at Stage 1 with no modifiers.")]
        public double baseRate = 1.0;

        [Tooltip("Tap-to-channel grant: seconds of current production per tap.")]
        public double tapChannelSeconds = 5.0;

        [Tooltip("Minimum offline seconds before the Welcome Back modal shows; below this the gain is credited silently.")]
        public int welcomeBackMinSeconds = 60;

        [Tooltip("Seconds between background autosaves.")]
        public int autosaveIntervalSeconds = 30;

        [Tooltip("Cap on the Renown rate bonus — the 7th additive term in the §2.1 rate. PLACEHOLDER pending the balance pass (issue #40).")]
        public double renownBonusCap = 0.25;
    }
}
