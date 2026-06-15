using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Slice 2a: a crossroads is drawn on each climb advance and held pending; the
    /// choice records a Deed and moves the steadfastness tally ("held N of M") for or
    /// against the vowed Ori. Hosts on a bare GameObject; the ServiceLocator wiring
    /// (CrossroadsSystem ← CultivationSystem.OnStageAdvanced) is exercised for real.
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

        [Test]
        public void StageAdvance_DrawsACrossroads()
        {
            Assert.IsFalse(_crossroads.HasPending);
            _save.SetAse(BigNumber.FromDouble(100));
            _cultivation.TryAdvance(); // 0→1 fires OnStageAdvanced
            Assert.IsTrue(_crossroads.HasPending, "advancing the climb draws a crossroads");
            Assert.IsNotNull(_crossroads.PendingBeat);
        }

        [Test]
        public void ChooseAligned_HoldsTheVow_AndRecordsADeed()
        {
            _crossroads.TryPresentNext();
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
            _crossroads.TryPresentNext();
            Assert.IsTrue(_crossroads.Choose(1)); // oriIndex 1 != vowed Ori 0
            Assert.AreEqual(0, _crossroads.Held, "a strayed choice does not hold the vow");
            Assert.AreEqual(1, _crossroads.Trials);
            Assert.AreEqual(1, _save.deeds.Count);
            Assert.IsFalse(_save.deeds[0].aligned);
        }

        [Test]
        public void EveryBeat_OffersTheVowedOriOption()
        {
            _crossroads.TryPresentNext();
            bool offersVow = false;
            foreach (var option in _crossroads.PendingBeat.options)
            {
                if (option.oriIndex == _save.currentOri) offersVow = true;
            }
            Assert.IsTrue(offersVow, "holding the vow must always be possible — temptation, not a trap");
        }

        [Test]
        public void Draw_IsSequential_OnePendingAtATime()
        {
            _crossroads.TryPresentNext();
            int first = _save.pendingCrossroads;
            _crossroads.TryPresentNext(); // already pending → no change
            Assert.AreEqual(first, _save.pendingCrossroads);

            _crossroads.Choose(0);
            _crossroads.TryPresentNext();
            Assert.AreEqual(first + 1, _save.pendingCrossroads, "the deck is drawn in order");
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
