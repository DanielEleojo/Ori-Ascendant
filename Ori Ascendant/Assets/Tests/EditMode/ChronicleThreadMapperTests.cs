using System.Collections.Generic;
using NUnit.Framework;
using OriAscendant.Save;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #27: node-per-entry mapping from chronicle data (acceptance criterion 4).
    /// ChronicleThreadMapper is pure (no MonoBehaviour, no scene), so all paths
    /// are coverable in EditMode without any host setup.
    /// </summary>
    public class ChronicleThreadMapperTests
    {
        private static ChronicleEntry Entry(bool ascended, int gen = 1, string rem = "A Title") =>
            new ChronicleEntry
            {
                generationNumber = gen,
                didAscend = ascended,
                remembrance = rem,
                chosenOri = 0,
                completedTimestamp = 1000,
            };

        // ---- ascended node brightness ----

        [Test]
        public void Ascended_NodeColor_HasFullAlpha()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: true));
            Assert.AreEqual(1.0f, state.NodeColor.a, 1e-4f, "ascended node must be at full brightness");
        }

        [Test]
        public void Fallen_NodeColor_HasLowerAlphaThanAscended()
        {
            var fallen   = ChronicleThreadMapper.Map(Entry(ascended: false));
            var ascended = ChronicleThreadMapper.Map(Entry(ascended: true));
            Assert.Less(fallen.NodeColor.a, ascended.NodeColor.a,
                "fallen node must be dimmer than ascended");
        }

        [Test]
        public void Fallen_NodeColor_IsVisible()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: false));
            Assert.Greater(state.NodeColor.a, 0.10f,
                "fallen node must still be visible — it is honoured, not erased");
        }

        // ---- colour identity ----

        [Test]
        public void Ascended_NodeColor_IsGold()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: true));
            var gold  = Palette.AseGold;
            Assert.AreEqual(gold.r, state.NodeColor.r, 0.01f, "ascended: red channel should be AseGold");
            Assert.AreEqual(gold.g, state.NodeColor.g, 0.01f, "ascended: green channel should be AseGold");
            Assert.AreEqual(gold.b, state.NodeColor.b, 0.01f, "ascended: blue channel should be AseGold");
        }

        [Test]
        public void Fallen_NodeColor_IsWarm_NotGrey()
        {
            // ART_BIBLE §1 / Palette: fallen ancestors are warm ember — never grey,
            // never red-as-failure. Verify red channel dominates blue.
            var state = ChronicleThreadMapper.Map(Entry(ascended: false));
            Assert.Greater(state.NodeColor.r, state.NodeColor.b,
                "fallen node colour must be warm (r > b); EmberWarm satisfies this");
        }

        [Test]
        public void Fallen_NodeColor_UsesEmberPalette()
        {
            // Fallen node must carry the same hue as Palette.EmberWarm.
            var state = ChronicleThreadMapper.Map(Entry(ascended: false));
            var ember = Palette.EmberWarm;
            Assert.AreEqual(ember.r, state.NodeColor.r, 0.01f, "fallen: red channel should be EmberWarm");
            Assert.AreEqual(ember.g, state.NodeColor.g, 0.01f, "fallen: green channel should be EmberWarm");
            Assert.AreEqual(ember.b, state.NodeColor.b, 0.01f, "fallen: blue channel should be EmberWarm");
        }

        [Test]
        public void AscendedAndFallen_ProduceDifferentHues()
        {
            var asc  = ChronicleThreadMapper.Map(Entry(ascended: true));
            var fall = ChronicleThreadMapper.Map(Entry(ascended: false));
            bool sameDot = Mathf.Approximately(asc.NodeColor.r, fall.NodeColor.r)
                        && Mathf.Approximately(asc.NodeColor.g, fall.NodeColor.g)
                        && Mathf.Approximately(asc.NodeColor.b, fall.NodeColor.b);
            Assert.IsFalse(sameDot, "ascended gold and fallen ember must be visually distinct hues");
        }

        // ---- label text ----

        [Test]
        public void Map_Ascended_LabelContainsAscended()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: true, gen: 3));
            StringAssert.Contains("Ascended", state.Label);
        }

        [Test]
        public void Map_Fallen_LabelContainsReturnedToTheSource()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: false, gen: 2));
            StringAssert.Contains("Returned to the source", state.Label);
        }

        [Test]
        public void Map_Label_ContainsGenerationNumber()
        {
            var state = ChronicleThreadMapper.Map(Entry(ascended: true, gen: 7));
            StringAssert.Contains("7", state.Label);
        }

        [Test]
        public void Map_Remembrance_IsPreserved()
        {
            const string rem = "Aṣẹ́gun Adé";
            var state = ChronicleThreadMapper.Map(Entry(ascended: true, rem: rem));
            Assert.AreEqual(rem, state.Remembrance);
        }

        [Test]
        public void Map_NullRemembrance_FallsBackToEmDash()
        {
            var entry = new ChronicleEntry { generationNumber = 1, didAscend = false, remembrance = null };
            var state = ChronicleThreadMapper.Map(entry);
            Assert.AreEqual("—", state.Remembrance, "null remembrance must render as em-dash, not empty/null");
        }

        // ---- MapAll: one node per entry (acceptance criterion 4) ----

        [Test]
        public void MapAll_ReturnsOneNodePerEntry()
        {
            var chronicle = new List<ChronicleEntry>
            {
                Entry(ascended: true,  gen: 1),
                Entry(ascended: false, gen: 2),
                Entry(ascended: true,  gen: 3),
            };
            var nodes = ChronicleThreadMapper.MapAll(chronicle);
            Assert.AreEqual(chronicle.Count, nodes.Length,
                "MapAll must produce exactly one node per chronicle entry — the thread is never broken");
        }

        [Test]
        public void MapAll_EmptyList_ReturnsEmpty()
        {
            var nodes = ChronicleThreadMapper.MapAll(new List<ChronicleEntry>());
            Assert.AreEqual(0, nodes.Length, "empty chronicle → empty node array");
        }

        [Test]
        public void MapAll_Null_ReturnsEmpty()
        {
            var nodes = ChronicleThreadMapper.MapAll(null);
            Assert.AreEqual(0, nodes.Length, "null chronicle → empty node array (no exception)");
        }

        [Test]
        public void MapAll_PreservesOrder()
        {
            var chronicle = new List<ChronicleEntry>
            {
                Entry(ascended: true,  gen: 1),
                Entry(ascended: false, gen: 2),
            };
            var nodes = ChronicleThreadMapper.MapAll(chronicle);
            StringAssert.Contains("1", nodes[0].Label, "first node must correspond to gen 1");
            StringAssert.Contains("2", nodes[1].Label, "second node must correspond to gen 2");
        }

        // ---- thread line: always present ----

        [Test]
        public void ThreadLineColor_HasNonZeroAlpha()
        {
            Assert.Greater(ChronicleThreadMapper.ThreadLineColor.a, 0f,
                "thread line must always be visible — the unbroken light");
        }

        // ---- issue #7: canonical fallen alpha agrees with PathMotif.FallenAlpha ----

        [Test]
        public void Fallen_Alpha_AgreesWithPathMotifCanonical()
        {
            float chronicleAlpha = ChronicleThreadMapper.Map(Entry(ascended: false)).NodeColor.a;
            Assert.AreEqual(PathMotif.FallenAlpha, chronicleAlpha, 1e-4f,
                "ChronicleThreadMapper fallen alpha must equal PathMotif.FallenAlpha — " +
                "EmberWarm hue is kept by design, but the alpha must be canonical (#7)");
        }
    }
}
