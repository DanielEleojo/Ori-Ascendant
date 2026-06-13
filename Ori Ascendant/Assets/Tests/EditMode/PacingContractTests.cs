using NUnit.Framework;
using OriAscendant.Data;
using UnityEditor;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Makes the GAMEPLAY §2.4 pacing table EXECUTABLE against the real, built
    /// config assets — the day-7 "balance verification" as a regression gate.
    /// If anyone edits a threshold or multiplier, the pacing contract breaks
    /// here instead of silently in playtesting. Pure arithmetic from the assets
    /// (time = Δthreshold ÷ rate); the runtime accrual is proven separately in
    /// the PlayMode suite.
    /// </summary>
    public class PacingContractTests
    {
        private CultivationStageConfig[] _stages;
        private double _baseRate;
        private double _aneOnline, _sangoOnline;
        private double _w;

        [OneTimeSetUp]
        public void Load()
        {
            _stages = new CultivationStageConfig[6];
            for (int i = 0; i < 6; i++)
            {
                _stages[i] = AssetDatabase.LoadAssetAtPath<CultivationStageConfig>(
                    $"Assets/Resources/StageConfigs/Stage{i + 1}.asset");
                Assert.IsNotNull(_stages[i], $"Stage{i + 1} asset missing — run SceneBuilder");
            }
            _baseRate = AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/Configs/GameplayConfig.asset").baseRate;
            _aneOnline = AssetDatabase.LoadAssetAtPath<PathConfig>("Assets/Resources/PathConfigs/Ane.asset").aseGenerationModifier;
            _sangoOnline = AssetDatabase.LoadAssetAtPath<PathConfig>("Assets/Resources/PathConfigs/Sango.asset").aseGenerationModifier;
            _w = AssetDatabase.LoadAssetAtPath<CouncilConfig>("Assets/Configs/CouncilConfig.asset").ancestorBaseBonus;
        }

        private double Threshold(int i) => _stages[i].GetAdvanceThreshold().ToDouble();

        /// <summary>Seconds to clear stage i. Path online multiplier applies only
        /// at tier 1 (index >= 3); council factor applies throughout the gen.</summary>
        private double ClearSeconds(int i, double pathOnline, double councilFactor)
        {
            double prev = i == 0 ? 0.0 : Threshold(i - 1);
            double delta = Threshold(i) - prev;
            double po = i >= 3 ? pathOnline : 1.0;
            double rate = _baseRate * _stages[i].productionMultiplier * po * councilFactor;
            return delta / rate;
        }

        private double ReachStage6Seconds(double pathOnline, double councilFactor)
        {
            double t = 0;
            for (int i = 0; i < 5; i++) t += ClearSeconds(i, pathOnline, councilFactor);
            return t;
        }

        [Test]
        public void FirstAdvance_LandsInsideTheFirstTwoMinuteSession()
        {
            double first = ClearSeconds(0, 1.0, 1.0);
            Assert.AreEqual(100.0, first, 0.01, "Stage 1 should clear in 100s at 1.0/s");
            Assert.LessOrEqual(first, 120.0, "must be affordable within a 2-minute check-in");
        }

        [Test]
        public void PathGate_OpensUnderTenMinutes()
        {
            // The verifier's one-number fix (stage-3 threshold 8000 -> 5500): Tier 1
            // entry must be inside the PRD's 10-minute path-differentiation window.
            double gate = ClearSeconds(0, 1, 1) + ClearSeconds(1, 1, 1) + ClearSeconds(2, 1, 1);
            Assert.AreEqual(580.0, gate, 0.01, "path gate at 9m40s");
            Assert.Less(gate, 600.0, "Tier 1 (where paths bite) must open under 10 minutes");
        }

        [Test]
        public void Tier0_IsPathIndependent()
        {
            // Stages 1-3 are path-less: choosing Sango vs Ane cannot change them.
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(ClearSeconds(i, _aneOnline, 1.0), ClearSeconds(i, _sangoOnline, 1.0), 1e-9,
                    $"stage {i} must be identical regardless of path");
            }
        }

        [Test]
        public void ReachStage6_MatchesTheVerifiedTable()
        {
            // GAMEPLAY §2.4: Ane/Osun (~63 min), Sango (~36 min).
            double ane = ReachStage6Seconds(_aneOnline, 1.0);
            double sango = ReachStage6Seconds(_sangoOnline, 1.0);
            Assert.AreEqual(3792.5, ane, 1.0, "Ane/Osun reach Stage 6 at ~63 min");
            Assert.AreEqual(2186.25, sango, 1.0, "Sango reaches Stage 6 at ~36 min");
            Assert.Less(sango, ane, "Sango's online x2 must make it strictly faster to the peak");
        }

        [Test]
        public void Gen2Ascend_IsAboutTwentyPercentFaster()
        {
            double ascendFactor = 1.0 + _w * 1.0; // one ascended ancestor: 1.25
            Assert.AreEqual(1.25, ascendFactor, 1e-9);

            double gen1 = ReachStage6Seconds(_aneOnline, 1.0);
            double gen2 = ReachStage6Seconds(_aneOnline, ascendFactor);
            double speedup = 1.0 - gen2 / gen1;

            Assert.AreEqual(0.20, speedup, 0.001, "gen 2 must be visibly (~20%) faster after an ascension");
        }

        [Test]
        public void OvernightBank_ClearsTheTribulationGate()
        {
            // 8h offline at the Stage-6 rate must exceed the 25M gate (the designed
            // "evening + one sleep" generation; GAMEPLAY §2.4 offline check).
            double stage6Rate = _baseRate * _stages[5].productionMultiplier * _aneOnline; // x1 path, worst case
            double banked = stage6Rate * 28800.0;
            var trib = AssetDatabase.LoadAssetAtPath<TribulationConfig>("Assets/Configs/TribulationConfig.asset");
            Assert.Greater(banked, trib.GetAseThreshold().ToDouble(),
                "one 8h overnight must arm the Crossing");
        }
    }
}
