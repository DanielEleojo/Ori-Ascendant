using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.Audio;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate C capstone: the PRD output metric in test form — two complete
    /// generational loops driven through the real systems (advance the stage
    /// ladder, choose a path, cross, verify generation N+1 starts stronger).
    /// </summary>
    public class FullLoopIntegrationTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
        private AncestralCouncilSystem _council;
        private TribulationSystem _tribulation;
        private FakeRandom _random;
        private CloudSaveManager _cloud;
        private SaveData _save;
        private int _announcements;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("World");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            _cultivation = _host.AddComponent<CultivationSystem>();
            EditModeTestHelpers.InjectArray(_cultivation, "_stages", EditModeTestHelpers.MakeStageTable());
            EditModeTestHelpers.InjectArray(_cultivation, "_paths", EditModeTestHelpers.MakePathTable());
            EditModeTestHelpers.Inject(_cultivation, "_tribulationConfig", EditModeTestHelpers.MakeTribulationConfig());
            _council = _host.AddComponent<AncestralCouncilSystem>();
            EditModeTestHelpers.Inject(_council, "_config", EditModeTestHelpers.MakeCouncilConfig());
            _tribulation = _host.AddComponent<TribulationSystem>();
            EditModeTestHelpers.Inject(_tribulation, "_config", EditModeTestHelpers.MakeTribulationConfig());
            EditModeTestHelpers.Inject(_tribulation, "_gameplayConfig", EditModeTestHelpers.MakeGameplayConfig());

            _cloud = _host.AddComponent<CloudSaveManager>();
            _cloud.Initialize(new FakeCloudProvider { AuthResult = false }); // editor-equivalent: local only

            ServiceLocator.Register(_aseGen);
            ServiceLocator.Register(_cultivation);
            ServiceLocator.Register(_council);
            ServiceLocator.Register(_tribulation);
            ServiceLocator.Register(_cloud);

            _random = new FakeRandom(0.0);
            _tribulation.SetRandomSource(_random);

            _save = new SaveData();
            _cultivation.Begin(_save);
            _council.Begin(_save);
            _tribulation.Begin(_save);
            _aseGen.Begin(_save);
            _aseGen.RecalculateRate();

            _announcements = 0;
            _cultivation.OnTribulationAvailable += () => _announcements++;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        /// <summary>Walks one generation to the armed peak via the real APIs:
        /// thresholds → Advance taps → mandatory path choice → 25M.</summary>
        private void ClimbToArmedPeak(int pathIndex)
        {
            double[] thresholds = { 100, 1500, 5500, 100000, 750000 };
            while (_save.currentStage < 5)
            {
                _save.SetAse(BigNumber.FromDouble(thresholds[_save.currentStage]));
                var outcome = _cultivation.TryAdvance();
                if (outcome == AdvanceOutcome.NeedsPathChoice)
                {
                    Assert.IsTrue(_cultivation.ChoosePath(pathIndex), "path choice must succeed at the gate");
                }
                else
                {
                    Assert.AreEqual(AdvanceOutcome.Advanced, outcome,
                        $"advance failed at stage {_save.currentStage}");
                }
            }
            _save.SetAse(new BigNumber(25.0, 6));
            _aseGen.ChannelTap(); // any Àṣẹ change re-evaluates eligibility
        }

        [Test]
        public void TwoFullGenerations_TheBloodlineGrowsStronger()
        {
            // ---- Generation 1: walk Sango, ascend ----
            ClimbToArmedPeak(pathIndex: 1);
            Assert.AreEqual(1, _announcements, "gen 1 tribulation must announce once");

            _random.Value = 0.0; // ascend
            var first = _tribulation.Resolve();
            Assert.IsNotNull(first);
            Assert.IsTrue(first.DidAscend);

            // Gen 2 baseline: reset state + visible council strength.
            Assert.AreEqual(0, _save.currentStage);
            Assert.AreEqual(-1, _save.currentPath);
            Assert.IsTrue(_save.GetAse().IsZero);
            Assert.AreEqual(1, _save.lineage.generationCount);
            Assert.AreEqual(1, _save.council.Count);
            Assert.AreEqual(BigNumber.FromDouble(1.25), _save.GetAsePerSecond(),
                "gen 2 starts visibly stronger: ×1.25 from one radiant ancestor");

            // ---- Generation 2: walk Osun, fall ----
            ClimbToArmedPeak(pathIndex: 2);
            Assert.AreEqual(2, _announcements, "eligibility must re-announce in gen 2");

            // Mid-walk sanity: Osun doubles the lineage term while walking it.
            // (Stage 5 → ×1250; council 0.25 → factor 1 + 2×0.25 = 1.5.)
            Assert.AreEqual(BigNumber.FromDouble(1250 * 1.5), _save.GetAsePerSecond());

            _random.Value = 0.99; // fall
            var second = _tribulation.Resolve();
            Assert.IsNotNull(second);
            Assert.IsFalse(second.DidAscend);

            // Gen 3 baseline: two ancestors (1.0 + 0.4), no retirement yet.
            Assert.AreEqual(2, _save.lineage.generationCount);
            Assert.AreEqual(2, _save.council.Count);
            Assert.AreEqual(0.0, _save.lineage.permanentAseBonus);
            Assert.AreEqual(BigNumber.FromDouble(1.35), _save.GetAsePerSecond(),
                "gen 3 rate = 1 × (1 + 0.25×1.0 + 0.25×0.4)");
            Assert.AreEqual(1.25, second.LineageFactorBefore, 1e-12);
            Assert.AreEqual(1.35, second.LineageFactorAfter, 1e-12);
        }

        [Test]
        public void CloudSync_FiresOnEachTribulation()
        {
            // The locked business rule: cloud sync on every Tribulation completion.
            // PushRequestCount increments synchronously inside the hook, so this
            // is deterministic regardless of the (inert, auth-failed) provider.
            ClimbToArmedPeak(pathIndex: 1);
            _random.Value = 0.0;
            _tribulation.Resolve();
            Assert.AreEqual(1, _cloud.PushRequestCount, "cloud push must fire on the first crossing");

            ClimbToArmedPeak(pathIndex: 2);
            _random.Value = 0.99;
            _tribulation.Resolve();
            Assert.AreEqual(2, _cloud.PushRequestCount, "and again on the second");
        }

        [Test]
        public void FallenGeneration_IsNeverADeadEnd()
        {
            ClimbToArmedPeak(pathIndex: 0);
            _random.Value = 0.99; // fall on the very first crossing

            var result = _tribulation.Resolve();

            Assert.IsFalse(result.DidAscend);
            Assert.AreEqual(1, _save.council.Count, "the line endures");
            Assert.IsTrue(_save.GetAsePerSecond() > BigNumber.One,
                "gen 2 is strictly stronger even after a fall");
        }
    }
}
