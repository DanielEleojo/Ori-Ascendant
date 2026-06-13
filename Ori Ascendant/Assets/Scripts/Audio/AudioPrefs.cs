using UnityEngine;

namespace OriAscendant.Audio
{
    /// <summary>
    /// BGM/SFX on-off preferences. Stored in PlayerPrefs, NOT SaveData — audio
    /// settings are a device preference, and keeping them out of the save avoids
    /// a schema-version bump (CLAUDE.md / GAMEPLAY §6). Defaults on.
    /// </summary>
    public static class AudioPrefs
    {
        private const string BgmKey = "ori_bgm_enabled";
        private const string SfxKey = "ori_sfx_enabled";

        public static bool BgmEnabled
        {
            get => PlayerPrefs.GetInt(BgmKey, 1) != 0;
            set => PlayerPrefs.SetInt(BgmKey, value ? 1 : 0);
        }

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) != 0;
            set => PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
        }
    }
}
