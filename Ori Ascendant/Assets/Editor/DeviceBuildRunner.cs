using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// One-click device build: applies the committed build config, builds the
    /// iOS player to Builds/iOS (appending on rebuilds, so Xcode signing and
    /// CocoaPods state survive), then hands off to Xcode to compile, install,
    /// and launch on the connected iPhone (BuildOptions.AutoRunPlayer).
    /// Builds through the active Build Profile (Assets/Settings/Build
    /// Profiles/iOS.asset) when one is set, so profile overrides apply.
    /// Menu: Ori Ascendant > Build &amp; Run on Device (Cmd+Shift+R).
    /// </summary>
    public static class DeviceBuildRunner
    {
        public const string BuildPath = "Builds/iOS";

        [MenuItem("Ori Ascendant/Build & Run on Device %#r")]
        public static void BuildAndRun()
        {
            BuildConfigurator.Apply();

            // Simulator builds flip this to SimulatorSDK; never ship one to device.
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            // SetBuildLocation silently no-ops if the folder doesn't exist yet.
            System.IO.Directory.CreateDirectory(BuildPath);
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.iOS, BuildPath);

            var profile = BuildProfile.GetActiveBuildProfile();
            BuildReport report;
            if (profile != null)
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = BuildPath,
                    options = BuildOptions.AutoRunPlayer,
                });
            }
            else
            {
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0)
                {
                    Debug.LogError("DeviceBuildRunner: no enabled scenes in Build Settings.");
                    return;
                }

                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = BuildPath,
                    target = BuildTarget.iOS,
                    options = BuildOptions.AutoRunPlayer,
                });
            }

            Debug.Log($"DeviceBuildRunner: {report.summary.result} in {report.summary.totalTime.TotalSeconds:F0}s -> {BuildPath}");
        }
    }
}
