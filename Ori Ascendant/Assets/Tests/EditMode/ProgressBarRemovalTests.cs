using System.Reflection;
using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #28: the standalone progress bar has been removed — the vessel is
    /// the sole progress gauge. Reflection gate ensures the old bar fields never
    /// creep back into the controller or skin.
    /// </summary>
    public class ProgressBarRemovalTests
    {
        // ---- Controller no longer drives a bar ----

        [Test]
        public void MainScreenController_HasNo_ProgressRootField()
        {
            var field = typeof(MainScreenController)
                .GetField("_progressRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(field,
                "_progressRoot must be removed — the progress zone is gone (issue #28)");
        }

        [Test]
        public void MainScreenController_HasNo_BarFillField()
        {
            var field = typeof(MainScreenController)
                .GetField("_barFill", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(field,
                "_barFill must be removed — the controller no longer drives a bar; " +
                "progress is conveyed by the vessel fill in MainScreenSkin (issue #28)");
        }

        [Test]
        public void MainScreenController_HasNo_ProgressLabelField()
        {
            var field = typeof(MainScreenController)
                .GetField("_progressLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(field,
                "_progressLabel must be removed — the progress label lived inside the " +
                "bar zone; the Àṣẹ counter in MainScreenView carries the figure (issue #28)");
        }

        // ---- Skin: bar replaced by vessel waterline glow ----

        [Test]
        public void MainScreenSkin_HasNo_BarFillField()
        {
            var field = typeof(MainScreenSkin)
                .GetField("_barFill", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(field,
                "_barFill must be removed from the skin — the bar is gone; tribulation " +
                "fraction now reads from vessel fill (issue #28)");
        }

        [Test]
        public void MainScreenSkin_Has_VesselWaterlineGlowField()
        {
            var field = typeof(MainScreenSkin)
                .GetField("_vesselWaterlineGlow", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                "_vesselWaterlineGlow must exist in the skin — the bar's leading-edge " +
                "glow migrates onto the vessel's rising waterline (issue #28)");
        }
    }
}
