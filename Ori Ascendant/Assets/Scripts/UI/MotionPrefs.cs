using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace OriAscendant.UI
{
    /// <summary>
    /// Reduce Motion on/off preference (issue #31 / ADR-0004 / issue #5).
    ///
    /// Two sources OR'd together so neither clobbers the other:
    ///   • In-app toggle  ("ReduceMotion"  key) — set by the player in Settings.
    ///   • OS-level flag  ("ReduceMotionOS" key) — written by SyncOsFlag(), which
    ///     reads UIAccessibility.isReduceMotionEnabled via OriAccessibility.mm on iOS.
    ///
    /// ReduceMotionEnabled getter = InApp || OsFlag.
    /// ReduceMotionEnabled setter writes only the in-app key — OS flag stays separate.
    /// Native side only READS the OS flag; C# owns all PlayerPrefs writes.
    /// Validated on device via Cloud Build → TestFlight (untestable on Linux). ADR-0004.
    /// </summary>
    public static class MotionPrefs
    {
        private const string ReduceMotionKey   = "ReduceMotion";
        private const string OsReduceMotionKey = "ReduceMotionOS";

        /// <summary>True when Reduce Motion is active from either source.
        /// Setter writes only the in-app key — use this from Settings UI.</summary>
        public static bool ReduceMotionEnabled
        {
            get => PlayerPrefs.GetInt(ReduceMotionKey, 0) != 0 ||
                   PlayerPrefs.GetInt(OsReduceMotionKey, 0) != 0;
            set => PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0);
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool OriAccessibility_IsReduceMotionEnabled();

        /// <summary>Read the OS Reduce-Motion flag and cache it in PlayerPrefs.
        /// Call on startup and OnApplicationFocus (true) to stay in sync.</summary>
        public static void SyncOsFlag()
        {
            bool flag = OriAccessibility_IsReduceMotionEnabled();
            PlayerPrefs.SetInt(OsReduceMotionKey, flag ? 1 : 0);
        }
#else
        public static void SyncOsFlag() { }
#endif
    }
}
