using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Seen/skip gate for the cold-open launch beat (issue #32).
    /// Stored in PlayerPrefs, NOT SaveData — no migration version bump needed.
    /// Key "ColdOpenSeen" matches the ADR-0004 pattern for PlayerPrefs preferences.
    /// </summary>
    public static class ColdOpenPrefs
    {
        private const string SeenKey = "ColdOpenSeen";

        /// <summary>True if the player has already seen (or dismissed) the cold open.</summary>
        public static bool HasSeen
        {
            get => PlayerPrefs.GetInt(SeenKey, 0) != 0;
            set => PlayerPrefs.SetInt(SeenKey, value ? 1 : 0);
        }
    }
}
