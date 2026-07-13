using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI;
using UnityEditor;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// DEVELOPER TOOL — a balance-tuning simulation harness, not a correctness test.
    /// Fast-forwards the game's economy using the REAL, live ScriptableObject config
    /// assets (loaded read-only via <c>AssetDatabase.LoadAssetAtPath</c> — the exact
    /// same "built assets" convention <see cref="ConfigAssetValidationTests"/> already
    /// established, reused rather than reinvented) and reuses the game's own pure math
    /// (<see cref="RateCalculator"/>, <see cref="StageProgression"/>, <see cref="BigNumber"/>,
    /// <see cref="MarketplaceStandingPresenter"/>) plus the live <see cref="TribulationSystem"/>
    /// (built the same GameObject-host + injectable-<see cref="IRandomSource"/> way
    /// <c>TribulationSystemTests.cs</c> already does) so a human can eyeball the resulting
    /// curves and decide how to retune the numbers.
    ///
    /// 100% READ-ONLY: every config asset is only ever loaded and read — never mutated,
    /// never re-saved. No SaveData is persisted, no PlayerPrefs is touched. The only file
    /// this harness writes is its own throwaway CSV under Logs/ (gitignored gate-artifact
    /// convention, matching the *.log/*.xml files already written there by headless runs).
    /// </summary>
    public class BalanceSimHarness
    {
        private const int GenerationsToSimulate = 20;
        private const int RandomSeed = 1234; // fixed seed → reproducible CSV across runs

        [Test]
        public void RunBalanceSim_ExportsCsvAndPrintsSummary()
        {
            var csv = new StringBuilder();
            var summary = new StringBuilder();

            // ---- load REAL config assets, read-only ----
            CultivationStageConfig[] stages = LoadRealStages();
            TribulationConfig tribulationConfig = Load<TribulationConfig>("Assets/Configs/TribulationConfig.asset");
            GameplayConfig gameplayConfig = Load<GameplayConfig>("Assets/Configs/GameplayConfig.asset");
            PathConfig[] paths = LoadRealPaths();

            Assert.Greater(stages.Length, 0,
                "no CultivationStageConfig assets found under Assets/Resources/StageConfigs — is the project built?");
            Assert.IsNotNull(tribulationConfig, "missing built asset: Assets/Configs/TribulationConfig.asset");
            Assert.IsNotNull(gameplayConfig, "missing built asset: Assets/Configs/GameplayConfig.asset");

            summary.AppendLine($"Ori Ascendant balance sim — {stages.Length} real stage(s) found in /Resources/StageConfigs/, " +
                                $"{paths.Length} real path(s) found in /Resources/PathConfigs/.");

            // ==== CORE: per-stage wall-clock progression time ====
            csv.AppendLine("# Section A — per-stage wall-clock progression time.");
            csv.AppendLine("Section,StageIndex,StageName,Tier,RateAsePerSec,DeltaAse,TimeSeconds,TimeHuman");

            // Reuse the SAME pure StageProgression the real game constructs
            // (Assets/Scripts/Systems/CultivationSystem.cs Begin()): cumulative
            // advance-out thresholds per stage, tribulation-gated final stage.
            BigNumber[] thresholds = stages.Select(s => s.GetAdvanceThreshold()).ToArray();
            var progression = new StageProgression(thresholds, tribulationConfig.GetAseThreshold());

            // Required baseline: path-neutral (no path chosen), no council/lineage bonuses —
            // the simplest honest comparable.
            AppendStageRows(csv, summary, "Baseline(path-neutral)", stages, progression, gameplayConfig, null);

            // Stretch (cheap to include): each real Path's ONLINE multiplier, applied only to
            // tier-1 stages — tier-0 stages are always path-neutral by design (a path is chosen
            // AT the tier-0→tier-1 gate, so PathOnlineMultiplier reads 1.0 below it; see
            // CultivationSystem.cs PathOnlineMultiplier / CurrentPathConfig).
            foreach (var path in paths)
            {
                AppendStageRows(csv, summary, $"Baseline({path.pathName})", stages, progression, gameplayConfig, path.aseGenerationModifier);
            }

            // ==== STRETCH: N-generation Crossing simulation ====
            csv.AppendLine();
            csv.AppendLine("# Section B — N-generation Crossing simulation (ascend/fall via the REAL TribulationSystem + TribulationConfig).");
            csv.AppendLine("Section,Generation,DidAscend,AscendChanceUsed,RenownGranted,RenownAfter,MarketplaceRank,MarketplaceLine");

            bool reached = TryRunGenerationSim(stages, tribulationConfig, gameplayConfig, paths, csv, summary, out string skipReason);
            if (!reached)
            {
                summary.AppendLine($"[SKIPPED] N-generation Crossing sim: {skipReason}");
                csv.AppendLine($"# SKIPPED: {skipReason}");
            }

            // ---- write CSV (the harness's own throwaway artifact — nothing game-owned is touched) ----
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string logsDir = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logsDir);
            string csvPath = Path.Combine(logsDir, "balance_sim.csv");
            File.WriteAllText(csvPath, csv.ToString());

            string report = summary.ToString();
            Debug.Log(report);
            Debug.Log($"[BalanceSimHarness] CSV written to {csvPath}");
            TestContext.WriteLine(report);
            TestContext.WriteLine($"CSV written to {csvPath}");

            // Sanity-only assertion — this is a data-generation tool, not a correctness test.
            // It must never fail just because the numbers "look bad", only if the sim itself broke.
            Assert.IsTrue(File.Exists(csvPath), "CSV was not written");
            Assert.Greater(new FileInfo(csvPath).Length, 0, "CSV is empty");
        }

        // ---------------------------------------------------------------
        // Section A helpers — per-stage progression time.
        // ---------------------------------------------------------------

        private static void AppendStageRows(StringBuilder csv, StringBuilder summary, string section,
            CultivationStageConfig[] stages, StageProgression progression, GameplayConfig gameplayConfig,
            double? tier1OnlineMultiplier)
        {
            BigNumber prevTarget = BigNumber.Zero;
            double totalSeconds = 0.0;
            bool anyUnreachable = false;

            for (int i = 0; i < stages.Length; i++)
            {
                CultivationStageConfig stage = stages[i];
                BigNumber target = progression.TargetFor(i);
                BigNumber deltaAse = target - prevTarget;

                // Tier-0 stages are always path-neutral (no path is chosen yet); only tier-1
                // stages read the path's online multiplier — mirrors CultivationSystem's own rule.
                double onlineMultiplier = (tier1OnlineMultiplier.HasValue && stage.tier > 0)
                    ? tier1OnlineMultiplier.Value
                    : 1.0;

                // The REAL rate formula (GAMEPLAY §2.1) — RateCalculator.ComputeRate, unmodified.
                BigNumber rate = RateCalculator.ComputeRate(
                    baseRate: gameplayConfig.baseRate,
                    stageProductionMultiplier: stage.productionMultiplier,
                    pathOnlineMultiplier: onlineMultiplier,
                    councilBonusModifier: 1.0,   // neutral — no bonus term is nonzero below
                    permanentAseBonus: 0.0,
                    activeCouncilSum: 0.0,
                    renownBonus: 0.0);

                string timeSecondsStr, timeHumanStr;
                if (rate.IsZero)
                {
                    anyUnreachable = true;
                    timeSecondsStr = "N/A";
                    timeHumanStr = "N/A (rate is 0 — check GameplayConfig.baseRate / stage.productionMultiplier)";
                }
                else
                {
                    double seconds = (deltaAse / rate).ToDouble();
                    totalSeconds += seconds;
                    timeSecondsStr = Inv(seconds);
                    timeHumanStr = FormatHuman(seconds);
                }

                csv.AppendLine($"{section},{i + 1},\"{stage.stageName}\",{stage.tier},{Inv(rate.ToDouble())},{Inv(deltaAse.ToDouble())},{timeSecondsStr},{timeHumanStr}");
                prevTarget = target;
            }

            summary.AppendLine(anyUnreachable
                ? $"{section}: total wall-time is N/A for at least one stage (see CSV)."
                : $"{section}: total wall-time stage-1 → Tribulation-eligible = {FormatHuman(totalSeconds)}");
        }

        // ---------------------------------------------------------------
        // Section B — live TribulationSystem N-generation Crossing sim.
        // ---------------------------------------------------------------

        private static bool TryRunGenerationSim(
            CultivationStageConfig[] stages, TribulationConfig tribulationConfig, GameplayConfig gameplayConfig,
            PathConfig[] paths, StringBuilder csv, StringBuilder summary, out string skipReason)
        {
            skipReason = null;
            GameObject host = null;
            try
            {
                ServiceLocator.Clear();
                host = new GameObject("BalanceSimHost");

                // Same host-building seam TribulationSystemTests.cs already uses: real
                // MonoBehaviours, config injected via SerializedObject (EditModeTestHelpers),
                // manually registered with ServiceLocator (Awake doesn't run outside Play mode).
                var cultivation = host.AddComponent<CultivationSystem>();
                EditModeTestHelpers.InjectArray(cultivation, "_stages", stages);
                EditModeTestHelpers.InjectArray(cultivation, "_paths", paths);
                EditModeTestHelpers.Inject(cultivation, "_tribulationConfig", tribulationConfig);

                var tribulation = host.AddComponent<TribulationSystem>();
                EditModeTestHelpers.Inject(tribulation, "_config", tribulationConfig);
                EditModeTestHelpers.Inject(tribulation, "_gameplayConfig", gameplayConfig);

                ServiceLocator.Register(cultivation);
                ServiceLocator.Register(tribulation);

                var save = new SaveData();
                cultivation.Begin(save);
                tribulation.Begin(save);

                // Seeded adapter over System.Random — NOT a duplicated game formula, just an
                // IRandomSource implementation (the exact seam TribulationSystem.SetRandomSource
                // exists for), so the N-generation sim is reproducible run-to-run.
                tribulation.SetRandomSource(new SeededRandomSource(RandomSeed));

                int finalStageIndex = cultivation.StageCount - 1;
                double renown = 0.0;

                for (int gen = 1; gen <= GenerationsToSimulate; gen++)
                {
                    // Simulation shortcut (documented, not silent): instead of replaying the full
                    // Advance/steadfastness loop each generation, we arm SaveData directly at the
                    // final stage with exactly the real Tribulation Àṣẹ threshold. Steadfastness
                    // (oriHeld/oriTrials) is left at SaveData's own default (0/0), which is the
                    // real "trials==0" branch TribulationSystem.AscendChance itself falls back to
                    // (ADR-0004 floor) — so AscendChance below is the REAL config's floor value,
                    // not a made-up number. This isolates the ascend/renown/rank curve from the
                    // (separately covered, Section A) advancement-speed curve.
                    save.currentStage = finalStageIndex;
                    save.currentPath = 0; // a real Aṣẹ́gun-stage life must have chosen a path; arbitrary which (Ane) — AscendChance never reads it (ADR-0004 path-orthogonality)
                    save.SetAse(tribulationConfig.GetAseThreshold());

                    double chanceUsed = tribulation.AscendChance;
                    TribulationResult result = tribulation.Resolve();
                    if (result == null)
                    {
                        skipReason = $"TribulationSystem.Resolve() returned null at generation {gen} " +
                                      "(CanResolve() was false) — cannot continue the N-generation sim.";
                        return false;
                    }

                    renown = save.lineage.renown;
                    MarketplaceStanding standing = MarketplaceStandingPresenter.Map(renown);

                    csv.AppendLine($"Generations,{gen},{result.DidAscend},{Inv(chanceUsed)},{Inv(result.RenownGranted)},{Inv(renown)},{standing.Rank},\"{standing.Line}\"");
                }

                summary.AppendLine($"N-generation sim ({GenerationsToSimulate} gens, seed {RandomSeed}): " +
                                    $"final renown={Inv(renown)}, final standing={MarketplaceStandingPresenter.Map(renown).Line}");
                return true;
            }
            catch (Exception ex)
            {
                skipReason = $"unexpected exception: {ex.GetType().Name}: {ex.Message}";
                return false;
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                ServiceLocator.Clear();
            }
        }

        // ---------------------------------------------------------------
        // Real-asset loading (read-only) — mirrors ConfigAssetValidationTests.cs.
        // ---------------------------------------------------------------

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path);

        /// <summary>Discovers every real CultivationStageConfig under /Resources/StageConfigs/
        /// (does not assume a count) and orders them by the numeric suffix in the filename
        /// ("Stage1", "Stage2", …) so stage index in the output matches in-game stage order.</summary>
        private static CultivationStageConfig[] LoadRealStages()
        {
            const string folder = "Assets/Resources/StageConfigs";
            string[] guids = AssetDatabase.FindAssets("t:CultivationStageConfig", new[] { folder });
            var numbered = new List<(int number, CultivationStageConfig config)>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<CultivationStageConfig>(path);
                if (config == null) continue;
                Match match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"(\d+)$");
                int number = match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : int.MaxValue;
                numbered.Add((number, config));
            }
            return numbered.OrderBy(t => t.number).Select(t => t.config).ToArray();
        }

        private static PathConfig[] LoadRealPaths()
        {
            const string folder = "Assets/Resources/PathConfigs";
            string[] guids = AssetDatabase.FindAssets("t:PathConfig", new[] { folder });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<PathConfig>)
                .Where(p => p != null)
                .OrderBy(p => p.pathName, StringComparer.Ordinal)
                .ToArray();
        }

        // ---------------------------------------------------------------
        // Small formatting/adapter helpers — harness-only, no game math.
        // ---------------------------------------------------------------

        private static string Inv(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static string FormatHuman(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) return "N/A";
            long total = (long)Math.Round(seconds, MidpointRounding.AwayFromZero);
            long h = total / 3600;
            long m = (total % 3600) / 60;
            long s = total % 60;
            return $"{h}:{m:D2}:{s:D2}";
        }

        /// <summary>Seeded adapter over System.Random for TribulationSystem's injectable
        /// IRandomSource seam — an adapter, not a reimplementation of any game formula.</summary>
        private sealed class SeededRandomSource : IRandomSource
        {
            private readonly System.Random _rng;
            public SeededRandomSource(int seed) => _rng = new System.Random(seed);
            public double NextDouble() => _rng.NextDouble();
        }
    }
}
