using System;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #38 — Marketplace cadence: rival Houses surface at Àṣẹ milestones, wait
    /// patiently, resolve via ContestResolver, renown is applied and rate recomputed.
    /// Mirrors CrossroadsSystemTests: ServiceLocator + injected config / RNG.
    /// </summary>
    public class MarketplaceSystemTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private CultivationSystem _cultivation;
        private MarketplaceSystem _marketplace;
        private ContestConfig _config;
        private SaveData _save;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("MarketplaceTestHost");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            ServiceLocator.Register(_aseGen);

            _cultivation = _host.AddComponent<CultivationSystem>();
            EditModeTestHelpers.InjectArray(_cultivation, "_stages", EditModeTestHelpers.MakeStageTable());
            EditModeTestHelpers.InjectArray(_cultivation, "_paths", EditModeTestHelpers.MakePathTable());
            EditModeTestHelpers.Inject(_cultivation, "_tribulationConfig", EditModeTestHelpers.MakeTribulationConfig());
            ServiceLocator.Register(_cultivation);

            _config = EditModeTestHelpers.MakeContestConfig();

            _marketplace = _host.AddComponent<MarketplaceSystem>();
            EditModeTestHelpers.Inject(_marketplace, "_config", _config);
            EditModeTestHelpers.Inject(_marketplace, "_remembranceConfig", EditModeTestHelpers.MakeRemembranceConfig());
            ServiceLocator.Register(_marketplace);

            _save = new SaveData();
            _cultivation.Begin(_save);
            _aseGen.Begin(_save);
            _marketplace.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        // ---- milestone firing ----

        [Test]
        public void BelowMilestone_NoPendingContest()
        {
            _save.SetAse(BigNumber.FromDouble(999)); // below 1 000 milestone
            _marketplace.EvaluateMilestone();
            Assert.IsFalse(_marketplace.HasPending, "no contest surfaces below the milestone");
        }

        [Test]
        public void AtMilestone_ContestBecomePending()
        {
            // FakeRandom(0.0 × 4) → name index 0 ("Adé"), path 0, power 0.75, stance 0 (Strike)
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            Assert.IsTrue(_marketplace.HasPending, "a contest surfaces at the Àṣẹ milestone");
            Assert.IsNotNull(_marketplace.Pending);
        }

        [Test]
        public void Pending_CarriesHouseFields_InExpectedRanges()
        {
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            var pc = _marketplace.Pending;
            Assert.IsNotNull(pc, "pending contest must not be null at milestone");
            Assert.IsFalse(string.IsNullOrEmpty(pc.houseName), "house must have a name from the pool");
            Assert.GreaterOrEqual(pc.houseStance, 0);
            Assert.LessOrEqual(pc.houseStance, 2, "stance index in [0,2]");
            Assert.GreaterOrEqual(pc.housePowerRatio, _config.housePowerMin);
            Assert.LessOrEqual(pc.housePowerRatio, _config.housePowerMax);
        }

        // ---- determinism ----

        [Test]
        public void KnownFakeRandom_ProducesDeterministicHouse()
        {
            // Draws in order: name, path, power, stance (HouseGenerator)
            // namePool = {"Adé", "Bàbá"}, pathCount = 3
            // Draw 1 name:  Index(0.0, 2) = 0 → "Adé"
            // Draw 2 path:  Index(0.0, 3) = 0
            // Draw 3 power: 0.75 + 0.0*0.5 = 0.75
            // Draw 4 stance: Index(0.0, 3) = 0 → Strike
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            var pc = _marketplace.Pending;
            Assert.AreEqual("Adé", pc.houseName, "name drawn from pool index 0");
            Assert.AreEqual(0, pc.houseStance, "stance = Strike (index 0)");
            Assert.AreEqual(0.75, pc.housePowerRatio, 1e-12, "power at min when roll=0");
        }

        // ---- ChooseStance: win ----

        [Test]
        public void ChooseStance_Win_IncreasesRenownAndClearsContest()
        {
            // Surface: all 0.0 rolls → "Adé", path 0, power 0.75, stance Strike (int 0)
            // Resolve roll = 0.0: player Strike vs house Strike, power 0.75
            //   odds = 0.5 + 0*0.20 + 0.30*(1-0.75) = 0.575
            //   roll 0.0 < 0.575 → won
            //   stake = 0.10 * (1-0.575) = 0.0425, delta = +0.0425
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            var outcome = _marketplace.ChooseStance(Stance.Strike);

            Assert.IsTrue(outcome.Won, "win outcome expected with roll=0");
            Assert.AreEqual(1, _save.contestsResolved, "contestsResolved incremented");
            Assert.IsNull(_save.pendingContest, "pendingContest cleared after resolve");
            Assert.AreEqual(0.0425, _save.lineage.renown, 1e-12, "renown gains the win stake");
        }

        // ---- ChooseStance: loss floors renown ----

        [Test]
        public void ChooseStance_Loss_FloorsRenownAtZero()
        {
            // Surface: all 0.0 → "Adé", path 0, power 0.75, stance Strike (int 0)
            // Resolve roll = 0.99 ≥ 0.575 → loss; delta = -0.0425*0.5 = -0.02125
            // renown starts 0, 0 + (-0.02125) → floored at 0
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0, 0.99));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            _marketplace.ChooseStance(Stance.Strike);

            Assert.AreEqual(0.0, _save.lineage.renown, 1e-12, "loss cannot push renown below 0");
            Assert.AreEqual(1, _save.contestsResolved);
        }

        // ---- rate recomputes after resolve ----

        [Test]
        public void ChooseStance_Win_RateReflectsNewRenown()
        {
            // After a win the rate must increase because renown entered the lineage factor.
            BigNumber rateBefore = _save.GetAsePerSecond();

            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();
            _marketplace.ChooseStance(Stance.Strike);

            BigNumber rateAfter = _save.GetAsePerSecond();
            Assert.IsTrue(rateAfter > rateBefore,
                "rate must increase after a renown-positive clash outcome");
        }

        // ---- patient queue ----

        [Test]
        public void PatientQueue_BothMilestonesCrossed_OnlyOneSurfaces()
        {
            // Re-initialise with a two-milestone config.
            UnityEngine.Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
            SetUpWithConfig(EditModeTestHelpers.MakeTwoMilestoneContestConfig());

            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(5_001)); // crosses BOTH milestones (1k and 5k)
            _marketplace.EvaluateMilestone();

            // Only one should surface (one at a time rule).
            Assert.IsTrue(_marketplace.HasPending, "first contest surfaces");
            Assert.AreEqual(0, _save.contestsResolved, "none resolved yet");
        }

        [Test]
        public void PatientQueue_AfterFirstResolve_SecondSurfaces()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
            SetUpWithConfig(EditModeTestHelpers.MakeTwoMilestoneContestConfig());

            // Surface first (4 draws), resolve it (1 draw), expect second to surface (4 draws).
            // We need 9 draws total: first surface (4) + resolve (1) + second surface (4).
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0,  // first surface
                                                        0.0,                   // resolve win
                                                        0.0, 0.0, 0.0, 0.0)); // second surface
            _save.SetAse(BigNumber.FromDouble(5_001));
            _marketplace.EvaluateMilestone();

            Assert.IsTrue(_marketplace.HasPending, "first pending");
            Assert.AreEqual(0, _save.contestsResolved);

            _marketplace.ChooseStance(Stance.Strike); // resolves first

            Assert.AreEqual(1, _save.contestsResolved, "first resolved");
            Assert.IsTrue(_marketplace.HasPending, "second contest surfaces after first resolves");
        }

        [Test]
        public void PatientQueue_BothResolved_NoPending()
        {
            UnityEngine.Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
            SetUpWithConfig(EditModeTestHelpers.MakeTwoMilestoneContestConfig());

            // 4 draws per surface, 1 draw per resolve → 4+1+4+1 = 10
            _marketplace.SetRandomSource(new FakeRandom(
                0.0, 0.0, 0.0, 0.0, // first surface
                0.0,                  // first resolve
                0.0, 0.0, 0.0, 0.0, // second surface
                0.0));               // second resolve
            _save.SetAse(BigNumber.FromDouble(5_001));
            _marketplace.EvaluateMilestone();

            _marketplace.ChooseStance(Stance.Strike); // resolve first
            _marketplace.ChooseStance(Stance.Strike); // resolve second

            Assert.AreEqual(2, _save.contestsResolved);
            Assert.IsFalse(_marketplace.HasPending, "no further contests owed");
        }

        [Test]
        public void ResolvedContest_DoesNotRefire_BelowNextMilestone()
        {
            // Single-milestone config: surface and resolve; re-evaluate at same Àṣẹ.
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0, 0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();
            _marketplace.ChooseStance(Stance.Strike);

            // Now re-evaluate — should not fire a second contest (only 1 milestone).
            _marketplace.EvaluateMilestone();

            Assert.IsFalse(_marketplace.HasPending, "resolved contest does not re-fire");
            Assert.AreEqual(1, _save.contestsResolved);
        }

        // ---- OnContestReady event ----

        [Test]
        public void OnContestReady_FiresWhenHouseSurfaces()
        {
            PendingContest received = null;
            _marketplace.OnContestReady += pc => received = pc;
            _marketplace.SetRandomSource(new FakeRandom(0.0, 0.0, 0.0, 0.0));

            _save.SetAse(BigNumber.FromDouble(1_000));
            _marketplace.EvaluateMilestone();

            Assert.IsNotNull(received, "OnContestReady fires when a house surfaces");
        }

        // ---- helper: re-setup with a custom config ----

        private void SetUpWithConfig(ContestConfig config)
        {
            _host = new GameObject("MarketplaceTestHost");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            ServiceLocator.Register(_aseGen);

            _cultivation = _host.AddComponent<CultivationSystem>();
            EditModeTestHelpers.InjectArray(_cultivation, "_stages", EditModeTestHelpers.MakeStageTable());
            EditModeTestHelpers.InjectArray(_cultivation, "_paths", EditModeTestHelpers.MakePathTable());
            EditModeTestHelpers.Inject(_cultivation, "_tribulationConfig", EditModeTestHelpers.MakeTribulationConfig());
            ServiceLocator.Register(_cultivation);

            _config = config;
            _marketplace = _host.AddComponent<MarketplaceSystem>();
            EditModeTestHelpers.Inject(_marketplace, "_config", _config);
            EditModeTestHelpers.Inject(_marketplace, "_remembranceConfig", EditModeTestHelpers.MakeRemembranceConfig());
            ServiceLocator.Register(_marketplace);

            _save = new SaveData();
            _cultivation.Begin(_save);
            _aseGen.Begin(_save);
            _marketplace.Begin(_save);
        }
    }
}
