// APPLE_GAMEKIT define gate: the Game Center + iCloud entitlements below are only
// valid once the Apple.Core / Apple.GameKit packages are wired in AND the matching
// App ID (with those capabilities + a real iCloud container) exists in the Apple
// Developer console. Until then this hook is excluded so device builds sign cleanly
// against a plain Apple Development profile (cloud save falls back to local — see
// CloudSaveManager / GameKitCloudSaveProvider, gated by the same symbol).
// NOTE for Phase D: ICloudContainer derives from BuildConfigurator.BundleId, which is
// still the placeholder "com.oriascendant.game" — fix it to the real App ID first.
#if UNITY_IOS && APPLE_GAMEKIT
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// Wires the generated Xcode project's iOS capabilities at build time. The repo had
    /// no such hook, so Game Center + iCloud had to be re-added by hand after every Cloud
    /// Build. This adds them deterministically:
    ///   • Game Center — GKLocalPlayer auth + GKSavedGame (CloudSaveManager / GameKitCloudSaveProvider).
    ///   • iCloud (Documents / ubiquity container) — the store backing GKSavedGame cloud saves.
    ///
    /// The iCloud container id MUST match the App ID set up in the Apple Developer console
    /// (the same bundle id with Game Center + iCloud Documents enabled).
    ///
    /// IMPORTANT — this is the ONE file in the project that cannot be verified on the Linux
    /// dev box: `UnityEditor.iOS.Xcode` only exists with iOS Build Support, and there is no
    /// local iOS compile. The whole file is `#if UNITY_IOS`, so it is excluded from (and
    /// never affects) the headless EditMode/Standalone gate. VALIDATE IT ON THE FIRST CLOUD
    /// BUILD: if `ProjectCapabilityManager.AddiCloud`'s overload differs in this Unity
    /// version, adjust the positional call below. If `iOSAutomaticallyDetectAndAddCapabilities`
    /// (Player Settings) double-adds Game Center, turn that toggle off and let this be the
    /// single source of truth. See docs/RELEASE_CHECKLIST.md.
    /// </summary>
    public static class IosBuildPostProcessor
    {
        // Ubiquity container backing Game Center saved games. Convention: iCloud.<bundleId>.
        private const string ICloudContainer = "iCloud." + BuildConfigurator.BundleId;

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbx = new PBXProject();
            pbx.ReadFromFile(pbxPath);
            string mainTargetGuid = pbx.GetUnityMainTargetGuid();

            var caps = new ProjectCapabilityManager(
                pbxPath,
                "OriAscendant.entitlements",
                targetName: null,
                targetGuid: mainTargetGuid);

            caps.AddGameCenter();

            // AddiCloud(keyValueStorage, iCloudDocuments, cloudKit, pushNotifications, customContainers).
            // GKSavedGame uses the iCloud Documents (ubiquity) container — not key-value, not CloudKit.
            caps.AddiCloud(false, true, false, false, new[] { ICloudContainer });

            caps.WriteToFile();
        }
    }
}
#endif
