using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Smoke gate for SceneBuilder wiring (issues #1 and #3).
    /// Reads Main.unity as text to verify AssignPath/Assign calls landed the right
    /// serialized-field names — locks down the whole class of path-drift and
    /// missing-Assign bugs, not just the two that bit this cycle.
    /// Run SceneBuilder.BuildAll before this suite (it runs first in the batchmode gate).
    /// </summary>
    public class SceneBuilderSmokeTests
    {
        private string _sceneText;

        [SetUp]
        public void SetUp()
        {
            string path = Path.Combine(Application.dataPath, "Scenes", "Main.unity");
            Assert.IsTrue(File.Exists(path),
                "Main.unity not found — run SceneBuilder.BuildAll to generate it");
            _sceneText = File.ReadAllText(path);
        }

        // ---- issue #1: CardRow field rename contribution → remembrance ----

        [Test]
        public void CouncilRows_UseRemembranceField()
        {
            StringAssert.Contains("remembrance: {fileID:", _sceneText,
                "CouncilScreenView rows must use 'remembrance' — run SceneBuilder.BuildAll (#1)");
        }

        [Test]
        public void CouncilRows_DoNotUseContributionField()
        {
            StringAssert.DoesNotContain("contribution: {fileID:", _sceneText,
                "'contribution' is the old renamed field; AssignPath must use 'remembrance' (#1)");
        }

        // ---- issue #3: Settings screen haptics + reduce-motion toggles ----

        [Test]
        public void SettingsScreen_HasHapticsToggle()
        {
            StringAssert.Contains("_hapticsToggle: {fileID:", _sceneText,
                "_hapticsToggle must be wired in SettingsScreen — run SceneBuilder.BuildAll (#3)");
        }

        [Test]
        public void SettingsScreen_HasReduceMotionToggle()
        {
            StringAssert.Contains("_reduceMotionToggle: {fileID:", _sceneText,
                "_reduceMotionToggle must be wired in SettingsScreen — run SceneBuilder.BuildAll (#3)");
        }
    }
}
