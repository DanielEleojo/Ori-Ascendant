using System.Collections.Generic;
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

        private void ArmAtPeak(int path = 1, double ase = 25_000_000, int chosenOri = 0)
        {
            _save.currentStage = 5;
            _save.currentPath = path;
            _save.chosenOri = chosenOri;
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
            _tribulation.SetRandomSource(new FakeRandom(0.0)); // 0.0 < any derived chance → ascend
            long before = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var result = _tribulation.Resolve();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.DidAscend);

            // GAMEPLAY §4.4 — every field, observable immediately:
            Assert.IsTrue(_save.GetAse().IsZero, "aseAmount → 0");
            Assert.AreEqual(0, _save.currentStage, "currentStage → 0");
            Assert.AreEqual(-1, _save.currentPath, "currentPath → -1 (re-chosen each generation)");
            Assert.AreEqual(-1, _save.chosenOri, "chosenOri → -1 (Àkùnlẹ̀yàn is re-vowed every generation)");
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

        [Test]
        public void Resolve_ResetsSteadfastnessAndCrossroadsState()
        {
            ArmAtPeak();
            // Simulate a life that held its Ori at a crossroads
            _save.oriHeld = 3;
            _save.oriTrials = 4;
            _save.pendingCrossroadsId = "card_a";
            _save.pendingCrossroadsQueue = new System.Collections.Generic.List<string> { "card_b" };
            _save.deeds = new System.Collections.Generic.List<DeedData>
            {
                new DeedData
                {
                    beatIndex = 0,
                    strayed = false,
                }
            };
            _tribulation.SetRandomSource(new FakeRandom(0.0));

            _tribulation.Resolve();

            Assert.AreEqual(0, _save.oriHeld,   "oriHeld resets to 0 for the new life");
            Assert.AreEqual(0, _save.oriTrials,  "oriTrials resets to 0 for the new life");
            Assert.AreEqual("", _save.pendingCrossroadsId, "pending crossroads clears at the Crossing");
            Assert.AreEqual(0, _save.pendingCrossroadsQueue.Count, "crossroads queue also clears at the Crossing");
            Assert.IsNotNull(_save.deeds);
            Assert.AreEqual(0, _save.deeds.Count, "deeds list cleared at the Crossing");
        }

        // ---- Remembrance (slice 4a) ----

        private void InjectRemembranceConfigs()
        {
            EditModeTestHelpers.Inject(_tribulation, "_remembranceConfig",
                EditModeTestHelpers.MakeRemembranceConfig());
            EditModeTestHelpers.Inject(_tribulation, "_crossroadsDeck",
                EditModeTestHelpers.MakeCrossroadsDeckConfig());
        }

        [Test]
        public void Resolve_Ascend_WritesTitle_AsRemembrance()
        {
            // Stage 5 = "Aṣẹ́gun" (GAMEPLAY §2.2). nameRoll=0.0 → index 0 → "Adé".
            InjectRemembranceConfigs();
            ArmAtPeak(); // currentStage=5 → honorific="Aṣẹ́gun"
            // Roll 1 (0.0) < floor (0.25) → ascend; Roll 2 (0.0) → nameIndex 0.
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.0));

            _tribulation.Resolve();

            Assert.AreEqual("Aṣẹ́gun Adé", _save.council[0].remembrance,
                "ascend → Title: honorific + personal name at index 0");
        }

        [Test]
        public void Resolve_Ascend_NameIndex_UsesSecondRoll()
        {
            // nameRoll=0.5 with 2-name pool → (int)(0.5 * 2) = 1 → "Bàbá".
            InjectRemembranceConfigs();
            ArmAtPeak();
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.5));

            _tribulation.Resolve();

            Assert.AreEqual("Aṣẹ́gun Bàbá", _save.council[0].remembrance,
                "second random draw selects the personal name by index");
        }

        [Test]
        public void Resolve_Fall_FirstStray_IsDefiningDeed()
        {
            // Two strays; FIRST stray (beatIndex 0) is the Defining Deed.
            InjectRemembranceConfigs();
            ArmAtPeak();
            _save.deeds.Add(new DeedData { beatIndex = 0, strayed = true });  // Defining Deed
            _save.deeds.Add(new DeedData { beatIndex = 1, strayed = true });  // not selected
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // 0.99 ≥ floor → fall

            _tribulation.Resolve();

            Assert.AreEqual("The Wavering", _save.council[0].remembrance,
                "fall Nickname comes from the FIRST strayed deed's fallenEpithet");
        }

        [Test]
        public void Resolve_Fall_SkipsNonStrayedDeeds_ThenUsesFirstStray()
        {
            // First deed is faithful; second is the Defining Deed.
            InjectRemembranceConfigs();
            ArmAtPeak();
            _save.deeds.Add(new DeedData { beatIndex = 2, strayed = false }); // faithful, skip
            _save.deeds.Add(new DeedData { beatIndex = 1, strayed = true });  // Defining Deed
            _save.deeds.Add(new DeedData { beatIndex = 0, strayed = true });  // after first — ignored
            _tribulation.SetRandomSource(new FakeRandom(0.99));

            _tribulation.Resolve();

            Assert.AreEqual("The Divided", _save.council[0].remembrance,
                "Defining Deed is the first STRAYED deed, not the first deed overall");
        }

        [Test]
        public void Resolve_Fall_NoStrays_UsesFaithfulFallLine()
        {
            InjectRemembranceConfigs();
            ArmAtPeak();
            _save.deeds.Add(new DeedData { beatIndex = 0, strayed = false }); // held true
            _tribulation.SetRandomSource(new FakeRandom(0.99));

            _tribulation.Resolve();

            Assert.AreEqual("The Faithful", _save.council[0].remembrance,
                "a life that held its vow throughout gets the shared faithful-fall line");
        }

        [Test]
        public void Resolve_Fall_EmptyDeeds_UsesFaithfulFallLine()
        {
            // No Crossroads system yet — deeds is always empty in isolation.
            InjectRemembranceConfigs();
            ArmAtPeak();
            // _save.deeds is empty by default
            _tribulation.SetRandomSource(new FakeRandom(0.99));

            _tribulation.Resolve();

            Assert.AreEqual("The Faithful", _save.council[0].remembrance,
                "empty deeds (no Crossroads yet) falls through to the faithful-fall line");
        }

        [Test]
        public void Resolve_Remembrance_IgnoresDeityPath()
        {
            InjectRemembranceConfigs();

            // Run 1: Ane path, one stray deed.
            ArmAtPeak(path: 0);
            _save.deeds.Add(new DeedData { beatIndex = 1, strayed = true });
            _tribulation.SetRandomSource(new FakeRandom(0.99));
            _tribulation.Resolve();
            string earthRemembrance = _save.council[0].remembrance;

            // Run 2: Osun path, same stray deed (deeds were cleared by Run 1's reset).
            ArmAtPeak(path: 2);
            _save.deeds.Add(new DeedData { beatIndex = 1, strayed = true });
            _tribulation.SetRandomSource(new FakeRandom(0.99));
            _tribulation.Resolve();
            string riverRemembrance = _save.council[1].remembrance;

            Assert.AreEqual(earthRemembrance, riverRemembrance,
                "deity-Path never affects remembrance (ADR-0004)");
            Assert.AreEqual("The Divided", earthRemembrance);
        }

        [Test]
        public void Resolve_DeedsAreCleared_AfterGenerationReset()
        {
            InjectRemembranceConfigs();
            ArmAtPeak();
            _save.deeds.Add(new DeedData { beatIndex = 0, strayed = true });
            _tribulation.SetRandomSource(new FakeRandom(0.99));

            _tribulation.Resolve();

            Assert.IsEmpty(_save.deeds,
                "deeds are per-life state — cleared alongside stage/path/ori at reset");
        }

        [Test]
        public void Remembrance_Derive_AscendTitle_DirectCalculator()
        {
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deck = EditModeTestHelpers.MakeCrossroadsDeckConfig();
            var deeds = new List<DeedData>();

            string r = Remembrance.Derive(true, "Aṣẹ́gun", deeds, deck, config, nameIndex: 1);

            Assert.AreEqual("Aṣẹ́gun Bàbá", r, "ascend → honorific + pool[nameIndex]");
        }

        [Test]
        public void Remembrance_Derive_FallNickname_DirectCalculator()
        {
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deck = EditModeTestHelpers.MakeCrossroadsDeckConfig();
            var deeds = new List<DeedData>
            {
                new DeedData { beatIndex = 2, strayed = false },
                new DeedData { beatIndex = 0, strayed = true },  // Defining Deed
            };

            string r = Remembrance.Derive(false, "Aṣẹ́gun", deeds, deck, config, nameIndex: 0);

            Assert.AreEqual("The Wavering", r, "fall → fallenEpithet of first strayed deed");
        }

        [Test]
        public void Remembrance_Derive_PathIndependence_IsStructural()
        {
            // Remembrance.Derive takes no path parameter — path-independence is structural.
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deck = EditModeTestHelpers.MakeCrossroadsDeckConfig();
            var deeds = new List<DeedData>
            {
                new DeedData { beatIndex = 1, strayed = true },
            };

            // Calling with the same args always gives the same result — no path to vary.
            string r1 = Remembrance.Derive(false, "Aṣẹ́gun", deeds, deck, config, 0);
            string r2 = Remembrance.Derive(false, "Aṣẹ́gun", deeds, deck, config, 0);
            Assert.AreEqual(r1, r2, "no path parameter → structurally path-independent");
            Assert.AreEqual("The Divided", r1);
        }

        // ---- Chronicle (issue #7) ----

        [Test]
        public void Resolve_AppendsChronicleEntry_EachGeneration()
        {
            InjectRemembranceConfigs();
            ArmAtPeak(path: 1, chosenOri: 0);
            _tribulation.SetRandomSource(new FakeRandom(0.0, 0.0)); // ascend; nameIndex 0

            _tribulation.Resolve();

            Assert.AreEqual(1, _save.chronicle.Count, "one chronicle entry per Crossing");
            var entry = _save.chronicle[0];
            Assert.AreEqual(1, entry.generationNumber, "generation 1 is the first completed life");
            Assert.AreEqual(0, entry.chosenOri, "chosenOri captured before the reset");
            Assert.IsTrue(entry.didAscend);
            Assert.IsNotNull(entry.remembrance, "remembrance is carried into the chronicle");
        }

        [Test]
        public void Chronicle_AccretesAcrossGenerations_PastCouncilCap()
        {
            InjectRemembranceConfigs();

            // Run 6 generations — one more than the Council cap (5).
            for (int i = 0; i < 6; i++)
            {
                ArmAtPeak(path: 1, chosenOri: 0);
                _tribulation.SetRandomSource(new FakeRandom(0.99)); // fall every time
                _tribulation.Resolve();
            }

            // Chronicle is unbounded.
            Assert.AreEqual(6, _save.chronicle.Count,
                "chronicle records every generation, including the 6th that retired an ancestor");
            // Council is capped at 5 (MaxCouncil).
            Assert.AreEqual(5, _save.council.Count,
                "council still caps at 5 — unaffected by the chronicle");

            // Generation numbers must be sequential.
            for (int i = 0; i < 6; i++)
                Assert.AreEqual(i + 1, _save.chronicle[i].generationNumber);
        }

        [Test]
        public void Chronicle_CouncilBehaviourUnchanged_WhenFull()
        {
            InjectRemembranceConfigs();
            // Fill the council (5 gens) + push one more to trigger retirement.
            for (int i = 0; i < 6; i++)
            {
                ArmAtPeak(path: 1, chosenOri: 0);
                _tribulation.SetRandomSource(new FakeRandom(0.0, 0.0));
                _tribulation.Resolve();
            }

            // The 6th generation retired the oldest council member — permanentAseBonus
            // absorbed it (Àṣẹ-neutral rule). Council stays at 5.
            Assert.AreEqual(5, _save.council.Count);
            // permanentAseBonus > 0 proves a retirement happened.
            Assert.Greater(_save.lineage.permanentAseBonus, 0.0,
                "council retirement baked the retired ancestor into permanentAseBonus");
        }

        [Test]
        public void Chronicle_Fall_RecordsCorrectOutcome()
        {
            InjectRemembranceConfigs();
            ArmAtPeak(path: 2, chosenOri: 1);
            _save.deeds.Add(new DeedData { beatIndex = 1, strayed = true }); // Defining Deed
            _tribulation.SetRandomSource(new FakeRandom(0.99)); // fall

            _tribulation.Resolve();

            var entry = _save.chronicle[0];
            Assert.IsFalse(entry.didAscend);
            Assert.AreEqual(1, entry.chosenOri);
            Assert.AreEqual("The Divided", entry.remembrance,
                "fall Nickname from the first strayed deed carries into the chronicle");
        }
    }
}
