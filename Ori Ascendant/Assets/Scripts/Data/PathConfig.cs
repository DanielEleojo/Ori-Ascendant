using UnityEngine;

namespace OriAscendant.Data
{
    /// <summary>
    /// Presentation discriminator for Tribulation art/copy/SFX per path.
    /// NEVER touches resolution odds — the 60/40 coin is identical on all paths
    /// (TribulationConfig.baseAscendChance, locked).
    /// </summary>
    public enum TribulationType
    {
        Storm, // Sango
        Earth, // Ane
        River, // Osun
    }

    /// <summary>
    /// One cultivation Path (GAMEPLAY §2.3). Each path owns exactly ONE numeric
    /// hook on an orthogonal axis of the rate formula; defaults of 1.0 mean a
    /// misconfigured asset degrades to neutral, never broken. Config only.
    /// </summary>
    [CreateAssetMenu(fileName = "PathConfig", menuName = "Ori Ascendant/Path Config")]
    public class PathConfig : ScriptableObject
    {
        [Tooltip("Display name, e.g. \"Sango — Path of Thunder\".")]
        public string pathName;

        [TextArea]
        public string pathDescription;

        [Tooltip("Card stat line — concrete numbers, not adjectives (GAMEPLAY §2.3).")]
        public string identityLine;

        [Tooltip("Tradition of origin, labelled distinctly (cultural red line §7.7).")]
        public string traditionLabel;

        [Tooltip("HUD hook badge, e.g. \"ACTIVE ×2\".")]
        public string hookBadge;

        [Tooltip("Welcome-Back itemized line label (Ane: \"Earth's Patience\"); empty = no line.")]
        public string offlineBonusLabel;

        [Tooltip("ONLINE path multiplier in the rate formula (Sango 2.0; others 1.0).")]
        public double aseGenerationModifier = 1.0;

        [Tooltip("Read ONLY by OfflineProgressCalculator. Multiplies the offline RATE, " +
                 "never the 8h time cap (Ane 1.5; Sango 0.5 = net offline ×1.0).")]
        public double offlineRateModifier = 1.0;

        [Tooltip("Wraps the WHOLE lineage term — permanent + active together — which " +
                 "keeps council retirement Àṣẹ-neutral (Osun 2.0; others 1.0).")]
        public double councilBonusModifier = 1.0;

        public TribulationType tribulationType;

        [Tooltip("Path BGM theme (Phase D).")]
        public AudioClip musicTheme;
    }
}
