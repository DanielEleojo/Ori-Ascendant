using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: SaveData JSON round-trip via Newtonsoft, including the BigNumber
    /// split fields at the magnitudes TECH_DESIGN §8 flags as serialization risk
    /// (e10 / e50 / e100 / e200), plus the locked v1 defaults.
    /// </summary>
    public class SaveDataRoundTripTests
    {
        private static SaveData RoundTrip(SaveData data) =>
            SaveSerializer.FromJson(SaveSerializer.ToJson(data));

        [Test]
        public void FreshSave_HasLockedV1Defaults()
        {
            var save = new SaveData();

            Assert.AreEqual(1, save.schemaVersion);
            Assert.IsTrue(save.GetAse().IsZero, "new save must hold zero Àṣẹ");
            Assert.IsTrue(save.GetAsePerSecond().IsZero);
            Assert.AreEqual(0, save.currentStage);
            Assert.AreEqual(-1, save.currentPath, "path not chosen at start");
            Assert.AreEqual(-1, save.chosenOri, "Ori not vowed at start (Àkùnlẹ̀yàn pending)");
            Assert.AreEqual(0, save.lastSaveTimestamp, "0 = never saved (fresh-install guard)");
            Assert.AreEqual(0, save.generationStartTimestamp);
            Assert.AreEqual(0, save.seenFlags);
            Assert.IsNotNull(save.council);
            Assert.IsEmpty(save.council);
            Assert.AreEqual(0.0, save.lineage.permanentAseBonus, "ADDITIVE accumulator defaults to 0.0");
            Assert.AreEqual(0, save.lineage.generationCount);
        }

        [TestCase(1.234, 10)]
        [TestCase(5.678, 50)]
        [TestCase(9.999, 100)]
        [TestCase(2.5, 200)]
        public void AseAmount_RoundTrips_AtLargeExponents(double mantissa, int exponent)
        {
            var original = new BigNumber(mantissa, exponent);
            var save = new SaveData();
            save.SetAse(original);

            var restored = RoundTrip(save);

            Assert.IsNotNull(restored);
            Assert.AreEqual(original, restored.GetAse(),
                $"round-trip lost precision at ~e{exponent}");
        }

        [Test]
        public void FullState_RoundTrips_Exactly()
        {
            var save = new SaveData
            {
                currentStage = 4,
                currentPath = 2,
                chosenOri = 1,
                lastSaveTimestamp = 1781136000,
                generationStartTimestamp = 1781100000,
                seenFlags = SeenFlags.ChannelHint | SeenFlags.FallCeremony,
            };
            save.SetAse(new BigNumber(123.456, 9));
            save.SetAsePerSecond(new BigNumber(1.875, 3));
            save.lineage.permanentAseBonus = 0.35;
            save.lineage.generationCount = 7;
            save.council.Add(new AncestorData
            {
                peakStage = 5,
                path = 1,
                didAscend = false,
                bonusMultiplier = 0.4,
                completedTimestamp = 1781000000,
            });

            var restored = RoundTrip(save);

            Assert.IsNotNull(restored);
            Assert.AreEqual(save.schemaVersion, restored.schemaVersion);
            Assert.AreEqual(save.GetAse(), restored.GetAse());
            Assert.AreEqual(save.GetAsePerSecond(), restored.GetAsePerSecond());
            Assert.AreEqual(4, restored.currentStage);
            Assert.AreEqual(2, restored.currentPath);
            Assert.AreEqual(1, restored.chosenOri, "chosenOri survives a round-trip");
            Assert.AreEqual(1781136000, restored.lastSaveTimestamp);
            Assert.AreEqual(1781100000, restored.generationStartTimestamp);
            Assert.IsTrue(restored.HasSeen(SeenFlags.ChannelHint));
            Assert.IsFalse(restored.HasSeen(SeenFlags.AscendCeremony));
            Assert.IsTrue(restored.HasSeen(SeenFlags.FallCeremony));
            Assert.AreEqual(1, restored.council.Count);
            Assert.AreEqual(5, restored.council[0].peakStage);
            Assert.AreEqual(1, restored.council[0].path);
            Assert.IsFalse(restored.council[0].didAscend);
            Assert.AreEqual(0.4, restored.council[0].bonusMultiplier);
            Assert.AreEqual(1781000000, restored.council[0].completedTimestamp);
            Assert.AreEqual(0.35, restored.lineage.permanentAseBonus);
            Assert.AreEqual(7, restored.lineage.generationCount);
        }

        [Test]
        public void UnknownJsonMembers_AreIgnored_ForwardCompatibility()
        {
            string futureJson = "{\"schemaVersion\":1,\"aseMantissa\":5.0,\"aseExponent\":3," +
                                "\"someFutureField\":\"ignored\",\"currentStage\":2}";

            var restored = SaveSerializer.FromJson(futureJson);

            Assert.IsNotNull(restored);
            Assert.AreEqual(new BigNumber(5.0, 3), restored.GetAse());
            Assert.AreEqual(2, restored.currentStage);
            Assert.AreEqual(-1, restored.currentPath, "missing members keep field defaults");
        }

        [Test]
        public void PreExistingV1Save_LoadsWithChosenOriDefaulted()
        {
            // A pre-Dynasty v1 save was written without chosenOri (add-only field
            // per ADR-0001: no schemaVersion bump). It must load without error and
            // default the new field to -1 (no vow held — the modal will surface).
            string legacyV1Json = "{\"schemaVersion\":1," +
                                  "\"aseMantissa\":0.0,\"aseExponent\":0," +
                                  "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                  "\"currentStage\":0,\"currentPath\":-1," +
                                  "\"lastSaveTimestamp\":1781136000," +
                                  "\"generationStartTimestamp\":1781100000," +
                                  "\"seenFlags\":0," +
                                  "\"council\":[]," +
                                  "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":0}}";

            var restored = SaveSerializer.FromJson(legacyV1Json);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.schemaVersion, "schema version stays at 1 (add-only field, ADR-0001)");
            Assert.AreEqual(-1, restored.chosenOri,
                "legacy save without chosenOri must default to -1 so the modal surfaces");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{not json")]
        public void CorruptOrEmptyJson_ReturnsNull(string json)
        {
            Assert.IsNull(SaveSerializer.FromJson(json));
        }
    }
}
