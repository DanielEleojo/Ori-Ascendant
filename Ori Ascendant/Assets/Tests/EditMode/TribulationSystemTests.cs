using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>Deterministic random for tests: returns the supplied values in order,
    /// repeating the last once exhausted. A single value behaves as a 1-element
    /// sequence, and the settable Value resets it to one fixed value — so both the
    /// constructor and the legacy `.Value =` call sites work unchanged.</summary>
    internal sealed class FakeRandom : IRandomSource
    {
        private double[] _values;
        private int _index;

        public FakeRandom(params double[] values) =>
            _values = values != null && values.Length > 0 ? values : new[] { 0.0 };

        /// <summary>Back-compat single-value control: resets the source to one fixed value.</summary>
        public double Value
        {
            get => _values[System.Math.Min(_index, _values.Length - 1)];
            set { _values = new[] { value }; _index = 0; }
        }

        public double NextDouble()
        {
            double v = _values[System.Math.Min(_index, _values.Length - 1)];
            _index++;
            return v;
        }
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
        private RemembranceConfig _remembrance;
        private CrossroadsDeckConfig _deck;
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
            _remembrance = EditModeTestHelpers.MakeRemembranceConfig();
            _deck = EditModeTestHelpers.MakeCrossroadsDeck();
            EditModeTestHelpers.Inject(_tribulation, "_remembranceConfig", _remembrance);
            EditModeTestHelpers.Inject(_tribulation, "_deck", _deck);

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
            _tribulation.SetRandomSource(new FakeRandom(0.0)); // 0.0 < any derived chance → ascend
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
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // 0/0 trials → floor 0.25; 0.99 ≥ 0.25 → fall

            var result = _tribulation.Resolve();

            Assert.IsNotNull(result);
            Assert.IsFalse(result.DidAscend);
            Assert.AreEqual(1, _save.council.Count, "a fallen cultivator still produces an ancestor");
            Assert.AreEqual(0.4, _save.council[0].bonusMultiplier, "locked 0.4 fall multiplier");
            Assert.AreEqual(BigNumber.FromDouble(1.10), _save.GetAsePerSecond(),
                "gen 2 rate = 1 × (1 + 0.25 × 0.4)");
        }

        [Test]
        public void AscendChance_IsLinearAcrossTheBand()
        {
            _save.oriTrials = 5;
            _save.oriHeld = 0;
            Assert.AreEqual(0.25, _tribulation.AscendChance, 1e-12, "full waver → floor");
            _save.oriHeld = 5;
            Assert.AreEqual(0.90, _tribulation.AscendChance, 1e-12, "full faith → ceiling");
            _save.oriHeld = 3;
            Assert.AreEqual(0.64, _tribulation.AscendChance, 1e-12, "3/5 → floor + (ceiling − floor) × 0.6");
        }

        [Test]
        public void AscendChance_ZeroTrials_IsFloor()
        {
            _save.oriTrials = 0;
            _save.oriHeld = 0;
            Assert.AreEqual(0.25, _tribulation.AscendChance, 1e-12,
                "a life that faced no resolved crossroads earns only the floor");
        }

        [Test]
        public void AscendChance_IgnoresDeityPath()
        {
            _save.oriHeld = 3;
            _save.oriTrials = 5;
            _save.currentPath = 0; // Ane
            double earth = _tribulation.AscendChance;
            _save.currentPath = 2; // Osun
            double river = _tribulation.AscendChance;
            Assert.AreEqual(earth, river, 1e-12, "deity-Path never touches Crossing odds (ADR-0004)");
            Assert.AreEqual(0.64, earth, 1e-12);
        }

        [Test]
        public void Resolve_RollUnderDerivedChance_Ascends()
        {
            ArmAtPeak();
            _save.oriHeld = 5; // steadfast → ceiling 0.90
            _save.oriTrials = 5;
            _tribulation.SetRandomSource(new FakeRandom(0.89));
            Assert.IsTrue(_tribulation.Resolve().DidAscend, "a roll under the derived chance ascends");
        }

        [Test]
        public void Resolve_RollAtDerivedChance_Falls()
        {
            ArmAtPeak();
            _save.oriHeld = 5; // steadfast → ceiling 0.90
            _save.oriTrials = 5;
            _tribulation.SetRandomSource(new FakeRandom(0.90));
            Assert.IsFalse(_tribulation.Resolve().DidAscend,
                "even the steadfast can fall — roll == chance is not < chance");
        }

        [Test]
        public void SequenceRandom_ReturnsValuesInOrder_ThenRepeatsLast()
        {
            var seq = new FakeRandom(0.1, 0.9);
            Assert.AreEqual(0.1, seq.NextDouble());
            Assert.AreEqual(0.9, seq.NextDouble());
            Assert.AreEqual(0.9, seq.NextDouble(), "an exhausted sequence repeats its last value");
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

        // ---- Remembrance (slice 4a, #6): a Title on ascend, a Nickname on fall ----

        [Test]
        public void Resolve_Ascend_RemembersByTitle_HonorificPlusPooledName()
        {
            ArmAtPeak();
            // The first roll ascends (0.0 < floor 0.25); the SECOND roll picks the name.
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.5));

            var result = _tribulation.Resolve();

            Assert.IsTrue(result.DidAscend);
            string honorific = _cultivation.PeekStageName(5); // the peak stage borne as an honorific
            int nameIndex = (int)(0.5 * _remembrance.personalNames.Length);
            Assert.AreEqual($"{honorific} {_remembrance.personalNames[nameIndex]}", result.Ancestor.remembrance,
                "an ascended cultivator is remembered by the peak-stage honorific + a pooled personal name");
        }

        [Test]
        public void Resolve_Fall_RemembersByNickname_OfTheFirstStray()
        {
            ArmAtPeak(path: 2);
            _save.deeds.Add(new DeedData { crossroadsIndex = 2, chosenOri = 2, stage = 1, aligned = true });  // held the vow
            _save.deeds.Add(new DeedData { crossroadsIndex = 0, chosenOri = 1, stage = 2, aligned = false }); // FIRST stray → the Defining Deed
            _save.deeds.Add(new DeedData { crossroadsIndex = 1, chosenOri = 3, stage = 3, aligned = false }); // a later stray, must be ignored
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // fall — a fall draws no second roll

            var result = _tribulation.Resolve();

            Assert.IsFalse(result.DidAscend);
            Assert.AreEqual(_deck.beats[0].fallenEpithet, result.Ancestor.remembrance,
                "the Nickname is the Defining Deed — the FIRST strayed choice (beat 0), never a later one");
        }

        [Test]
        public void Resolve_FaithfulFall_RemembersByTheSharedDignifiedLine()
        {
            ArmAtPeak();
            _save.deeds.Add(new DeedData { crossroadsIndex = 0, chosenOri = 0, stage = 1, aligned = true });
            _save.deeds.Add(new DeedData { crossroadsIndex = 1, chosenOri = 0, stage = 2, aligned = true });
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // held every vow, yet still fell

            var result = _tribulation.Resolve();

            Assert.IsFalse(result.DidAscend);
            Assert.AreEqual(_remembrance.faithfulFallLine, result.Ancestor.remembrance,
                "a life that never strayed yet fell shares one dignified line, not a stray epithet");
        }

        [Test]
        public void Remembrance_IgnoresDeityPath()
        {
            var strayedAtFord = new DeedData { crossroadsIndex = 0, chosenOri = 1, stage = 2, aligned = false };
            string earth = FallNicknameForPath(0, strayedAtFord); // Ane
            string river = FallNicknameForPath(2, strayedAtFord); // Osun

            Assert.AreEqual(earth, river, "deity-Path never touches how a life is remembered (ADR-0004)");
            Assert.AreEqual(_deck.beats[0].fallenEpithet, earth, "and the Nickname is still the first stray's epithet");
        }

        /// <summary>Resolve a fresh fallen life on the given path with one strayed deed and
        /// return the Nickname it earned — for asserting the deity-Path has no bearing.</summary>
        private string FallNicknameForPath(int path, DeedData deed)
        {
            var save = new SaveData();
            _cultivation.Begin(save);
            _council.Begin(save);
            _tribulation.Begin(save);
            _aseGen.Begin(save);
            save.currentStage = 5;
            save.currentPath = path;
            save.generationStartTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 3600;
            save.SetAse(BigNumber.FromDouble(25_000_000));
            save.deeds.Add(new DeedData
            {
                crossroadsIndex = deed.crossroadsIndex,
                chosenOri = deed.chosenOri,
                stage = deed.stage,
                aligned = deed.aligned,
            });
            _aseGen.RecalculateRate();
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // fall (floor 0.25; 0.99 ≥ 0.25)
            return _tribulation.Resolve().Ancestor.remembrance;
        }
    }
}
