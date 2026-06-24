using NUnit.Framework;
using OriAscendant.Save;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #22: star-state mapping from council data (acceptance criterion 4).
    /// Issue #26: deep-field star count and colour from chronicle/lineage data.
    /// ConstellationStarMapper is pure (no MonoBehaviour, no scene), so all
    /// paths are coverable in EditMode without any host setup.
    /// </summary>
    public class ConstellationStarMapperTests
    {
        private static AncestorData Ancestor(int path, bool ascended) => new AncestorData
        {
            path = path,
            didAscend = ascended,
            bonusMultiplier = ascended ? 1.0 : 0.4,
            peakStage = 5,
            completedTimestamp = 100,
        };

        private static ChronicleEntry Entry(bool ascended) => new ChronicleEntry
        {
            generationNumber = 1,
            didAscend = ascended,
            completedTimestamp = 100,
        };

        private static SaveData BuildSave(int chronicleCount, int councilCount)
        {
            var save = new SaveData();
            for (int i = 0; i < chronicleCount; i++)
                save.chronicle.Add(Entry(ascended: i % 2 == 0));
            for (int i = 0; i < councilCount; i++)
                save.council.Add(Ancestor(path: 1, ascended: true));
            return save;
        }

        // ---- ascended vs fallen brightness ----

        [Test]
        public void Ascended_HasFullAlpha()
        {
            var color = ConstellationStarMapper.StarColor(Ancestor(1, ascended: true));
            Assert.AreEqual(1.0f, color.a, 1e-4f, "ascended star must be at full brightness");
        }

        [Test]
        public void Fallen_HasLowerAlphaThanAscended()
        {
            var fallen   = ConstellationStarMapper.StarColor(Ancestor(1, ascended: false));
            var ascended = ConstellationStarMapper.StarColor(Ancestor(1, ascended: true));
            Assert.Less(fallen.a, ascended.a, "fallen star must be dimmer than ascended");
        }

        [Test]
        public void Fallen_IsNotInvisible()
        {
            var color = ConstellationStarMapper.StarColor(Ancestor(1, ascended: false));
            Assert.Greater(color.a, 0.10f, "fallen star must still be visible (low ember, not invisible)");
        }

        // ---- path colour fidelity ----

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Ascended_ReflectsPathColour_MatchesPathMotif(int pathIndex)
        {
            var color    = ConstellationStarMapper.StarColor(Ancestor(pathIndex, ascended: true));
            var expected = PathMotif.ColorOf(pathIndex);
            Assert.AreEqual(expected.r, color.r, 0.01f, $"path {pathIndex} red channel");
            Assert.AreEqual(expected.g, color.g, 0.01f, $"path {pathIndex} green channel");
            Assert.AreEqual(expected.b, color.b, 0.01f, $"path {pathIndex} blue channel");
        }

        [Test]
        public void PathlessAncestor_UsesNeutralGold()
        {
            // path = -1 means no path chosen; star should be neutral gold, not the
            // indigo-neutral chip colour used by the old strip.
            var color    = ConstellationStarMapper.StarColor(Ancestor(-1, ascended: true));
            var gold     = Palette.AseGold;
            Assert.AreEqual(gold.r, color.r, 0.01f, "path-less star: red channel should be AseGold");
            Assert.AreEqual(gold.g, color.g, 0.01f, "path-less star: green channel should be AseGold");
            Assert.AreEqual(gold.b, color.b, 0.01f, "path-less star: blue channel should be AseGold");
        }

        [Test]
        public void DifferentPaths_ProduceDifferentColours()
        {
            var earth   = ConstellationStarMapper.StarColor(Ancestor(0, ascended: true));
            var thunder = ConstellationStarMapper.StarColor(Ancestor(1, ascended: true));
            var river   = ConstellationStarMapper.StarColor(Ancestor(2, ascended: true));
            Assert.AreNotEqual(earth,   thunder, "earth vs thunder must differ");
            Assert.AreNotEqual(thunder, river,   "thunder vs river must differ");
        }

        // ---- fallen carries path colour ----

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Fallen_SameHueAsAscended_SamePath(int pathIndex)
        {
            var fallen   = ConstellationStarMapper.StarColor(Ancestor(pathIndex, ascended: false));
            var ascended = ConstellationStarMapper.StarColor(Ancestor(pathIndex, ascended: true));
            Assert.AreEqual(ascended.r, fallen.r, 0.01f, $"path {pathIndex}: fallen red");
            Assert.AreEqual(ascended.g, fallen.g, 0.01f, $"path {pathIndex}: fallen green");
            Assert.AreEqual(ascended.b, fallen.b, 0.01f, $"path {pathIndex}: fallen blue");
        }

        // ---- empty seat ----

        [Test]
        public void EmptySeat_IsVeryFaint()
        {
            var color = ConstellationStarMapper.EmptySeatColor();
            Assert.Less(color.a, 0.30f, "empty seat must be a faint unlit point");
        }

        [Test]
        public void EmptySeat_IsFainterThanFallen()
        {
            var fallen    = ConstellationStarMapper.StarColor(Ancestor(1, ascended: false));
            var emptySeat = ConstellationStarMapper.EmptySeatColor();
            Assert.Less(emptySeat.a, fallen.a, "empty seat must be fainter than a fallen ancestor");
        }

        [Test]
        public void EmptySeat_IsNotBlack()
        {
            // A completely dark colour would be invisible on the indigo sky and
            // give no hint that a slot exists.
            var color = ConstellationStarMapper.EmptySeatColor();
            float luma = 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
            Assert.Greater(luma * color.a, 0.01f, "empty seat must have some perceptible light");
        }

        // ---- deep field: field population from chronicle/lineage data (issue #26) ----

        [Test]
        public void DeepFieldStarCount_NullSave_ReturnsZero()
        {
            Assert.AreEqual(0, ConstellationStarMapper.DeepFieldStarCount(null));
        }

        [Test]
        public void DeepFieldStarCount_Gen1_EmptySky()
        {
            // Before any generation completes, the sky is near-empty — no deep field.
            var save = new SaveData(); // chronicle = 0, council = 0
            Assert.AreEqual(0, ConstellationStarMapper.DeepFieldStarCount(save),
                "Gen 1 (no completed generations) must show a near-empty sky");
        }

        [Test]
        public void DeepFieldStarCount_ThreeCompleted_NoRetired()
        {
            // Three completions, all in the council (≤ 5) — nothing retired yet.
            var save = BuildSave(chronicleCount: 3, councilCount: 3);
            Assert.AreEqual(0, ConstellationStarMapper.DeepFieldStarCount(save));
        }

        [Test]
        public void DeepFieldStarCount_SixthGeneration_OneRetired()
        {
            // 6 chronicle entries; council full at 5 → 1 ancestor retired to deep field.
            var save = BuildSave(chronicleCount: 6, councilCount: 5);
            Assert.AreEqual(1, ConstellationStarMapper.DeepFieldStarCount(save));
        }

        [Test]
        public void DeepFieldStarCount_GrowsWithGenerations()
        {
            // Each new generation beyond 5 adds one more deep-field star.
            var save7  = BuildSave(chronicleCount: 7,  councilCount: 5);
            var save10 = BuildSave(chronicleCount: 10, councilCount: 5);
            Assert.AreEqual(2, ConstellationStarMapper.DeepFieldStarCount(save7));
            Assert.AreEqual(5, ConstellationStarMapper.DeepFieldStarCount(save10));
        }

        [Test]
        public void DeepFieldStarColor_Ascended_IsFainterThanCouncilAscended()
        {
            float deepA    = ConstellationStarMapper.DeepFieldStarColor(didAscend: true).a;
            float councilA = ConstellationStarMapper.StarColor(Ancestor(1, ascended: true)).a;
            Assert.Less(deepA, councilA, "deep-field ascended must be dimmer than council ascended");
        }

        [Test]
        public void DeepFieldStarColor_Fallen_IsFainterThanCouncilFallen()
        {
            float deepA    = ConstellationStarMapper.DeepFieldStarColor(didAscend: false).a;
            float councilA = ConstellationStarMapper.StarColor(Ancestor(1, ascended: false)).a;
            Assert.Less(deepA, councilA, "deep-field fallen must be dimmer than council fallen");
        }

        [Test]
        public void DeepFieldStarColor_Ascended_IsBrighterThanFallen()
        {
            float ascendedA = ConstellationStarMapper.DeepFieldStarColor(didAscend: true).a;
            float fallenA   = ConstellationStarMapper.DeepFieldStarColor(didAscend: false).a;
            Assert.Greater(ascendedA, fallenA, "ascended deep-field star must outshine fallen");
        }

        [Test]
        public void DeepFieldStarColor_IsNotInvisible()
        {
            var fallen = ConstellationStarMapper.DeepFieldStarColor(didAscend: false);
            Assert.Greater(fallen.a, 0.05f, "even fallen deep-field stars must be perceptible");
        }

        [Test]
        public void DeepFieldStarColor_UsesAseGoldHue()
        {
            // Path data is absent in the chronicle; all deep-field stars use neutral AseGold.
            var deepColor = ConstellationStarMapper.DeepFieldStarColor(didAscend: true);
            var gold      = Palette.AseGold;
            Assert.AreEqual(gold.r, deepColor.r, 0.01f, "deep-field red channel must match AseGold");
            Assert.AreEqual(gold.g, deepColor.g, 0.01f, "deep-field green channel must match AseGold");
            Assert.AreEqual(gold.b, deepColor.b, 0.01f, "deep-field blue channel must match AseGold");
        }

        // ---- issue #7: canonical fallen alpha agrees with PathMotif.FallenAlpha ----

        [Test]
        public void Fallen_Alpha_AgreesWithPathMotifCanonical()
        {
            float mapperAlpha = ConstellationStarMapper.StarColor(Ancestor(1, ascended: false)).a;
            Assert.AreEqual(PathMotif.FallenAlpha, mapperAlpha, 1e-4f,
                "ConstellationStarMapper fallen alpha must equal PathMotif.FallenAlpha — route through AncestorTint (#7)");
        }
    }
}
