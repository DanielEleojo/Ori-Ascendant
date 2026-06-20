using NUnit.Framework;
using OriAscendant.Save;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate: shrine row visual state — headless (no scene, no MonoBehaviour).
    /// Covers issue #29: council ancestors rendered as silhouettes of light with
    /// title, remembrance, and radiance-vs-ember visual distinction.
    /// </summary>
    public class ShrineAncestorPresenterTests
    {
        private static AncestorData Ancestor(int path, bool ascended, string remembrance = null) =>
            new AncestorData
            {
                path = path,
                didAscend = ascended,
                bonusMultiplier = ascended ? 1.0 : 0.4,
                peakStage = 5,
                completedTimestamp = 100,
                remembrance = remembrance,
            };

        // ---- radiance vs ember ----

        [Test]
        public void Ascended_HasFullAlpha()
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(1, ascended: true), 3);
            Assert.AreEqual(1.0f, row.SilhouetteColor.a, 1e-4f,
                "ascended ancestor must read at full radiance");
        }

        [Test]
        public void Fallen_HasLowerAlphaThanAscended()
        {
            var fallen   = ShrineAncestorPresenter.Map(Ancestor(1, ascended: false), 3);
            var ascended = ShrineAncestorPresenter.Map(Ancestor(1, ascended: true), 3);
            Assert.Less(fallen.SilhouetteColor.a, ascended.SilhouetteColor.a,
                "fallen ancestor must read dimmer than ascended");
        }

        [Test]
        public void Fallen_IsNotInvisible()
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(1, ascended: false), 3);
            Assert.Greater(row.SilhouetteColor.a, 0.10f,
                "fallen ancestor must remain visible (low ember, not gone)");
        }

        // ---- path colour fidelity ----

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Ascended_ReflectsPathColour(int pathIndex)
        {
            var row      = ShrineAncestorPresenter.Map(Ancestor(pathIndex, ascended: true), 1);
            var expected = PathMotif.ColorOf(pathIndex);
            Assert.AreEqual(expected.r, row.SilhouetteColor.r, 0.01f, $"path {pathIndex} red");
            Assert.AreEqual(expected.g, row.SilhouetteColor.g, 0.01f, $"path {pathIndex} green");
            Assert.AreEqual(expected.b, row.SilhouetteColor.b, 0.01f, $"path {pathIndex} blue");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Fallen_KeepsPathHue_NotGreyedOut(int pathIndex)
        {
            var fallen   = ShrineAncestorPresenter.Map(Ancestor(pathIndex, ascended: false), 1);
            var ascended = ShrineAncestorPresenter.Map(Ancestor(pathIndex, ascended: true), 1);
            Assert.AreEqual(ascended.SilhouetteColor.r, fallen.SilhouetteColor.r, 0.01f,
                $"path {pathIndex}: fallen must keep path hue — only alpha dims, not the colour");
            Assert.AreEqual(ascended.SilhouetteColor.g, fallen.SilhouetteColor.g, 0.01f,
                $"path {pathIndex}: fallen must keep path hue (g)");
            Assert.AreEqual(ascended.SilhouetteColor.b, fallen.SilhouetteColor.b, 0.01f,
                $"path {pathIndex}: fallen must keep path hue (b)");
        }

        [Test]
        public void PathlessAncestor_UsesAseGold()
        {
            var row  = ShrineAncestorPresenter.Map(Ancestor(-1, ascended: true), 1);
            var gold = Palette.AseGold;
            Assert.AreEqual(gold.r, row.SilhouetteColor.r, 0.01f, "path-less ancestor: red must be AseGold");
            Assert.AreEqual(gold.g, row.SilhouetteColor.g, 0.01f, "path-less ancestor: green must be AseGold");
            Assert.AreEqual(gold.b, row.SilhouetteColor.b, 0.01f, "path-less ancestor: blue must be AseGold");
        }

        [Test]
        public void DifferentPaths_ProduceDifferentHues()
        {
            var earth   = ShrineAncestorPresenter.Map(Ancestor(0, ascended: true), 1).SilhouetteColor;
            var thunder = ShrineAncestorPresenter.Map(Ancestor(1, ascended: true), 1).SilhouetteColor;
            var river   = ShrineAncestorPresenter.Map(Ancestor(2, ascended: true), 1).SilhouetteColor;
            Assert.AreNotEqual(earth,   thunder, "earth vs thunder must differ");
            Assert.AreNotEqual(thunder, river,   "thunder vs river must differ");
        }

        // ---- title ----

        [Test]
        public void Title_ContainsGenerationNumber()
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(0, ascended: true), 4);
            StringAssert.Contains("4", row.Title);
        }

        [Test]
        public void Title_Ascended_HasNoEmberMark()
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(1, ascended: true), 1);
            StringAssert.DoesNotContain("ember", row.Title);
        }

        [Test]
        public void Title_Fallen_ContainsEmberMark()
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(2, ascended: false), 2);
            StringAssert.Contains("ember", row.Title);
        }

        [TestCase(0, "Earth")]
        [TestCase(1, "Thunder")]
        [TestCase(2, "River")]
        public void Title_ContainsPathName(int pathIndex, string pathName)
        {
            var row = ShrineAncestorPresenter.Map(Ancestor(pathIndex, ascended: true), 1);
            StringAssert.Contains(pathName, row.Title);
        }

        // ---- remembrance ----

        [Test]
        public void Remembrance_IsReturned_WhenSet()
        {
            var row = ShrineAncestorPresenter.Map(
                Ancestor(0, ascended: true, remembrance: "The Steadfast"), 1);
            Assert.AreEqual("The Steadfast", row.Remembrance);
        }

        [Test]
        public void Remembrance_IsEmpty_WhenNull()
        {
            var row = ShrineAncestorPresenter.Map(
                Ancestor(0, ascended: true, remembrance: null), 1);
            Assert.IsEmpty(row.Remembrance);
        }

        [Test]
        public void Remembrance_IsEmpty_WhenEmptyString()
        {
            var row = ShrineAncestorPresenter.Map(
                Ancestor(1, ascended: false, remembrance: ""), 1);
            Assert.IsEmpty(row.Remembrance);
        }

        // ---- empty seat ----

        [Test]
        public void EmptySeat_HasExpectedTitle()
        {
            Assert.AreEqual("An empty seat awaits", ShrineAncestorPresenter.EmptySeat.Title);
        }

        [Test]
        public void EmptySeat_RemembranceIsEmpty()
        {
            Assert.IsEmpty(ShrineAncestorPresenter.EmptySeat.Remembrance);
        }

        [Test]
        public void EmptySeat_IsNearlyTransparent()
        {
            Assert.Less(ShrineAncestorPresenter.EmptySeat.SilhouetteColor.a, 0.25f,
                "empty seat must be barely visible — a hint, not a presence");
        }

        [Test]
        public void EmptySeat_IsFainterThanFallen()
        {
            float fallenAlpha =
                ShrineAncestorPresenter.Map(Ancestor(1, ascended: false), 1).SilhouetteColor.a;
            Assert.Less(ShrineAncestorPresenter.EmptySeat.SilhouetteColor.a, fallenAlpha,
                "an empty seat must be fainter than even a fallen ancestor");
        }
    }
}
