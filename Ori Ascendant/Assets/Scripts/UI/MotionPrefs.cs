using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Reduce Motion on/off preference (issue #31 / ADR-0004).
    /// Key "ReduceMotion" is shared with the future iOS native bridge (ADR-0004)
    /// which will write it from UIAccessibility.isReduceMotionEnabled.
    /// Defaults off. Stored in PlayerPrefs, NOT SaveData.
    /// </summary>
    public static class MotionPrefs
    {
        private const string ReduceMotionKey = "ReduceMotion";

        public static bool ReduceMotionEnabled
        {
            get => PlayerPrefs.GetInt(ReduceMotionKey, 0) != 0;
            set => PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0);
        }
    }
}
