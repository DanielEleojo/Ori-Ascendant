#if UNITY_IOS
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
