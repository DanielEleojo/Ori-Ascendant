using System.Collections.Generic;
using NUnit.Framework;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// The Chronicle core (the deepening, slice 4a follow-up): one owner for recording a
    /// Crossroads choice as a Deed, the steadfastness read-model OVER those Deeds, the
    /// remembrance derivation (Title / Nickname / Defining Deed), and the per-life reset.
    /// Pure C# — exercised directly, with no GameObject host or ServiceLocator. Deeds are
    /// the source of truth; oriHeld/oriTrials are a written cache kept in lockstep.
    /// FakeRandom is shared with TribulationSystemTests (same test assembly).
    /// </summary>
    public class ChronicleTests
    {
        // ──────────────────────────── RECORD ────────────────────────────

        [Test]
        public void RecordChoice_HoldingTheVow_AppendsAnAlignedDeed_AndBumpsCache()
        {
            var save = new SaveData { currentOri = 0, currentStage = 2 };
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();

            Chronicle.RecordChoice(save, deck.beats[1], 1, optionIndex: 0); // option 0 → oriIndex 0 == vow

            Assert.AreEqual(1, save.deeds.Count);
            DeedData deed = save.deeds[0];
            Assert.IsTrue(deed.aligned, "option matching the vowed Ori holds the vow");
            Assert.AreEqual(1, deed.crossroadsIndex, "the beat index is recorded");
            Assert.AreEqual(0, deed.chosenOri, "the chosen option's virtue is recorded");
            Assert.AreEqual(2, deed.stage, "the stage at the moment of choice is recorded");
            Assert.AreEqual(1, save.oriTrials, "the cache advances in lockstep with the Deed");
            Assert.AreEqual(1, save.oriHeld);
        }

        [Test]
        public void RecordChoice_Straying_AppendsAnUnalignedDeed()
        {
            var save = new SaveData { currentOri = 0 };
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();

            Chronicle.RecordChoice(save, deck.beats[0], 0, optionIndex: 1); // oriIndex 1 != vow 0

            Assert.IsFalse(save.deeds[0].aligned);
            Assert.AreEqual(1, save.oriTrials);
            Assert.AreEqual(0, save.oriHeld, "a strayed choice does not hold the vow");
        }

        [Test]
        public void RecordChoice_IgnoresAnOutOfRangeOption()
        {
            var save = new SaveData { currentOri = 0 };
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();

            Chronicle.RecordChoice(save, deck.beats[0], 0, optionIndex: 99);

            Assert.IsEmpty(save.deeds, "an invalid option records nothing");
            Assert.AreEqual(0, save.oriTrials);
        }

        // ──────────────────── STEADFASTNESS READ-MODEL ────────────────────

        [Test]
        public void ReadModel_NoDeeds_IsZeroAcross()
        {
            var save = new SaveData();
            Assert.AreEqual(0, Chronicle.Trials(save));
            Assert.AreEqual(0, Chronicle.Held(save));
            Assert.AreEqual(0.0, Chronicle.SteadfastnessRate(save), 1e-12,
                "a life that faced no Crossroads earns no steadfastness credit");
        }

        [Test]
        public void SteadfastnessRate_DerivesFromTheDeeds()
        {
            var save = new SaveData { currentOri = 0 };
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            for (int i = 0; i < 3; i++) Chronicle.RecordChoice(save, deck.beats[0], 0, 0); // held
            for (int i = 0; i < 2; i++) Chronicle.RecordChoice(save, deck.beats[0], 0, 1); // strayed

            Assert.AreEqual(5, Chronicle.Trials(save));
            Assert.AreEqual(3, Chronicle.Held(save));
            Assert.AreEqual(0.6, Chronicle.SteadfastnessRate(save), 1e-12, "held 3 of 5");
        }

        // ────────────────────────── REMEMBER (pure) ──────────────────────────

        [Test]
        public void DeriveRemembrance_Ascend_IsHonorificPlusPooledName()
        {
            var config = EditModeTestHelpers.MakeRemembranceConfig(); // { Adé, Olú, Ìfẹ́ }
            string r = Chronicle.DeriveRemembrance(true, "Aṣẹ́gun", null, null, config, nameIndex: 1);
            Assert.AreEqual("Aṣẹ́gun Olú", r);
        }

        [Test]
        public void DeriveRemembrance_Ascend_ClampsTheNameIndexToThePool()
        {
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            string r = Chronicle.DeriveRemembrance(true, "Aṣẹ́gun", null, null, config, nameIndex: 99);
            Assert.AreEqual("Aṣẹ́gun Ìfẹ́", r, "an over-range draw clamps to the last pooled name");
        }

        [Test]
        public void DeriveRemembrance_Ascend_EmptyPool_FallsBackToTheHonorific()
        {
            var empty = ScriptableObject.CreateInstance<RemembranceConfig>();
            empty.personalNames = new string[0];
            string r = Chronicle.DeriveRemembrance(true, "Aṣẹ́gun", null, null, empty, nameIndex: 0);
            Assert.AreEqual("Aṣẹ́gun", r);
        }

        [Test]
        public void DeriveRemembrance_Fall_IsTheDefiningDeed_TheFirstStray()
        {
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deeds = new List<DeedData>
            {
                new DeedData { crossroadsIndex = 2, chosenOri = 0, stage = 1, aligned = true },  // held
                new DeedData { crossroadsIndex = 0, chosenOri = 1, stage = 2, aligned = false }, // FIRST stray → beat 0
                new DeedData { crossroadsIndex = 1, chosenOri = 3, stage = 3, aligned = false }, // later stray, ignored
            };

            string r = Chronicle.DeriveRemembrance(false, "ignored", deeds, deck, config, 0);

            Assert.AreEqual(deck.beats[0].fallenEpithet, r,
                "the Nickname is the first strayed choice's epithet, never a later one");
        }

        [Test]
        public void DeriveRemembrance_FaithfulFall_IsTheSharedDignifiedLine()
        {
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deeds = new List<DeedData>
            {
                new DeedData { crossroadsIndex = 0, chosenOri = 0, stage = 1, aligned = true },
                new DeedData { crossroadsIndex = 1, chosenOri = 0, stage = 2, aligned = true },
            };

            string r = Chronicle.DeriveRemembrance(false, null, deeds, deck, config, 0);

            Assert.AreEqual(config.faithfulFallLine, r,
                "a life that never strayed yet fell shares one line, not a stray epithet");
        }

        [Test]
        public void DeriveRemembrance_Fall_UnnameableStray_FallsBackToTheDignifiedLine()
        {
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            var deeds = new List<DeedData>
            {
                new DeedData { crossroadsIndex = 99, chosenOri = 1, stage = 2, aligned = false }, // out of deck range
            };

            string r = Chronicle.DeriveRemembrance(false, null, deeds, deck, config, 0);

            Assert.AreEqual(config.faithfulFallLine, r, "a stray with no nameable beat → the dignified line");
        }

        // ───────────────────────── REMEMBER (roll) ─────────────────────────

        [Test]
        public void Remember_Ascend_DrawsThePersonalNameFromTheInjectedRandom()
        {
            var save = new SaveData();
            var config = EditModeTestHelpers.MakeRemembranceConfig(); // 3 names
            var rng = new FakeRandom(0.5); // 0.5 × 3 = 1.5 → index 1 → Olú

            string r = Chronicle.Remember(save, didAscend: true, "Aṣẹ́gun", null, config, rng);

            Assert.AreEqual("Aṣẹ́gun Olú", r);
        }

        [Test]
        public void Remember_Fall_ConsumesNoRandomness()
        {
            var save = new SaveData();
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            var config = EditModeTestHelpers.MakeRemembranceConfig();
            save.deeds.Add(new DeedData { crossroadsIndex = 0, chosenOri = 1, stage = 1, aligned = false });
            var rng = new FakeRandom(0.5, 0.99);

            string r = Chronicle.Remember(save, didAscend: false, "ignored", deck, config, rng);

            Assert.AreEqual(deck.beats[0].fallenEpithet, r);
            Assert.AreEqual(0.5, rng.NextDouble(), "a fall draws no name — the random sequence is untouched");
        }

        // ───────────────────────────── RESET ─────────────────────────────

        [Test]
        public void ResetForNewGeneration_ClearsTheLedgerAndTheCache()
        {
            var save = new SaveData { currentOri = 0 };
            var deck = EditModeTestHelpers.MakeCrossroadsDeck();
            Chronicle.RecordChoice(save, deck.beats[0], 0, 0);
            Chronicle.RecordChoice(save, deck.beats[0], 0, 1);
            Assert.AreEqual(2, save.deeds.Count);

            Chronicle.ResetForNewGeneration(save);

            Assert.IsEmpty(save.deeds, "the per-life ledger clears at the Crossing");
            Assert.AreEqual(0, save.oriHeld, "the cache clears too");
            Assert.AreEqual(0, save.oriTrials);
            Assert.AreEqual(0, Chronicle.Trials(save));
        }
    }
}
