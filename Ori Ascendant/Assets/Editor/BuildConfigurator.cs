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
        public const string BundleId = "com.oriascendant.game"; // placeholder — set the real App ID before submission
        public const string Version = "0.1.0";
        public const string MinIosVersion = "15.0";

        [MenuItem("Ori Ascendant/Apply Build Config")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            PlayerSettings.iOS.targetOSVersionString = MinIosVersion;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;

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
