using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// Applies the iOS build configuration headlessly (BUILD_PLAN Phase D.5).
    /// Sets the values that DON'T need art — product/bundle identity, portrait
    /// lock, iOS 15 minimum. App icon + launch screen need real art and are
    /// flagged in the checklist; a missing icon does not break a Player build,
    /// it just ships the default. Run via
    ///   -executeMethod OriAscendant.EditorTools.BuildConfigurator.Apply
    /// (SceneBuilder.BuildAll also calls it).
    /// </summary>
    public static class BuildConfigurator
    {
        public const string ProductName = "Ori Ascendant";
        public const string CompanyName = "Vallicade"; // the real studio, distinct from ProductName
        public const string BundleId = "com.vallicade.oriascendant"; // verified: the App ID from the signed on-device build (bb0c700)
        public const string Version = "1.0.0";
        public const string MinIosVersion = "15.0";

        [MenuItem("Ori Ascendant/Apply Build Config")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            PlayerSettings.iOS.targetOSVersionString = MinIosVersion;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;

            // iOS is IL2CPP-only; set it explicitly so a Cloud Build is deterministic and
            // never relies on the project default. Signing (team id, distribution cert,
            // provisioning profile) and the per-upload build number are owned by Unity
            // Cloud Build, not committed here — see docs/RELEASE_CHECKLIST.md.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            // Portrait lock (idle game, single screen).
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            AssetDatabase.SaveAssets();
            Debug.Log($"BuildConfigurator: applied {ProductName} {Version} ({BundleId}), iOS {MinIosVersion} portrait.");
        }
    }
}
