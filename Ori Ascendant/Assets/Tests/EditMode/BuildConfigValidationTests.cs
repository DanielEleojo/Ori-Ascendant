using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate D: the headless build config landed the expected PlayerSettings.
    /// Reads PlayerSettings directly (BuildConfigurator lives in the editor
    /// assembly, which the test assembly does not reference); relies on
    /// SceneBuilder.BuildAll / BuildConfigurator.Apply having run earlier in the
    /// build+test chain, which is how the gate is always executed.
    /// </summary>
    public class BuildConfigValidationTests
    {
        [Test]
        public void ProductIdentity_IsSet()
        {
            Assert.AreEqual("Ori Ascendant", PlayerSettings.productName);
            Assert.AreEqual("Vallicade", PlayerSettings.companyName);
            Assert.AreEqual("1.1.0", PlayerSettings.bundleVersion);
        }

        /// <summary>Every App Store upload needs a unique, increasing
        /// CFBundleVersion; an empty one is rejected at upload.</summary>
        [Test]
        public void BuildNumber_IsSet_ForIos()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber));
        }

        [Test]
        public void BundleId_IsSet_ForIos()
        {
            Assert.AreEqual("com.vallicade.oriascendant",
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));
        }

        [Test]
        public void IosMinVersion_Is15()
        {
            Assert.AreEqual("15.0", PlayerSettings.iOS.targetOSVersionString);
        }

        [Test]
        public void Orientation_IsPortraitLocked()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);
        }
    }
}
