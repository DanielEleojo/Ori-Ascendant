using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Dynasty PRD Phase 1, slice 1: Àkùnlẹ̀yàn — the at-birth virtue vow.
    /// Tests cover ChooseOri persistence, save/load round-trip carry, and
    /// re-vow behaviour after the Tribulation reset (the generation reset
    /// point reused per the issue spec).
    /// </summary>
    public class OriSystemTests
    {
        private GameObject _host;
        private OriSystem _oriSystem;
        private OriConfig _config;
        private SaveData _save;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("OriTestHost");

            _config = ScriptableObject.CreateInstance<OriConfig>();
            _config.virtues = new[]
            {
                new OriVirtue { virtueName = "Patience", vowLine = "I will hold the long road." },
                new OriVirtue { virtueName = "Courage",  vowLine = "I will not turn." },
                new OriVirtue { virtueName = "Mercy",    vowLine = "I will spare what I could strike." },
            };

            _oriSystem = _host.AddComponent<OriSystem>();
            EditModeTestHelpers.Inject(_oriSystem, "_config", _config);
            ServiceLocator.Register(_oriSystem);

            _save = new SaveData();
            _oriSystem.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        [Test]
        public void FreshSave_HasNoOriVowed()
        {
            Assert.IsFalse(_oriSystem.HasChosen, "a fresh life starts without an Ori vow");
            Assert.AreEqual(-1, _oriSystem.ChosenIndex);
            Assert.IsNull(_oriSystem.ChosenVirtue);
        }

        [Test]
        public void ChooseOri_PersistsTheVowToSaveData()
        {
            bool chosenFired = false;
            int chosenIndex = -1;
            _oriSystem.OnOriChosen += i => { chosenFired = true; chosenIndex = i; };

            Assert.IsTrue(_oriSystem.ChooseOri(1));

            Assert.IsTrue(_oriSystem.HasChosen);
            Assert.AreEqual(1, _save.chosenOri, "vow is written to SaveData");
            Assert.AreEqual(1, _oriSystem.ChosenIndex);
            Assert.AreEqual("Courage", _oriSystem.ChosenVirtue.virtueName);
            Assert.IsTrue(chosenFired, "OnOriChosen fired");
            Assert.AreEqual(1, chosenIndex);
        }

        [Test]
        public void ChooseOri_RejectsOutOfRangeIndex()
        {
            Assert.IsFalse(_oriSystem.ChooseOri(-1));
            Assert.IsFalse(_oriSystem.ChooseOri(_config.Count));
            Assert.IsFalse(_oriSystem.ChooseOri(99));
            Assert.AreEqual(-1, _save.chosenOri, "no vow committed on bad input");
            Assert.IsFalse(_oriSystem.HasChosen);
        }

        [Test]
        public void ChooseOri_IsOneShotPerLife()
        {
            Assert.IsTrue(_oriSystem.ChooseOri(0));
            Assert.IsFalse(_oriSystem.ChooseOri(2), "a second vow this life must be refused");
            Assert.AreEqual(0, _save.chosenOri, "the first vow stands");
        }

        [Test]
        public void Vow_SurvivesSaveLoadRoundTrip()
        {
            _oriSystem.ChooseOri(2);

            var restored = SaveSerializer.FromJson(SaveSerializer.ToJson(_save));

            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored.chosenOri,
                "the chosen Ori persists across save/load (acceptance criterion #1)");
        }

        [Test]
        public void Begin_RehydratesAnExistingVowFromTheSave()
        {
            // Simulate restart: the save already holds a vow from the prior session.
            _save = new SaveData { chosenOri = 2 };
            _oriSystem.Begin(_save);

            Assert.IsTrue(_oriSystem.HasChosen, "Begin must read the existing vow");
            Assert.AreEqual(2, _oriSystem.ChosenIndex);
            Assert.AreEqual("Mercy", _oriSystem.ChosenVirtue.virtueName);
        }

        [Test]
        public void GenerationReset_ClearsTheVow_NextLifeCanReVow()
        {
            _oriSystem.ChooseOri(0);
            Assert.IsTrue(_oriSystem.HasChosen);

            // The Tribulation atomic reset is the one place SaveData.chosenOri
            // returns to -1 — simulate that field-level reset here. The
            // TribulationSystem integration is asserted separately in
            // TribulationSystemTests.Resolve_Ascend_WritesTheCompleteGenerationReset.
            _save.chosenOri = -1;

            Assert.IsFalse(_oriSystem.HasChosen,
                "post-Crossing the life must surface a fresh Ori choice (acceptance criterion #2)");
            Assert.IsTrue(_oriSystem.ChooseOri(2), "the next life can vow a new Ori");
            Assert.AreEqual(2, _save.chosenOri);
        }
    }
}
