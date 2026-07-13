using UnityEngine;

namespace OriAscendant.Save
{
    /// <summary>
    /// Local push notification preferences. Stored in PlayerPrefs, NOT SaveData —
    /// these are device settings, not save-state (mirrors HapticPrefs, issue #31).
    /// </summary>
    public static class NotificationPrefs
    {
        private const string EnabledKey = "ori_notifications_enabled";
        private const string PermissionRequestedKey = "ori_notification_permission_requested";

        /// <summary>Player-facing toggle (Settings screen). Defaults on.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) != 0;
            set => PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
        }

        /// <summary>Whether the iOS authorization prompt has already been shown once,
        /// lifetime. Not the same as <see cref="Enabled"/> — this never resets, so the
        /// OS permission dialog is only ever requested a single time. Defaults off.</summary>
        public static bool PermissionRequested
        {
            get => PlayerPrefs.GetInt(PermissionRequestedKey, 0) != 0;
            set => PlayerPrefs.SetInt(PermissionRequestedKey, value ? 1 : 0);
        }
    }
}
