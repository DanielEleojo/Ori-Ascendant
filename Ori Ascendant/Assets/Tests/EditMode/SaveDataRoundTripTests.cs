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

        [Test]
        public void ChannelHintShownAt_RoundTrips()
        {
            var save = new SaveData { channelHintShownAt = 1_781_200_000L };
            var restored = RoundTrip(save);
            Assert.AreEqual(1_781_200_000L, restored.channelHintShownAt,
                "channelHintShownAt must survive JSON round-trip");
        }

        [Test]
        public void PreExistingV1Save_LoadsWithChannelHintShownAtDefaultedToZero()
        {
            // A save written before issue #18 has no channelHintShownAt field.
            // Newtonsoft must leave it at 0 (never shown) — the hint will surface
            // after the appear delay on first resume, consistent with a fresh install.
            string legacyJson = "{\"schemaVersion\":1," +
                                "\"aseMantissa\":0.0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1," +
                                "\"lastSaveTimestamp\":1781136000," +
                                "\"generationStartTimestamp\":1781100000," +
                                "\"seenFlags\":0," +
                                "\"council\":[]," +
                                "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":0}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.schemaVersion,
                "schema version stays at 1 (add-only field, ADR-0001)");
            Assert.AreEqual(0L, restored.channelHintShownAt,
                "legacy save without channelHintShownAt must default to 0 (never shown)");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{not json")]
        public void CorruptOrEmptyJson_ReturnsNull(string json)
        {
            Assert.IsNull(SaveSerializer.FromJson(json));
        }

        [Test]
        public void AncestorData_Remembrance_RoundTrips()
        {
            var save = new SaveData();
            save.council.Add(new AncestorData
            {
                peakStage = 5,
                path = 1,
                didAscend = true,
                bonusMultiplier = 1.0,
                completedTimestamp = 1781000000,
                remembrance = "Aṣẹ́gun Adé",
            });

            var restored = RoundTrip(save);

            Assert.AreEqual("Aṣẹ́gun Adé", restored.council[0].remembrance,
                "remembrance string persists across a save round-trip");
        }

        [Test]
        public void AncestorData_Deeds_RoundTrip()
        {
            var save = new SaveData();
            save.deeds.Add(new DeedData { beatIndex = 2, strayed = true });
            save.deeds.Add(new DeedData { beatIndex = 0, strayed = false });

            var restored = RoundTrip(save);

            Assert.AreEqual(2, restored.deeds.Count);
            Assert.AreEqual(2, restored.deeds[0].beatIndex);
            Assert.IsTrue(restored.deeds[0].strayed);
            Assert.AreEqual(0, restored.deeds[1].beatIndex);
            Assert.IsFalse(restored.deeds[1].strayed);
        }

        [Test]
        public void PreExistingAncestor_LoadsWithNullRemembrance()
        {
            // A save written before slice 4a has no remembrance field on AncestorData.
            // Add-only field (ADR-0001): loads as null — Chronicle will handle null gracefully.
            string legacyJson = "{\"schemaVersion\":1,\"aseMantissa\":0.0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1,\"lastSaveTimestamp\":0," +
                                "\"generationStartTimestamp\":0,\"seenFlags\":0," +
                                "\"council\":[{\"peakStage\":5,\"path\":1,\"didAscend\":true," +
                                "\"bonusMultiplier\":1.0,\"completedTimestamp\":1781000000}]," +
                                "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":1}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.council.Count);
            Assert.IsNull(restored.council[0].remembrance,
                "legacy ancestor without remembrance field loads with null (add-only, ADR-0001)");
        }

        [Test]
        public void PreExistingV1Save_LoadsWithEmptyDeeds()
        {
            // A save written before slice 4a has no deeds field.
            // Add-only field (ADR-0001): loads with the default empty list.
            string legacyJson = "{\"schemaVersion\":1,\"aseMantissa\":0.0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1,\"lastSaveTimestamp\":0," +
                                "\"generationStartTimestamp\":0,\"seenFlags\":0," +
                                "\"council\":[]," +
                                "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":0}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.deeds, "deeds must not be null — default is an empty list");
            Assert.IsEmpty(restored.deeds, "legacy save without deeds loads with empty list");
        }

        // ---- Pending Crossroads Queue (issue #4) ----

        [Test]
        public void PendingCrossroadsQueue_RoundTrips()
        {
            var save = new SaveData
            {
                pendingCrossroadsId = "card_a",
            };
            save.pendingCrossroadsQueue.Add("card_b");
            save.pendingCrossroadsQueue.Add("card_c");

            var restored = RoundTrip(save);

            Assert.AreEqual("card_a", restored.pendingCrossroadsId, "active crossroads survives round-trip");
            Assert.IsNotNull(restored.pendingCrossroadsQueue, "queue must not be null after load");
            Assert.AreEqual(2, restored.pendingCrossroadsQueue.Count, "both queued crossroads survive round-trip");
            Assert.AreEqual("card_b", restored.pendingCrossroadsQueue[0]);
            Assert.AreEqual("card_c", restored.pendingCrossroadsQueue[1]);
        }

        [Test]
        public void PreExistingV1Save_LoadsWithEmptyPendingQueue()
        {
            // A save written before slice 2b has no pendingCrossroadsQueue field.
            // Add-only field (ADR-0001): loads with the default empty list.
            string legacyJson = "{\"schemaVersion\":1,\"aseMantissa\":0.0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1,\"lastSaveTimestamp\":0," +
                                "\"generationStartTimestamp\":0,\"seenFlags\":0," +
                                "\"pendingCrossroadsId\":\"card_a\"," +
                                "\"council\":[]," +
                                "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":0}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored);
            Assert.AreEqual("card_a", restored.pendingCrossroadsId, "legacy single pending id preserved");
            Assert.IsNotNull(restored.pendingCrossroadsQueue, "queue must not be null on old save — default empty list");
            Assert.IsEmpty(restored.pendingCrossroadsQueue, "legacy save without queue loads with empty list");
        }

        // ---- Chronicle (issue #7) ----

        [Test]
        public void ChronicleEntry_RoundTrips()
        {
            var save = new SaveData();
            save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = 3,
                chosenOri = 1,
                didAscend = true,
                remembrance = "Aṣẹ́gun Adé",
                completedTimestamp = 1_781_200_000L,
            });
            save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = 4,
                chosenOri = 0,
                didAscend = false,
                remembrance = "The Steadfast",
                completedTimestamp = 1_781_300_000L,
            });

            var restored = RoundTrip(save);

            Assert.IsNotNull(restored);
            Assert.AreEqual(2, restored.chronicle.Count);

            var e0 = restored.chronicle[0];
            Assert.AreEqual(3, e0.generationNumber);
            Assert.AreEqual(1, e0.chosenOri);
            Assert.IsTrue(e0.didAscend);
            Assert.AreEqual("Aṣẹ́gun Adé", e0.remembrance);
            Assert.AreEqual(1_781_200_000L, e0.completedTimestamp);

            var e1 = restored.chronicle[1];
            Assert.AreEqual(4, e1.generationNumber);
            Assert.IsFalse(e1.didAscend);
            Assert.AreEqual("The Steadfast", e1.remembrance);
        }

        [Test]
        public void PreExistingV1Save_LoadsWithEmptyChronicle()
        {
            // A save written before issue #7 has no chronicle field.
            // Add-only field (ADR-0001): loads with the default empty list.
            string legacyJson = "{\"schemaVersion\":1,\"aseMantissa\":0.0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0.0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1,\"lastSaveTimestamp\":0," +
                                "\"generationStartTimestamp\":0,\"seenFlags\":0," +
                                "\"council\":[]," +
                                "\"lineage\":{\"permanentAseBonus\":0.0,\"generationCount\":0}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.chronicle,
                "chronicle must not be null — default is an empty list (ADR-0001)");
            Assert.IsEmpty(restored.chronicle,
                "legacy save without chronicle loads with empty list");
        }

        [Test]
        public void Chronicle_NullRemembrance_RoundTrips()
        {
            // Serialization safety: remembrance is a reference type and defaults
            // to null when not set; the screen renders null as "—".
            var save = new SaveData();
            save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = 1,
                chosenOri = -1,
                didAscend = false,
                remembrance = null,
                completedTimestamp = 1_781_000_000L,
            });

            var restored = RoundTrip(save);

            Assert.IsNull(restored.chronicle[0].remembrance,
                "null remembrance survives round-trip (Chronicle handles null gracefully)");
        }

        // ---- Forebear compounding (issue #8) ----

        [Test]
        public void ChronicleEntry_ForebearCrossroadsId_RoundTrips()
        {
            var save = new SaveData();
            save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = 1,
                forebearCrossroadsId = "card_b",
            });
            save.chronicle.Add(new ChronicleEntry
            {
                generationNumber = 2,
                forebearCrossroadsId = "", // faithful life — no forebear deed
            });

            var restored = RoundTrip(save);

            Assert.AreEqual("card_b", restored.chronicle[0].forebearCrossroadsId,
                "forebearCrossroadsId survives a JSON round-trip");
            Assert.AreEqual("", restored.chronicle[1].forebearCrossroadsId,
                "empty forebearCrossroadsId (faithful life) survives round-trip");
        }

        [Test]
        public void LegacyChronicleEntry_LoadsWithNullForebearCrossroadsId()
        {
            // A save written before issue #8 has no forebearCrossroadsId on ChronicleEntry.
            // Add-only field (ADR-0001): loads as null; CrossroadsSystem treats null == empty.
            string legacyJson = "{\"schemaVersion\":1,\"aseMantissa\":0,\"aseExponent\":0," +
                                "\"asePerSecondMantissa\":0,\"asePerSecondExponent\":0," +
                                "\"currentStage\":0,\"currentPath\":-1,\"lastSaveTimestamp\":0," +
                                "\"generationStartTimestamp\":0,\"seenFlags\":0,\"council\":[]," +
                                "\"chronicle\":[{\"generationNumber\":1,\"chosenOri\":0," +
                                "\"didAscend\":false,\"remembrance\":\"The Wavering\",\"completedTimestamp\":1781000000}]," +
                                "\"lineage\":{\"permanentAseBonus\":0,\"generationCount\":1}}";

            var restored = SaveSerializer.FromJson(legacyJson);

            Assert.AreEqual(1, restored.chronicle.Count);
            Assert.IsTrue(string.IsNullOrEmpty(restored.chronicle[0].forebearCrossroadsId),
                "legacy ChronicleEntry without forebearCrossroadsId loads as null/empty (ADR-0001)");
        }
    }
}
