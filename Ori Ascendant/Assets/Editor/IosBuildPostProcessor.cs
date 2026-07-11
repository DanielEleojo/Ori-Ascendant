// APPLE_GAMEKIT define gate: the Game Center + iCloud entitlements below are only
// valid once the Apple.Core / Apple.GameKit packages are wired in AND the matching
// App ID (with those capabilities + a real iCloud container) exists in the Apple
// Developer console. Until then this hook is excluded so device builds sign cleanly
// against a plain Apple Development profile (cloud save falls back to local — see
// CloudSaveManager / GameKitCloudSaveProvider, gated by the same symbol).
// IosAdCompliancePostProcessor below is gated on UNITY_IOS only — ad compliance
// (SKAdNetwork ids + app-level privacy manifest) must ship on every iOS build.
#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace OriAscendant.EditorTools
{
#if APPLE_GAMEKIT
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
#endif // APPLE_GAMEKIT

    /// <summary>
    /// App-Store ad compliance for the LevelPlay bottom banner. Ads are NON-personalized by
    /// product decision: deliberately NO ATT prompt and NO NSUserTrackingUsageDescription.
    ///   • SKAdNetworkItems in Info.plist — privacy-safe install attribution for the ad
    ///     networks in the mediation waterfall (SKAdNetwork works without ATT).
    ///   • App-level PrivacyInfo.xcprivacy at the app-bundle root — declares the APP tracks
    ///     nothing and collects nothing. SDKs (Unity engine, LevelPlay) ship their OWN
    ///     bundled manifests for their own data use; this is only the app's.
    /// Same caveat as the capability hook above: UnityEditor.iOS.Xcode cannot compile on the
    /// Linux dev box — VERIFY ON THE FIRST CLOUD BUILD (SKAdNetworkItems present in the built
    /// Info.plist, PrivacyInfo.xcprivacy present at the app-bundle root).
    /// </summary>
    public static class IosAdCompliancePostProcessor
    {
        // ponytail: representative set only — Unity Ads, ironSource/LevelPlay, and the usual
        // LevelPlay-mediated networks. LevelPlay publishes the authoritative full list for the
        // networks enabled in the dashboard; reconcile against it on the first Cloud Build.
        // Extra ids are harmless, missing ids just lose attribution. Keep all ids lowercase.
        private static readonly string[] SkAdNetworkIds =
        {
            "4dzt52r2t5.skadnetwork", // Unity Ads
            "su67r6k2v3.skadnetwork", // ironSource / LevelPlay
            "cstr6suwn9.skadnetwork", // Google AdMob
            "ludvb6z3bs.skadnetwork", // AppLovin
            "v9wttpbfk9.skadnetwork", // Meta Audience Network
            "n38lu8286q.skadnetwork", // Meta Audience Network (secondary)
            "kbd757ywx3.skadnetwork", // Mintegral
            "238da6jt44.skadnetwork", // Pangle
            "22mmun2rn5.skadnetwork", // Pangle (China)
            "gta9lk7p23.skadnetwork", // Vungle / Liftoff Monetize
            "wzmmz9fp6w.skadnetwork", // InMobi
        };

        private const string PrivacyManifestName = "PrivacyInfo.xcprivacy";

        // App-level manifest: no tracking, no tracking domains, no data collected by the app
        // itself (non-personalized ads, no ATT). NSPrivacyAccessedAPITypes is empty because
        // the thin app launcher uses no required-reason APIs — engine/SDK usage is declared
        // in UnityFramework's and LevelPlay's own bundled manifests.
        private const string PrivacyManifestXml =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>NSPrivacyTracking</key>
    <false/>
    <key>NSPrivacyTrackingDomains</key>
    <array/>
    <key>NSPrivacyCollectedDataTypes</key>
    <array/>
    <key>NSPrivacyAccessedAPITypes</key>
    <array/>
</dict>
</plist>
";

        [PostProcessBuild(110)] // after the capability hook; each does read→modify→write, so order-safe
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            AddSkAdNetworkItems(pathToBuiltProject);
            AddAppPrivacyManifest(pathToBuiltProject);
        }

        private static void AddSkAdNetworkItems(string pathToBuiltProject)
        {
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Append-if-missing rather than CreateArray (which would replace the key): keeps
            // re-runs idempotent and preserves ids another postprocessor may already have added.
            PlistElementArray items = plist.root.values.TryGetValue("SKAdNetworkItems", out PlistElement found)
                ? found.AsArray()
                : plist.root.CreateArray("SKAdNetworkItems");

            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PlistElement el in items.values)
            {
                if (el is PlistElementDict dict
                    && dict.values.TryGetValue("SKAdNetworkIdentifier", out PlistElement id)
                    && id is PlistElementString str)
                {
                    present.Add(str.value);
                }
            }

            foreach (string id in SkAdNetworkIds)
            {
                if (present.Contains(id)) continue;
                items.AddDict().SetString("SKAdNetworkIdentifier", id);
            }

            plist.WriteToFile(plistPath);
        }

        private static void AddAppPrivacyManifest(string pathToBuiltProject)
        {
            // Overwrite every build — the manifest is generated here, never hand-edited in Xcode.
            File.WriteAllText(Path.Combine(pathToBuiltProject, PrivacyManifestName), PrivacyManifestXml);

            string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbx = new PBXProject();
            pbx.ReadFromFile(pbxPath);

            // Only wire the file in once (Append builds reuse the pbxproj; Replace regenerates it).
            if (pbx.FindFileGuidByProjectPath(PrivacyManifestName) == null)
            {
                string fileGuid = pbx.AddFile(PrivacyManifestName, PrivacyManifestName);
                // Add to the app target's Resources phase explicitly (extension-agnostic) so the
                // manifest lands at the app-bundle root, where App Store review expects it.
                string mainTargetGuid = pbx.GetUnityMainTargetGuid();
                pbx.AddFileToBuildSection(mainTargetGuid, pbx.GetResourcesBuildPhaseByTarget(mainTargetGuid), fileGuid);
                pbx.WriteToFile(pbxPath);
            }
        }
    }
}
#endif
