using UnityEngine;

namespace OriAscendant.Audio
{
    /// <summary>
    /// Haptics on/off preference. Stored in PlayerPrefs, NOT SaveData — this is
    /// a device setting, not save-state (issue #31). Defaults on.
    /// </summary>
    public static class HapticPrefs
    {
        private const string HapticsKey = "ori_haptics_enabled";

        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) != 0;
            set => PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0);
        }
    }
}
