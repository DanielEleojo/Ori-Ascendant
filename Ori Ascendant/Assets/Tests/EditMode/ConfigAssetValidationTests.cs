using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using UnityEditor;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate B: the BUILT assets must match GAMEPLAY §2 exactly — these values are
    /// playtest-locked (/Resources/StageConfigs/ off-limits rule). Guards against
    /// SceneBuilder drift and hand-edits alike. Loads via AssetDatabase (editor
    /// tests), so the suite fails loudly if the builder hasn't produced an asset.
    /// </summary>
    public class ConfigAssetValidationTests
    {
        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"missing built asset: {path} — run SceneBuilder.BuildAll");
            return asset;
        }

        private static CultivationStageConfig Stage(int displayNumber) =>
            Load<CultivationStageConfig>($"Assets/Resources/StageConfigs/Stage{displayNumber}.asset");

        [Test]
        public void StageTable_MatchesGameplaySpec()
        {
            string[] names = { "Ọmọ Ayé", "Akẹ́kọ̀ọ́", "Awo", "Aláàṣẹ", "Àgbà", "Aṣẹ́gun" };
            double[] multipliers = { 1, 5, 20, 80, 320, 1250 };
            double[] thresholds = { 100, 1500, 5500, 100000, 750000 }; // advance-out, idx 0–4
            int[] tiers = { 0, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 6; i++)
            {
                var stage = Stage(i + 1);
                Assert.AreEqual(names[i], stage.stageName, $"Stage{i + 1} name");
                Assert.AreEqual(multipliers[i], stage.productionMultiplier, $"Stage{i + 1} multiplier");
                Assert.AreEqual(tiers[i], stage.tier, $"Stage{i + 1} tier");
                if (i < 5)
                {
                    Assert.AreEqual(BigNumber.FromDouble(thresholds[i]), stage.GetAdvanceThreshold(),
                        $"Stage{i + 1} advance threshold");
                }
            }
        }

        [Test]
        public void StageThresholds_StrictlyAscending()
        {
            for (int i = 1; i < 5; i++)
            {
                Assert.IsTrue(Stage(i + 1).GetAdvanceThreshold() > Stage(i).GetAdvanceThreshold(),
                    $"threshold of Stage{i + 1} must exceed Stage{i}");
            }
        }

        [Test]
        public void PathTable_MatchesGameplaySpec()
        {
            var ane = Load<PathConfig>("Assets/Resources/PathConfigs/Ane.asset");
            Assert.AreEqual(1.0, ane.aseGenerationModifier);
            Assert.AreEqual(1.5, ane.offlineRateModifier);
            Assert.AreEqual(1.0, ane.councilBonusModifier);
            Assert.AreEqual(TribulationType.Earth, ane.tribulationType);
            Assert.IsNotEmpty(ane.offlineBonusLabel, "Ane needs the Welcome-Back itemized label");

            var sango = Load<PathConfig>("Assets/Resources/PathConfigs/Sango.asset");
            Assert.AreEqual(2.0, sango.aseGenerationModifier);
            Assert.AreEqual(0.5, sango.offlineRateModifier);
            Assert.AreEqual(1.0, sango.councilBonusModifier);
            Assert.AreEqual(1.0, sango.aseGenerationModifier * sango.offlineRateModifier,
                "Sango net offline must normalize to ×1.0 — the storm sleeps");
            Assert.AreEqual(TribulationType.Storm, sango.tribulationType);

            var osun = Load<PathConfig>("Assets/Resources/PathConfigs/Osun.asset");
            Assert.AreEqual(1.0, osun.aseGenerationModifier);
            Assert.AreEqual(1.0, osun.offlineRateModifier);
            Assert.AreEqual(2.0, osun.councilBonusModifier);
            Assert.AreEqual(TribulationType.River, osun.tribulationType);

            foreach (var path in new[] { ane, sango, osun })
            {
                Assert.IsNotEmpty(path.pathName);
                Assert.IsNotEmpty(path.identityLine, $"{path.pathName}: card stat line required");
                Assert.IsNotEmpty(path.traditionLabel, $"{path.pathName}: tradition label is a cultural red-line requirement");
                Assert.IsNotEmpty(path.hookBadge, $"{path.pathName}: HUD badge required");
            }
        }

        [Test]
        public void TribulationConfig_MatchesGameplaySpec()
        {
            var config = Load<TribulationConfig>("Assets/Configs/TribulationConfig.asset");
            Assert.AreEqual(0.60, config.baseAscendChance, "retained as the documented midpoint anchor (ADR-0004)");
            Assert.AreEqual(0.25, config.ascendFloor, "ADR-0004 steadfastness floor");
            Assert.AreEqual(0.90, config.ascendCeiling, "ADR-0004 steadfastness ceiling");
            Assert.AreEqual(new BigNumber(25.0, 6), config.GetAseThreshold(), "25M capstone gate");
            CollectionAssert.AreEqual(new[] { 0.5f, 0.8f, 1.0f }, config.ambientFractions);
            Assert.AreEqual(0.8, config.holdToConfirmSeconds);
            // Ceremony beats (GAMEPLAY §3.5 timing table).
            Assert.AreEqual(2.0f, config.transitionSeconds);
            Assert.AreEqual(3, config.stormWaveCount);
            Assert.AreEqual(1.0f, config.stormWaveIntervalSeconds);
            Assert.AreEqual(1.5f, config.silenceHoldSeconds);
            Assert.AreEqual(2.5f, config.revealSeconds);
            Assert.AreEqual(2.5f, config.ancestorCardSeconds);
            Assert.AreEqual(2.0f, config.finalBeatSeconds);
        }

        [Test]
        public void CouncilConfig_MatchesGameplaySpec()
        {
            var config = Load<CouncilConfig>("Assets/Configs/CouncilConfig.asset");
            Assert.AreEqual(0.25, config.ancestorBaseBonus, "W");
            Assert.AreEqual(5, config.maxCouncil);
        }

        [Test]
        public void OriConfig_HasASeedVirtueSet()
        {
            // Dynasty PRD Phase 1 (slice 1): seed virtue set ships pre-§7.10 with
            // placeholder copy. Phase 5 swaps the content for native-speaker-vetted
            // text but the shape (non-empty list, each entry named) is the contract.
            var config = Load<OriConfig>("Assets/Configs/OriConfig.asset");
            Assert.IsNotNull(config.virtues, "OriConfig must define a virtue list");
            Assert.GreaterOrEqual(config.Count, 2,
                "the choice needs to feel like a choice — at least two virtues");
            foreach (var virtue in config.virtues)
            {
                Assert.IsNotEmpty(virtue.virtueName, "every Ori virtue needs a display name");
                Assert.IsNotEmpty(virtue.vowLine, "every Ori virtue needs a vow line");
            }
        }

        [Test]
        public void CrossroadsConfig_HasASeedDeck()
        {
            // Dynasty PRD Phase 1 (slice 2a): seed deck ships pre-§7.10. The field
            // shape (non-empty deck, each card with id + prompt + at least 2 options,
            // each option with text) is the contract; final content is the review pass.
            var config = Load<CrossroadsConfig>("Assets/Configs/CrossroadsConfig.asset");
            Assert.IsNotNull(config.deck, "CrossroadsConfig must define a deck");
            Assert.GreaterOrEqual(config.DeckSize, 1, "the deck needs at least one card");
            Assert.IsTrue(config.GetMilestone() > BigNumber.Zero,
                "milestone must be positive — a zero milestone fires immediately");
            foreach (var card in config.deck)
            {
                Assert.IsNotEmpty(card.id, "every crossroads card needs a unique id");
                Assert.IsNotEmpty(card.prompt, "every crossroads card needs a prompt");
                Assert.IsNotNull(card.options);
                Assert.GreaterOrEqual(card.options.Length, 2,
                    $"card '{card.id}' must offer at least 2 options so it is a real dilemma");
                foreach (var option in card.options)
                {
                    Assert.IsNotEmpty(option.optionText,
                        $"every option in card '{card.id}' needs display text");
                }
            }
        }

        [Test]
        public void RemembranceConfig_HasNamePoolAndFaithfulFallLine()
        {
            // Dynasty PRD slice 4a: seed content is placeholder (pre-§7.10).
            // The shape — non-empty pool, non-empty faithful-fall line — is the contract.
            var config = Load<RemembranceConfig>("Assets/Configs/RemembranceConfig.asset");
            Assert.IsNotNull(config.personalNames, "RemembranceConfig must define a personal-name pool");
            Assert.GreaterOrEqual(config.personalNames.Length, 1,
                "at least one personal name is required to form a Title");
            foreach (var name in config.personalNames)
            {
                Assert.IsNotEmpty(name, "every personal name in the pool must be non-empty");
            }
            Assert.IsNotEmpty(config.faithfulFallLine,
                "faithfulFallLine is the dignified fallback for lives that held their Ori vow");
        }

        [Test]
        public void CrossroadsDeckConfig_HasBeatsWithEpithets()
        {
            // Dynasty PRD slice 4a: each beat carries a fallenEpithet (placeholder pre-§7.10).
            // The shape — non-empty deck, non-empty epithet on every beat — is the contract.
            var config = Load<CrossroadsDeckConfig>("Assets/Configs/CrossroadsDeckConfig.asset");
            Assert.IsNotNull(config.beats, "CrossroadsDeckConfig must define a beats array");
            Assert.GreaterOrEqual(config.Count, 1,
                "the deck must contain at least one beat");
            foreach (var beat in config.beats)
            {
                Assert.IsNotNull(beat, "beats array must not contain null entries");
                Assert.IsNotEmpty(beat.fallenEpithet,
                    "every beat must supply a fallenEpithet — the Defining Deed Nickname");
            }
        }
    }
}
