using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// PRD Issue #1 end-to-end verification: a simulated dynasty run through the real
    /// systems confirms the four PRD verification criteria (Verification section):
    ///
    /// 1. Steadfastness drives Crossing odds within the configured floor/ceiling band.
    /// 2. Titles (ascend) and Nicknames (fall) derive correctly into Chronicle entries.
    /// 3. The Chronicle accretes across generations.
    /// 4. Light dynasty compounding (forebear-seeded crossroads) fires in a
    ///    descendant's life.
    ///
    /// Setup mirrors FullLoopIntegrationTests but adds CrossroadsSystem and injects
    /// RemembranceConfig + CrossroadsDeckConfig into TribulationSystem so that
    /// Remembrance.Derive produces real strings end-to-end.
    /// </summary>
    public class DynastyArcIntegrationTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
        private AncestralCouncilSystem _council;
        private TribulationSystem _tribulation;
        private CrossroadsSystem _crossroads;
        private CloudSaveManager _cloud;
        private SaveData _save;

        // Seed card:
        //   option 0 → virtueIndex 0 (Ori-aligned when chosenOri=0)
        //   option 1 → virtueIndex 1 (diverges from chosenOri=0 → stray)
        private static CrossroadsCard SeedCard() => new CrossroadsCard
        {
            id = "card_a",
            prompt = "A stranger asks for your help.",
            options = new[]
            {
                new CrossroadsOption { virtueIndex = 0, optionText = "Hold your patience." },
                new CrossroadsOption { virtueIndex = 1, optionText = "Act with courage." },
            }
        };

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("DynastyArcHost");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());

            _cultivation = _host.AddComponent<CultivationSystem>();
            EditModeTestHelpers.InjectArray(_cultivation, "_stages", EditModeTestHelpers.MakeStageTable());
            EditModeTestHelpers.InjectArray(_cultivation, "_paths", EditModeTestHelpers.MakePathTable());
            EditModeTestHelpers.Inject(_cultivation, "_tribulationConfig",
                EditModeTestHelpers.MakeTribulationConfig());

            _council = _host.AddComponent<AncestralCouncilSystem>();
            EditModeTestHelpers.Inject(_council, "_config", EditModeTestHelpers.MakeCouncilConfig());

            _tribulation = _host.AddComponent<TribulationSystem>();
            EditModeTestHelpers.Inject(_tribulation, "_config", EditModeTestHelpers.MakeTribulationConfig());
            EditModeTestHelpers.Inject(_tribulation, "_gameplayConfig",
                EditModeTestHelpers.MakeGameplayConfig());
            // Remembrance configs are injected in SetUp so every test gets real Title/Nickname strings.
            EditModeTestHelpers.Inject(_tribulation, "_remembranceConfig",
                EditModeTestHelpers.MakeRemembranceConfig());
            EditModeTestHelpers.Inject(_tribulation, "_crossroadsDeck",
                EditModeTestHelpers.MakeCrossroadsDeckConfig());

            _crossroads = _host.AddComponent<CrossroadsSystem>();
            EditModeTestHelpers.Inject(_crossroads, "_config",
                EditModeTestHelpers.MakeCrossroadsConfig(SeedCard()));

            _cloud = _host.AddComponent<CloudSaveManager>();
            _cloud.Initialize(new FakeCloudProvider { AuthResult = false });

            ServiceLocator.Register(_aseGen);
            ServiceLocator.Register(_cultivation);
            ServiceLocator.Register(_council);
            ServiceLocator.Register(_tribulation);
            ServiceLocator.Register(_crossroads);
            ServiceLocator.Register(_cloud);

            _save = new SaveData();
            _aseGen.Begin(_save);
            _cultivation.Begin(_save);
            _council.Begin(_save);
            _tribulation.Begin(_save);
            _crossroads.Begin(_save);
            _aseGen.RecalculateRate();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        /// <summary>Arms the save to the tribulation-eligible peak without full climbing,
        /// matching the TribulationSystemTests.ArmAtPeak pattern.</summary>
        private void ArmAtPeak(int chosenOri = 0)
        {
            _save.currentStage = 5;
            _save.currentPath = 1;
            _save.chosenOri = chosenOri;
            _save.SetAse(new BigNumber(25.0, 6));
            _aseGen.RecalculateRate();
        }

        // ---- Verification 1: steadfastness drives Crossing odds within the configured band ----

        [Test]
        public void FaithfulCrossroadsChoice_RaisesAscendChanceAboveFloor()
        {
            var config = EditModeTestHelpers.MakeTribulationConfig();

            // Before any crossroads: oriTrials=0 → AscendChance = floor (ADR-0004).
            _save.chosenOri = 0;
            Assert.AreEqual(config.ascendFloor, _tribulation.AscendChance, 1e-12,
                "AscendChance starts at the floor before any crossroads (ADR-0004 / no trials)");

            // Fire one crossroads; make the Ori-aligned choice (option 0 = virtueIndex 0).
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0)); // selects card_a
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(0); // Ori-aligned → oriHeld=1, oriTrials=1

            double afterFaithful = _tribulation.AscendChance;
            Assert.Greater(afterFaithful, config.ascendFloor,
                "a faithful crossroads choice must raise AscendChance above the floor");
            Assert.LessOrEqual(afterFaithful, config.ascendCeiling,
                "AscendChance must never exceed the configured ceiling (ADR-0004 / ADR-0005)");
        }

        [Test]
        public void WaveringCrossroadsChoice_KeepsAscendChanceAtFloor()
        {
            var config = EditModeTestHelpers.MakeTribulationConfig();

            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(1); // option 1 = virtueIndex 1 ≠ chosenOri 0 → stray

            // oriHeld=0, oriTrials=1 → steadfastness=0 → rate=0 → AscendChance=floor.
            Assert.AreEqual(config.ascendFloor, _tribulation.AscendChance, 1e-12,
                "a fully-wavering cultivator lands exactly at the configured floor");
            Assert.GreaterOrEqual(_tribulation.AscendChance, config.ascendFloor,
                "even a wavering cultivator retains the floor: a sliver of hope (ADR-0004 §15)");
        }

        // ---- Verifications 2 & 3: Chronicle accretes with correct Titles and Nicknames ----

        [Test]
        public void ThreeGenerationChronicle_AccretesWithTitlesAndNicknames()
        {
            // Generation 1: faithful life → ascend → Title in chronicle.
            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(0); // Ori-aligned → steadfastness = 1/1
            ArmAtPeak(chosenOri: 0);
            // Roll 1 (0.0) < AscendChance (0.90) → ascend; Roll 2 (0.0) → nameIndex 0 → "Adé".
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.0));
            var gen1 = _tribulation.Resolve();

            Assert.IsNotNull(gen1);
            Assert.IsTrue(gen1.DidAscend, "gen 1 must ascend");
            Assert.AreEqual(1, _save.chronicle.Count, "chronicle must have one entry after gen 1");
            Assert.IsTrue(_save.chronicle[0].didAscend);
            StringAssert.Contains("Aṣẹ́gun", _save.chronicle[0].remembrance,
                "Title must contain the Stage-6 honorific (GAMEPLAY §2.2)");
            StringAssert.Contains("Adé", _save.chronicle[0].remembrance,
                "Title must include a personal name from the curated pool");

            // Generation 2: stray life → fall → Nickname from the Defining Deed.
            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(1); // stray → beatIndex=0, Defining Deed card_a
            ArmAtPeak(chosenOri: 0);
            // oriHeld=0, oriTrials=1 → AscendChance=floor=0.25; roll 0.99 → fall.
            _tribulation.SetRandomSource(new FakeRandom(0.99));
            var gen2 = _tribulation.Resolve();

            Assert.IsNotNull(gen2);
            Assert.IsFalse(gen2.DidAscend, "gen 2 must fall");
            Assert.AreEqual(2, _save.chronicle.Count, "chronicle must have two entries after gen 2");
            Assert.IsFalse(_save.chronicle[1].didAscend);
            Assert.AreEqual("The Wavering", _save.chronicle[1].remembrance,
                "Nickname must come from the Defining Deed's fallenEpithet (beats[0])");

            // Generation 3: faithful life → ascend → another Title.
            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(0); // Ori-aligned
            ArmAtPeak(chosenOri: 0);
            // Roll 0.0 → ascend; Roll 0.0 → nameIndex 0 → "Adé".
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.0));
            var gen3 = _tribulation.Resolve();

            Assert.IsNotNull(gen3);
            Assert.IsTrue(gen3.DidAscend, "gen 3 must ascend");
            Assert.AreEqual(3, _save.chronicle.Count, "chronicle must have three entries after gen 3");
            Assert.IsTrue(_save.chronicle[2].didAscend);
            StringAssert.Contains("Aṣẹ́gun", _save.chronicle[2].remembrance,
                "gen 3 Title must still carry the Stage-6 honorific");
        }

        // ---- Verification 4: Light dynasty compounding — forebear crossroads surfaces ----

        [Test]
        public void ForebearCrossroads_SurfacesInDescendantsLife()
        {
            // Generation 1: stray → fall → chronicle records forebearCrossroadsId="card_a".
            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            _crossroads.SetRandomSource(new FakeRandom(0.0)); // draws card_a
            _crossroads.EvaluateMilestone();
            _crossroads.MakeChoice(1); // stray → crossroadsId="card_a", strayed=true, beatIndex=0
            ArmAtPeak(chosenOri: 0);
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // fall
            _tribulation.Resolve();

            Assert.AreEqual("card_a", _save.chronicle[0].forebearCrossroadsId,
                "the Defining Deed's card ID must be stored in the chronicle entry");

            // Generation 2: with forebearSeedChance=1.0 the forebear's card must surface.
            var seedConfig = EditModeTestHelpers.MakeCrossroadsConfigWithSeedChance(
                1.0f, SeedCard());
            EditModeTestHelpers.Inject(_crossroads, "_config", seedConfig);

            _save.chosenOri = 0;
            _save.SetAse(BigNumber.FromDouble(1000));
            // Seed roll: 0.0 < forebearSeedChance 1.0 → passes → card_a returned.
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _crossroads.EvaluateMilestone();

            Assert.IsTrue(_crossroads.HasPending,
                "a crossroads must be pending in the descendant's life");
            Assert.AreEqual("card_a", _save.pendingCrossroadsId,
                "the forebear's crossroads card must surface in the descendant's life " +
                "(light dynasty compounding, PRD Phase 4 / issue #8)");
        }
    }
}
