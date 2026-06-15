using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Slices 2a/2b: crossroads are drawn at Àṣẹ milestones (the advance thresholds,
    /// so they accrue from banked Àṣẹ — including offline — not the manual Advance
    /// tap), held in a patient queue that never expires, and resolved front-first;
    /// each choice records a Deed and moves the steadfastness tally for or against the
    /// vowed Ori. Hosts on a bare GameObject; the ServiceLocator wiring is exercised
    /// for real. Milestone thresholds (MakeStageTable): 100, 1500, 5500, 100000,
    /// 750000; the seed deck has 3 beats (c0, c1, c2).
    /// </summary>
    public class CrossroadsSystemTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
        private CrossroadsSystem _crossroads;
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
            EditModeTestHelpers.InjectArray(_cultivation, "_oris", EditModeTestHelpers.MakeOriTable());
            EditModeTestHelpers.Inject(_cultivation, "_tribulationConfig", EditModeTestHelpers.MakeTribulationConfig());

            _crossroads = _host.AddComponent<CrossroadsSystem>();
            EditModeTestHelpers.Inject(_crossroads, "_deck", EditModeTestHelpers.MakeCrossroadsDeck());
            EditModeTestHelpers.InjectArray(_crossroads, "_stages", EditModeTestHelpers.MakeStageTable());

            ServiceLocator.Register(_aseGen);
            ServiceLocator.Register(_cultivation);
            ServiceLocator.Register(_crossroads);

            _save = new SaveData { currentOri = 0 }; // vowed the Path of Mercy (index 0)
            _cultivation.Begin(_save);
            _aseGen.Begin(_save);
            _crossroads.Begin(_save);
            _aseGen.RecalculateRate();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        private void BankAse(double amount) => _save.SetAse(BigNumber.FromDouble(amount));

        [Test]
        public void CrossingAMilestone_DrawsACrossroads()
        {
            Assert.IsFalse(_crossroads.HasPending, "nothing pending before any Àṣẹ is banked");
            BankAse(100);
            _crossroads.CheckMilestones();
            Assert.IsTrue(_crossroads.HasPending, "crossing the first Àṣẹ milestone draws a crossroads");
            Assert.IsNotNull(_crossroads.PendingBeat);
            Assert.AreEqual(1, _crossroads.PendingCount);
        }

        [Test]
        public void MultipleMilestonesWhileAway_QueueAll()
        {
            BankAse(5500); // one return crosses milestones 100, 1500, 5500 at once
            _crossroads.CheckMilestones();
            Assert.AreEqual(3, _crossroads.PendingCount,
                "every milestone crossed while away is waiting on return");
        }

        [Test]
        public void ResolvingTheQueue_AdvancesInOrder()
        {
            BankAse(5500);
            _crossroads.CheckMilestones();
            Assert.AreEqual("c0", _crossroads.PendingBeat.id);
            Assert.IsTrue(_crossroads.Choose(0));
            Assert.AreEqual("c1", _crossroads.PendingBeat.id, "the queue resolves front-first");
            Assert.IsTrue(_crossroads.Choose(0));
            Assert.AreEqual("c2", _crossroads.PendingBeat.id);
            Assert.IsTrue(_crossroads.Choose(0));
            Assert.IsFalse(_crossroads.HasPending, "queue drained in order");
        }

        [Test]
        public void PendingCrossroads_NeverExpires()
        {
            BankAse(100);
            _crossroads.CheckMilestones();
            Assert.IsTrue(_crossroads.HasPending);

            // Time passes — more Àṣẹ ticks, more milestone checks. Nothing but a choice
            // (or a new generation) removes a pending crossroads: there is no timer.
            for (int i = 0; i < 5; i++) _crossroads.CheckMilestones();
            Assert.IsTrue(_crossroads.HasPending, "a pending crossroads waits patiently — no expiry");
        }

        [Test]
        public void DeckExhaustion_CapsTheQueue()
        {
            BankAse(1_000_000_000); // past every milestone (max 750000)
            _crossroads.CheckMilestones();
            Assert.AreEqual(3, _crossroads.PendingCount, "draws cap at the deck length (3 seed beats)");
        }

        [Test]
        public void ChooseAligned_HoldsTheVow_AndRecordsADeed()
        {
            BankAse(100);
            _crossroads.CheckMilestones();
            Assert.IsTrue(_crossroads.Choose(0)); // option oriIndex 0 == vowed Ori
            Assert.AreEqual(1, _crossroads.Held);
            Assert.AreEqual(1, _crossroads.Trials);
            Assert.IsFalse(_crossroads.HasPending);
            Assert.AreEqual(1, _save.deeds.Count);
            Assert.IsTrue(_save.deeds[0].aligned);
        }

        [Test]
        public void ChooseOther_IsAStrayingTrial()
        {
            BankAse(100);
            _crossroads.CheckMilestones();
            Assert.IsTrue(_crossroads.Choose(1)); // oriIndex 1 != vowed Ori 0
            Assert.AreEqual(0, _crossroads.Held, "a strayed choice does not hold the vow");
            Assert.AreEqual(1, _crossroads.Trials);
            Assert.AreEqual(1, _save.deeds.Count);
            Assert.IsFalse(_save.deeds[0].aligned);
        }

        [Test]
        public void EveryBeat_OffersTheVowedOriOption()
        {
            BankAse(100);
            _crossroads.CheckMilestones();
            bool offersVow = false;
            foreach (var option in _crossroads.PendingBeat.options)
            {
                if (option.oriIndex == _save.currentOri) offersVow = true;
            }
            Assert.IsTrue(offersVow, "holding the vow must always be possible — temptation, not a trap");
        }

        [Test]
        public void Choose_RefusedWhenNothingPending()
        {
            Assert.IsFalse(_crossroads.HasPending);
            Assert.IsFalse(_crossroads.Choose(0));
            Assert.AreEqual(0, _crossroads.Trials);
        }
    }
}
