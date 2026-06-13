using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate B: host-level flows — the path gate, multi-advance, once-only
    /// eligibility, rate recompute on advance/path-choice (Sango ×2, Osun wrap,
    /// path-less neutrality) and the channel grant. Hosts live on a bare
    /// GameObject; AddComponent fires Awake immediately in EditMode, so the
    /// ServiceLocator wiring is exercised for real.
    /// </summary>
    public class CultivationSystemTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
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

            // AddComponent does NOT invoke Awake() in EditMode (no ExecuteAlways),
            // so mirror what Awake does at runtime: register with the locator.
            ServiceLocator.Register(_aseGen);
            ServiceLocator.Register(_cultivation);

            _save = new SaveData();
            _cultivation.Begin(_save);
            _aseGen.Begin(_save);
            _aseGen.RecalculateRate();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        private void SetAse(double value) => _save.SetAse(BigNumber.FromDouble(value));

        [Test]
        public void Diagnostics_WiringChain()
        {
            Assert.IsTrue(ServiceLocator.TryGet(out AseGenerationSystem foundGen), "LINK 1a: AseGen not in locator");
            Assert.AreSame(_aseGen, foundGen, "LINK 1b: locator holds a different AseGen instance");
            Assert.IsTrue(ServiceLocator.TryGet(out CultivationSystem foundCult), "LINK 1c: Cultivation not in locator");
            Assert.AreSame(_cultivation, foundCult, "LINK 1d: locator holds a different Cultivation instance");

            _save.currentStage = 3;
            Assert.IsNotNull(_cultivation.CurrentStageConfig, "LINK 2a: CurrentStageConfig null at stage 3");
            Assert.AreEqual(80.0, _cultivation.StageProductionMultiplier, 1e-9, "LINK 2b: stage accessor wrong");

            bool fired = false;
            _aseGen.OnAseChanged += _ => fired = true;
            _aseGen.ChannelTap();
            Assert.IsTrue(fired, "LINK 3: OnAseChanged did not fire on ChannelTap");

            _aseGen.RecalculateRate();
            Assert.AreEqual(BigNumber.FromDouble(80.0), _save.GetAsePerSecond(),
                "LINK 4: recalc ignored stage multiplier (locator lookup inside RecalculateRate failed?)");
        }

        // ---- advancement ----

        [Test]
        public void Advance_BelowThreshold_Refuses()
        {
            SetAse(99);
            Assert.AreEqual(AdvanceOutcome.ThresholdNotMet, _cultivation.TryAdvance());
            Assert.AreEqual(0, _save.currentStage);
        }

        [Test]
        public void Advance_AtThreshold_AdvancesOneStage_AndAseIsNeverSpent()
        {
            SetAse(100);
            Assert.AreEqual(AdvanceOutcome.Advanced, _cultivation.TryAdvance());
            Assert.AreEqual(1, _save.currentStage);
            Assert.AreEqual(BigNumber.FromDouble(100), _save.GetAse(), "advancement must never spend Àṣẹ");
        }

        [Test]
        public void MultiAdvance_OneStagePerTap_UntilPathGate()
        {
            SetAse(10000); // banked overnight: clears 100, 1500, then hits the gate at 5500

            Assert.AreEqual(AdvanceOutcome.Advanced, _cultivation.TryAdvance());
            Assert.AreEqual(AdvanceOutcome.Advanced, _cultivation.TryAdvance());
            Assert.AreEqual(AdvanceOutcome.NeedsPathChoice, _cultivation.TryAdvance(),
                "Tier 0 peak with no path must demand the choice");
            Assert.AreEqual(2, _save.currentStage);
        }

        [Test]
        public void RateRecalculates_OnEveryAdvance()
        {
            Assert.AreEqual(BigNumber.One, _save.GetAsePerSecond(), "stage 1 baseline");
            SetAse(100);
            _cultivation.TryAdvance();
            Assert.AreEqual(BigNumber.FromDouble(5.0), _save.GetAsePerSecond(),
                "stage 2 multiplier ×5 must land in the cached rate immediately");
        }

        // ---- the path gate (GAMEPLAY §3.3) ----

        [Test]
        public void ChoosePath_IsTheAdvance_FiresBothEvents()
        {
            _save.currentStage = 2;
            SetAse(5500);

            int chosenPath = -1;
            int advancedTo = -1;
            _cultivation.OnPathChosen += p => chosenPath = p;
            _cultivation.OnStageAdvanced += s => advancedTo = s;

            Assert.IsTrue(_cultivation.ChoosePath(1)); // Sango
            Assert.AreEqual(1, _save.currentPath);
            Assert.AreEqual(3, _save.currentStage, "choosing IS the advance into Tier 1");
            Assert.AreEqual(1, chosenPath);
            Assert.AreEqual(3, advancedTo);
        }

        [Test]
        public void ChoosePath_RefusedOffTheGate_OrBelowThreshold()
        {
            Assert.IsFalse(_cultivation.ChoosePath(0), "stage 1 is not the gate");

            _save.currentStage = 2;
            SetAse(5499);
            Assert.IsFalse(_cultivation.ChoosePath(0), "gate still needs the threshold");
            Assert.AreEqual(-1, _save.currentPath);
        }

        [Test]
        public void Sango_DoublesTheRate_TheInstantOfSelection()
        {
            _save.currentStage = 2;
            SetAse(5500);

            _cultivation.ChoosePath(1); // Sango ×2 online

            // 1.0 base × 80 (Aláàṣẹ) × 2.0 = 160/s — the visible jump.
            Assert.AreEqual(BigNumber.FromDouble(160.0), _save.GetAsePerSecond());
        }

        [Test]
        public void Osun_WrapsPermanentAndActiveTogether()
        {
            _save.lineage.permanentAseBonus = 0.25;
            _save.currentStage = 2;
            SetAse(5500);

            _cultivation.ChoosePath(2); // Osun council ×2

            // 80 × (1 + 2 × (0.25 + 0)) = 120/s.
            Assert.AreEqual(BigNumber.FromDouble(120.0), _save.GetAsePerSecond());
        }

        [Test]
        public void Pathless_AllModifiersReadNeutral()
        {
            Assert.AreEqual(-1, _save.currentPath);
            Assert.AreEqual(1.0, _cultivation.PathOnlineMultiplier);
            Assert.AreEqual(1.0, _cultivation.PathOfflineRateModifier);
            Assert.AreEqual(1.0, _cultivation.CouncilBonusModifier);
        }

        // ---- tap-to-channel (GAMEPLAY §5.3) ----

        [Test]
        public void ChannelTap_GrantsTapChannelSecondsOfProduction()
        {
            _aseGen.ChannelTap(); // stage-1 rate 1.0/s × 5s
            Assert.AreEqual(BigNumber.FromDouble(5.0), _save.GetAse());
        }

        [Test]
        public void ChannelTap_FlowsThroughPathMultipliers()
        {
            _save.currentStage = 2;
            SetAse(5500);
            _cultivation.ChoosePath(1); // Sango → 160/s

            BigNumber before = _save.GetAse();
            _aseGen.ChannelTap();

            Assert.AreEqual(BigNumber.FromDouble(800.0), _save.GetAse() - before,
                "channeling as Sango must grant 160 × 5");
        }

        // ---- tribulation eligibility (once per generation) ----

        [Test]
        public void Eligibility_FiresExactlyOnce_ViaAseChanges()
        {
            _save.currentStage = 5;
            _aseGen.RecalculateRate(); // stage-6 rate so taps grant Àṣẹ
            SetAse(25_000_000);

            int announcements = 0;
            _cultivation.OnTribulationAvailable += () => announcements++;

            _aseGen.ChannelTap(); // crosses/holds the gate → announce
            _aseGen.ChannelTap(); // still over the gate → must NOT announce again

            Assert.AreEqual(1, announcements);
        }

        [Test]
        public void Eligibility_AnnouncedAtBegin_WhenLoadedSaveAlreadyQualifies()
        {
            // An overnight bank can arm the tribulation before the first frame.
            var loaded = new SaveData { currentStage = 5 };
            loaded.SetAse(new BigNumber(54.0, 6)); // full 8h bank

            int announcements = 0;
            _cultivation.OnTribulationAvailable += () => announcements++;
            _cultivation.Begin(loaded);

            Assert.AreEqual(1, announcements);
        }
    }
}
