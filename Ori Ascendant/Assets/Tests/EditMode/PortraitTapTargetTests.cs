using System.Reflection;
using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #19: de-frame the silhouette — the portrait Image must be a
    /// transparent, raycast-only tap-target. Visuals (aura, silhouette, motes)
    /// come from child Images that MainScreenSkin builds; the tap-target Image
    /// itself draws nothing and never receives a sprite at runtime.
    /// </summary>
    public class PortraitTapTargetTests
    {
        [Test]
        public void MainScreenController_HasNoPortraitImageField()
        {
            var field = typeof(MainScreenController)
                .GetField("_portraitImage",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(field,
                "_portraitImage must be removed — the portrait is a transparent " +
                "hit-area; the controller no longer assigns sprites to it (issue #19)");
        }

        [Test]
        public void MainScreenController_HasNoRefreshPortraitMethod()
        {
            var method = typeof(MainScreenController)
                .GetMethod("RefreshPortrait",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(method,
                "RefreshPortrait() must be removed — it was the sole site that " +
                "assigned a sprite to the portrait Image at runtime (issue #19)");
        }
    }
}
