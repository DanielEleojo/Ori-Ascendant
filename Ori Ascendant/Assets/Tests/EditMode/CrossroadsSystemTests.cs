using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Dynasty PRD Phase 1, slice 2a: Crossroads — a dilemma fires, the player
    /// chooses, steadfastness moves, a Deed is recorded.
    /// Tests are system-level: ServiceLocator + injected deck/RNG as specified
    /// in the issue acceptance criteria.
    /// </summary>
    public class CrossroadsSystemTests
    {
        private GameObject _host;
        private CrossroadsSystem _crossroads;
        private AseGenerationSystem _aseGen;
        private CrossroadsConfig _config;
        private SaveData _save;

        // Seed deck: card 0 has virtue-tagged options covering all three virtues
        // (indices 0=Patience, 1=Courage, 2=Mercy). Card 1 is a second card.
        private CrossroadsCard Card0 => new CrossroadsCard
        {
            id = "card_a",
            prompt = "A stranger blocks the road.",
            options = new[]
            {
                new CrossroadsOption { virtueIndex = 0, optionText = "Wait in patience." },
                new CrossroadsOption { virtueIndex = 1, optionText = "Push past boldly." },
                new CrossroadsOption { virtueIndex = 2, optionText = "Step aside and yield." },
            }
        };

        private CrossroadsCard Card1 => new CrossroadsCard
        {
            id = "card_b",
            prompt = "You find a coin in the road.",
            options = new[]
            {
                new CrossroadsOption { virtueIndex = 0, optionText = "Wait to see whose it is." },
                new CrossroadsOption { virtueIndex = -1, optionText = "Pocket it — no one saw." },
            }
        };

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("CrossroadsTestHost");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            ServiceLocator.Register(_aseGen);

            _config = EditModeTestHelpers.MakeCrossroadsConfig(Card0, Card1);

            _crossroads = _host.AddComponent<CrossroadsSystem>();
            EditModeTestHelpers.Inject(_crossroads, "_config", _config);
            ServiceLocator.Register(_crossroads);

            _save = new SaveData { chosenOri = 0 }; // vowed Patience
            _aseGen.Begin(_save);
            _crossroads.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        // ---- crossroads firing ----

        [Test]
        public void BelowMilestone_NoCrossroadsIsPending()
        {
            _save.SetAse(BigNumber.FromDouble(999)); // below 1 000 milestone
            _crossroads.EvaluateMilestone();
            Assert.IsFalse(_crossroads.HasPending, "no crossroads below the milestone");
        }

        [Test]
        public void AtMilestone_CrossroadsBecomePending()
        {
            // FakeRandom(0) → index 0 → card_a
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _save.SetAse(BigNumber.FromDouble(1_000)); // hits milestone
            _crossroads.EvaluateMilestone();

            Assert.IsTrue(_crossroads.HasPending, "a crossroads fires at the milestone");
            Assert.AreEqual("card_a", _save.pendingCrossroadsId);
            Assert.AreEqual("card_a", _crossroads.PendingCard?.id);
        }

        [Test]
        public void MilestoneEvent_FiresOnCrossroadsReady()
        {
            string receivedId = null;
            _crossroads.OnCrossroadsReady += id => receivedId = id;
            _crossroads.SetRandomSource(new FakeRandom(0.0));

            _save.SetAse(BigNumber.FromDouble(1_000));
            _crossroads.EvaluateMilestone();

            Assert.AreEqual("card_a", receivedId, "OnCrossroadsReady fires with the card id");
        }

        [Test]
        public void MilestoneFires_OnlyOncePerLife()
        {
            _crossroads.SetRandomSource(new FakeRandom(0.0));
            _save.SetAse(BigNumber.FromDouble(1_000));
            _crossroads.EvaluateMilestone(); // fires card_a

            // Resolve the crossroads, then re-evaluate milestone
            _crossroads.MakeChoice(0);
            Assert.AreEqual(1, _save.deeds.Count, "deed recorded after choice");

            // Àṣẹ still above milestone — no second crossroads should fire
            int readyCount = 0;
            _crossroads.OnCrossroadsReady += _ => readyCount++;
            _crossroads.EvaluateMilestone(); // would fire again if not guarded
            Assert.IsFalse(_crossroads.HasPending, "milestone fires only once per life");
            Assert.AreEqual(0, readyCount);
        }

        // ---- choice → tally mapping ----

        [Test]
        public void OriAlignedChoice_IncrementsHeldAndTrials()
        {
            ArmPending("card_a");
            _save.chosenOri = 0; // Patience = option index 0 in card_a

            _crossroads.MakeChoice(0); // choose Patience option (index 0)

            Assert.AreEqual(1, _save.oriTrials, "trials always increments");
            Assert.AreEqual(1, _save.oriHeld,   "held increments for Ori-aligned choice");
        }

        [Test]
        public void OffOriChoice_IncrementsTrialsOnly()
        {
            ArmPending("card_a");
            _save.chosenOri = 0; // Patience

            _crossroads.MakeChoice(1); // choose Courage (virtueIndex 1, not Patience)

            Assert.AreEqual(1, _save.oriTrials, "trials always increments");
            Assert.AreEqual(0, _save.oriHeld,   "held does NOT increment for off-Ori choice");
        }

        [Test]
        public void VirtueNeutralOption_IncrementsTrialsOnly()
        {
            ArmPending("card_b");
            _save.chosenOri = 0; // Patience

            _crossroads.MakeChoice(1); // virtueIndex -1 (neutral)

            Assert.AreEqual(1, _save.oriTrials);
            Assert.AreEqual(0, _save.oriHeld);
        }

        // ---- Deed recording ----

        [Test]
        public void OriAlignedChoice_RecordsDeed_WithAlignedTrue()
        {
            ArmPending("card_a");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(0);

            Assert.AreEqual(1, _save.deeds.Count, "exactly one deed recorded");
            var deed = _save.deeds[0];
            Assert.AreEqual("card_a", deed.crossroadsId);
            Assert.AreEqual(0, deed.chosenOptionIndex);
            Assert.IsTrue(deed.wasOriAligned);
        }

        [Test]
        public void OffOriChoice_RecordsDeed_WithAlignedFalse()
        {
            ArmPending("card_a");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(2); // Mercy (virtueIndex 2)

            var deed = _save.deeds[0];
            Assert.AreEqual("card_a", deed.crossroadsId);
            Assert.AreEqual(2, deed.chosenOptionIndex);
            Assert.IsFalse(deed.wasOriAligned);
        }

        [Test]
        public void OnCrossroadsResolved_FiresAfterStateIsWritten()
        {
            ArmPending("card_a");
            _save.chosenOri = 1; // Courage = option[1] in card_a

            DeedData eventDeed = null;
            int trialsAtEvent = -1;
            _crossroads.OnCrossroadsResolved += deed =>
            {
                eventDeed = deed;
                trialsAtEvent = _save.oriTrials; // must already be updated
            };

            _crossroads.MakeChoice(1);

            Assert.IsNotNull(eventDeed, "OnCrossroadsResolved fires");
            Assert.IsTrue(eventDeed.wasOriAligned);
            Assert.AreEqual(1, trialsAtEvent, "tally is written before the event fires");
        }

        // ---- MakeChoice guard-rails ----

        [Test]
        public void MakeChoice_NoPending_ReturnsFalse()
        {
            Assert.IsFalse(_crossroads.MakeChoice(0), "no-op when nothing is pending");
            Assert.AreEqual(0, _save.oriTrials);
            Assert.AreEqual(0, _save.deeds.Count);
        }

        [Test]
        public void MakeChoice_OutOfRangeIndex_ReturnsFalse()
        {
            ArmPending("card_a"); // card_a has 3 options (indices 0-2)

            Assert.IsFalse(_crossroads.MakeChoice(-1));
            Assert.IsFalse(_crossroads.MakeChoice(3));
            Assert.AreEqual(0, _save.oriTrials, "bad index never updates tally");
            Assert.IsTrue(_crossroads.HasPending, "pending survives a bad choice attempt");
        }

        [Test]
        public void MakeChoice_ClearsPendingState()
        {
            ArmPending("card_a");
            _crossroads.MakeChoice(0);

            Assert.IsFalse(_crossroads.HasPending);
            Assert.AreEqual("", _save.pendingCrossroadsId);
        }

        // ---- Deed Remembrance fields (beatIndex / strayed) ----

        [Test]
        public void OriAlignedChoice_SetsStrayed_False_AndWasOriAligned_True()
        {
            ArmPending("card_a");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(0); // Patience option

            var deed = _save.deeds[0];
            Assert.IsFalse(deed.strayed, "Ori-aligned choice: strayed=false");
            Assert.IsTrue(deed.wasOriAligned, "Ori-aligned choice: wasOriAligned=true");
        }

        [Test]
        public void OffOriChoice_SetsStrayed_True_AndWasOriAligned_False()
        {
            ArmPending("card_a");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(1); // Courage — off-Ori

            var deed = _save.deeds[0];
            Assert.IsTrue(deed.strayed, "off-Ori choice: strayed=true");
            Assert.IsFalse(deed.wasOriAligned, "off-Ori choice: wasOriAligned=false");
        }

        [Test]
        public void MakeChoice_SetsBeatIndex_ForFirstCard()
        {
            // card_a is at index 0 in the test deck (Card0 first, Card1 second)
            ArmPending("card_a");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(0);

            Assert.AreEqual(0, _save.deeds[0].beatIndex, "card_a is deck position 0");
        }

        [Test]
        public void MakeChoice_SetsBeatIndex_ForSecondCard()
        {
            // card_b is at index 1 in the test deck
            ArmPending("card_b");
            _save.chosenOri = 0;

            _crossroads.MakeChoice(0);

            Assert.AreEqual(1, _save.deeds[0].beatIndex, "card_b is deck position 1");
        }

        // ---- session resume: pending crossroads is patient ----

        [Test]
        public void Begin_WithPendingSaved_FiresReadyEvent()
        {
            // Simulate a crossroads pending from a previous session
            _save.pendingCrossroadsId = "card_b";

            // Re-Begin (simulates session resume)
            ServiceLocator.Clear();
            var newHost = new GameObject("ResumeHost");
            var newSystem = newHost.AddComponent<CrossroadsSystem>();
            EditModeTestHelpers.Inject(newSystem, "_config", _config);
            ServiceLocator.Register(newSystem);

            string receivedId = null;
            newSystem.OnCrossroadsReady += id => receivedId = id;
            newSystem.Begin(_save);

            Assert.AreEqual("card_b", receivedId, "session resume surfaces the pending crossroads");
            Assert.AreEqual("card_b", newSystem.PendingCard?.id);

            Object.DestroyImmediate(newHost);
        }

        // ---- helpers ----

        /// <summary>Directly arms a pending crossroads by setting the save field,
        /// bypassing the milestone (to test choice logic in isolation).</summary>
        private void ArmPending(string cardId)
        {
            _save.pendingCrossroadsId = cardId;
        }
    }
}
