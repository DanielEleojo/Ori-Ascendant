using System.Collections.Generic;
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
            Assert.AreEqual(0.60, config.baseAscendChance, "LOCKED 60/40 coin");
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
        public void CrossroadsDeck_MatchesSeedSpec()
        {
            // Slice 2a seed: 5 beats (one per stage-advance), each offering one option
            // per Ori so the life's vow is always a choice. PLACEHOLDER copy, pre-§7.10.
            var deck = Load<CrossroadsDeckConfig>("Assets/Configs/CrossroadsDeck.asset");
            Assert.AreEqual(5, deck.beats.Length, "seed deck is 5 beats");

            for (int i = 0; i < deck.beats.Length; i++)
            {
                var beat = deck.beats[i];
                Assert.IsNotEmpty(beat.id, $"beat {i}: id");
                Assert.IsNotEmpty(beat.prompt, $"beat {i}: prompt");
                Assert.AreEqual(4, beat.options.Length, $"beat {i}: one option per Ori");

                var oriIndices = new List<int>();
                foreach (var option in beat.options)
                {
                    Assert.IsNotEmpty(option.text, $"beat {i}: option text");
                    oriIndices.Add(option.oriIndex);
                }
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, oriIndices,
                    $"beat {i}: every Ori must be on the table (the vow is always a choice)");
            }
        }
    }
}
