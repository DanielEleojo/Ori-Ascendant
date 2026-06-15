using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    internal sealed class FakeRandom : IRandomSource
    {
        public double Value;
        public FakeRandom(double value) => Value = value;
        public double NextDouble() => Value;
    }

    /// <summary>
    /// Gate C: roll-once-persist-first. The COMPLETE §4.4 generation reset must
    /// be observable in SaveData the moment Resolve() returns — the ceremony is
    /// replayable theater, never a state machine.
    /// </summary>
    public class TribulationSystemTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
        private AncestralCouncilSystem _council;
        private TribulationSystem _tribulation;
        private SaveData _save;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("TestHost");

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

            // EditMode: Awake doesn't run — register manually (see memory note).
            ServiceLocator.Register(_aseGen);
            ServiceLocator.Register(_cultivation);
            ServiceLocator.Register(_council);
            ServiceLocator.Register(_tribulation);

            _save = new SaveData();
            _cultivation.Begin(_save);
            _council.Begin(_save);
            _tribulation.Begin(_save);
            _aseGen.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        private void ArmAtPeak(int path = 1, double ase = 25_000_000)
        {
            _save.currentStage = 5;
            _save.currentPath = path;
            _save.generationStartTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
            _save.SetAse(BigNumber.FromDouble(ase));
            _aseGen.RecalculateRate();
        }

        [Test]
        public void Resolve_RefusedBelowPeak_AndBelowThreshold()
        {
            _save.currentStage = 3;
            _save.SetAse(new BigNumber(54.0, 6));
            Assert.IsNull(_tribulation.Resolve(), "mid-climb must never resolve");

            _save.currentStage = 5;
            _save.SetAse(BigNumber.FromDouble(24_999_000));
            Assert.IsNull(_tribulation.Resolve(), "below the 25M gate must never resolve");
        }

        [Test]
        public void Resolve_Ascend_WritesTheCompleteGenerationReset()
        {
            ArmAtPeak(path: 1);
            _save.currentOri = 2; // a virtue vowed this life — must reset to -1
            _save.oriHeld = 3;
            _save.oriTrials = 5;
            _save.pendingCrossroads = 1;
            _save.deeds.Add(new DeedData { crossroadsIndex = 0, chosenOri = 1, stage = 4, aligned = false });
            _tribulation.SetRandomSource(new FakeRandom(0.0)); // < 0.60 → ascend
            long before = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var result = _tribulation.Resolve();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.DidAscend);

            // GAMEPLAY §4.4 — every field, observable immediately:
            Assert.IsTrue(_save.GetAse().IsZero, "aseAmount → 0");
            Assert.AreEqual(0, _save.currentStage, "currentStage → 0");
            Assert.AreEqual(-1, _save.currentPath, "currentPath → -1 (re-chosen each generation)");
            Assert.AreEqual(-1, _save.currentOri, "currentOri → -1 (re-vowed each generation)");
            Assert.AreEqual(0, _save.oriHeld, "steadfastness tally → 0");
            Assert.AreEqual(0, _save.oriTrials);
            Assert.AreEqual(-1, _save.pendingCrossroads, "pending crossroads cleared");
            Assert.IsEmpty(_save.deeds, "deeds cleared for the new life");
            Assert.GreaterOrEqual(_save.generationStartTimestamp, before, "generationStartTimestamp → now");
            Assert.AreEqual(1, _save.lineage.generationCount, "generationCount++");
            Assert.AreEqual(1, _save.council.Count, "ancestor appended");

            var ancestor = _save.council[0];
            Assert.AreEqual(5, ancestor.peakStage);
            Assert.AreEqual(1, ancestor.path, "path captured BEFORE the reset");
            Assert.IsTrue(ancestor.didAscend);
            Assert.AreEqual(1.0, ancestor.bonusMultiplier);
            Assert.GreaterOrEqual(ancestor.completedTimestamp, before);

            // Cached rate already holds gen 2's stage-1 reality: 1 × (1 + 0.25).
            Assert.AreEqual(BigNumber.FromDouble(1.25), _save.GetAsePerSecond());
        }

        [Test]
        public void Resolve_Fall_StillProducesAnAncestor_At0Point4()
        {
            ArmAtPeak(path: 2);
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // ≥ 0.60 → fall

            var result = _tribulation.Resolve();

            Assert.IsNotNull(result);
            Assert.IsFalse(result.DidAscend);
            Assert.AreEqual(1, _save.council.Count, "a fallen cultivator still produces an ancestor");
            Assert.AreEqual(0.4, _save.council[0].bonusMultiplier, "locked 0.4 fall multiplier");
            Assert.AreEqual(BigNumber.FromDouble(1.10), _save.GetAsePerSecond(),
                "gen 2 rate = 1 × (1 + 0.25 × 0.4)");
        }

        [Test]
        public void Resolve_Boundary_ExactlyPointSixFalls()
        {
            ArmAtPeak();
            _tribulation.SetRandomSource(new FakeRandom(0.60)); // roll < chance ascends; 0.60 is NOT < 0.60

            var result = _tribulation.Resolve();
            Assert.IsFalse(result.DidAscend);
        }

        [Test]
        public void Result_CarriesTheCeremonyFacts()
        {
            ArmAtPeak(path: 0);
            _tribulation.SetRandomSource(new FakeRandom(0.99));

            var result = _tribulation.Resolve();

            Assert.AreEqual(1, result.CompletedGenerationNumber);
            Assert.AreEqual(0, result.PathIndexAtCrossing);
            Assert.AreEqual(new BigNumber(25.0, 6), result.PeakAse);
            Assert.GreaterOrEqual(result.TimeInGenerationSeconds, 3600);
            Assert.AreEqual(1.0, result.LineageFactorBefore, 1e-12);
            Assert.AreEqual(1.10, result.LineageFactorAfter, 1e-12);
            Assert.AreEqual(BigNumber.One, result.OldStage1Rate);
            Assert.AreEqual(BigNumber.FromDouble(1.10), result.NewStage1Rate);
        }

        [Test]
        public void Resolve_FiresLockedEvent_AfterStateIsWritten()
        {
            ArmAtPeak();
            _tribulation.SetRandomSource(new FakeRandom(0.0));

            bool? eventAscend = null;
            AncestorData eventAncestor = null;
            int saveStageAtEvent = -99;
            _tribulation.OnTribulationComplete += (ascended, ancestor) =>
            {
                eventAscend = ascended;
                eventAncestor = ancestor;
                saveStageAtEvent = _save.currentStage; // state must already be reset
            };

            _tribulation.Resolve();

            Assert.IsTrue(eventAscend.HasValue && eventAscend.Value);
            Assert.IsNotNull(eventAncestor);
            Assert.AreEqual(0, saveStageAtEvent, "event is notification-only — fired AFTER the atomic write");
        }
    }
}
