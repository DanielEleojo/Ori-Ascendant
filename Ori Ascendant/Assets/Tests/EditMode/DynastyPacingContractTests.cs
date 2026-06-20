using System;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using UnityEditor;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Dynasty balance-pass contracts (issue #12, PRD §Balance Pass).
    ///
    /// Locks the simulation-derived numbers for three axes:
    ///   1. Steadfastness curve shape — floor/ceiling/midpoint (ADR-0004).
    ///   2. Crossroads milestone density — 6 milestones per life (one per stage tier),
    ///      derived so a casual idle player resolves ~2 crossroads per daily check-in.
    ///   3. Dynasty pacing — first Crossing ≤ 1 day idle; full council in ≤ 5 gens.
    ///
    /// Simulation basis: one life accumulates Àṣẹ at the GAMEPLAY §2.2 base rates for
    /// each stage (no council bonus, Ane path for the conservative offline case). The
    /// six milestones are placed at the Àṣẹ value that roughly bisects each stage's
    /// active-play time, so a crossroads surfaces in each stage of the life arc.
    ///
    /// Pure-arithmetic tests run without built assets. Asset-dependent tests load the
    /// real ScriptableObjects and fail loudly if SceneBuilder hasn't been run.
    /// </summary>
    public class DynastyPacingContractTests
    {
        // ---- Locked simulation-derived constants ----

        // ADR-0004 steadfastness curve.
        private const double Floor = 0.25;
        private const double Ceiling = 0.90;
        private const double Midpoint = 0.60;

        // Rate at which AscendChance equals Midpoint (the old flat 60% is the anchor).
        // (0.60 − 0.25) / (0.90 − 0.25) ≈ 0.5385
        private static readonly double MidpointRate = (Midpoint - Floor) / (Ceiling - Floor);

        // ADR-0005 line-legacy cap.
        private const double LineLegacyBonusPerGen = 0.05;
        private const double LineLegacyMaxBonus = 0.15;

        private const double TribulationGate = 25_000_000;

        // Balance-pass milestone placement: one per stage (derived, issue #12).
        // First milestone stays at 100 (the Stage 1 boundary — earliest hook).
        // Extra milestones fire mid-way through each subsequent stage so a crossroads
        // appears throughout the climb, not all at the start or end.
        private static readonly double[] AllMilestonesAse =
        {
            100,         // Stage 1 boundary (first milestone in CrossroadsConfig)
            1_000,       // Stage 2 — midpoint active time in Stage 2
            4_000,       // Stage 3 — midpoint active time in Stage 3
            50_000,      // Stage 4 — near midpoint of Stage 4
            400_000,     // Stage 5 — near midpoint of Stage 5
            5_000_000,   // Stage 6 — early in the Stage 6 grind (arms steadfastness before Tribulation)
        };

        // ---- §1: Steadfastness curve shape ----

        [Test]
        public void SteadfastnessCurve_AtZeroRate_IsFloor()
        {
            // A fully-wavering life (held=0 at every crossroads) still earns the floor chance.
            double chance = Floor + (Ceiling - Floor) * 0.0;
            Assert.AreEqual(Floor, chance, 1e-12,
                "ADR-0004: rate=0 (fully wavering) must land on the floor — the river keeps a sliver of mercy");
        }

        [Test]
        public void SteadfastnessCurve_AtFullRate_IsCeiling()
        {
            // A perfectly steadfast life earns the ceiling — not certainty.
            double chance = Floor + (Ceiling - Floor) * 1.0;
            Assert.AreEqual(Ceiling, chance, 1e-12,
                "ADR-0004: rate=1 (fully faithful) must land on the ceiling — steadfast, not invincible");
        }

        [Test]
        public void SteadfastnessCurve_AtMidpointRate_IsDocumentedBaseChance()
        {
            // The old flat 60% is the midpoint anchor (ADR-0004). The steadfastness rate
            // at which the new curve hits 60% is (0.60-0.25)/(0.90-0.25) ≈ 0.538.
            double chance = Floor + (Ceiling - Floor) * MidpointRate;
            Assert.AreEqual(Midpoint, chance, 1e-12,
                "at the midpoint steadfastness rate the curve must pass through the documented 60% base anchor");
        }

        [Test]
        public void SteadfastnessCurve_BandWidth_IsWideEnoughToMatter()
        {
            // The spread (ceiling − floor = 0.65) ensures there is a meaningful visible
            // difference between a wavering and a faithful life. At exactly 3-of-6
            // crossroads faithful (the midpoint sample), the odds are 57.5%; a single
            // extra crossroads held (4-of-6) pushes them to 68.3% — a +10.8pp swing.
            const double rMid = 3.0 / 6.0;
            const double rHigh = 4.0 / 6.0;
            double midChance = Floor + (Ceiling - Floor) * rMid;
            double highChance = Floor + (Ceiling - Floor) * rHigh;
            double swing = highChance - midChance;
            Assert.AreEqual(0.575, midChance, 1e-9, "3-of-6 faithful → 57.5%");
            Assert.AreEqual(0.6833, highChance, 0.001, "4-of-6 faithful → ~68.3%");
            Assert.Greater(swing, 0.05,
                "each crossroads choice must move the odds by more than 5pp — choices must feel consequential");
        }

        // ---- §2: ADR-0005 ceiling cap ----

        [Test]
        public void LineLegacyCap_MaxBonusAtCeiling_NeverExceedsCeiling()
        {
            // Even with the max line-legacy bonus on top of a ceiling-rate life, the
            // result must be clamped to the ceiling. No single "inflation of a bland life."
            double raw = Ceiling + LineLegacyMaxBonus; // 0.90 + 0.15 = 1.05 unclamped
            double clamped = Math.Min(raw, Ceiling);
            Assert.AreEqual(Ceiling, clamped, 1e-12,
                "ADR-0005: max line-legacy at a steadfast-ceiling life must still clamp to 0.90");
        }

        [Test]
        public void LineLegacyCap_MaxBonusAtFloor_LiftsBelowCeiling()
        {
            // A wavering life in a multi-gen faithful dynasty gets a meaningful lift,
            // but the lift must stay below the ceiling so the Crossing stays a real crossing.
            double withBonus = Math.Min(Floor + LineLegacyMaxBonus, Ceiling);
            Assert.AreEqual(0.40, withBonus, 1e-9,
                "floor + max-legacy = 0.40 — wavering life in a steadfast dynasty still has real risk");
            Assert.Less(withBonus, Ceiling,
                "floor + max-legacy must stay below ceiling — even a dynasty of saints isn't certain");
        }

        [Test]
        public void LineLegacyCap_MaxBonusIsThreeGenStreak()
        {
            // Max bonus (0.15) divided by per-gen bonus (0.05) = 3: the cap is the equivalent
            // of exactly three consecutive faithful generations — bounded by design (ADR-0005).
            double gensToMax = LineLegacyMaxBonus / LineLegacyBonusPerGen;
            Assert.AreEqual(3.0, gensToMax, 1e-9,
                "the line-legacy cap is worth exactly 3 consecutive faithful gens — easy to communicate, bounded");
        }

        // ---- §3: Crossroads milestone density ----

        [Test]
        public void CrossroadsMilestones_SixPerLife_OnePerStage()
        {
            Assert.AreEqual(6, AllMilestonesAse.Length,
                "balance-pass derived: 6 milestones — one per stage, so a crossroads surfaces throughout the life arc");
        }

        [Test]
        public void CrossroadsMilestones_AreStrictlyAscending()
        {
            for (int i = 1; i < AllMilestonesAse.Length; i++)
            {
                Assert.Greater(AllMilestonesAse[i], AllMilestonesAse[i - 1],
                    $"milestones must be strictly ascending: [{i}]={AllMilestonesAse[i]} > [{i-1}]={AllMilestonesAse[i-1]}");
            }
        }

        [Test]
        public void CrossroadsMilestones_EachFiresInDistinctStage()
        {
            // Map each milestone to the stage it fires in (0-indexed), then assert each
            // milestone[i] belongs to stage i. Uses strict classification:
            //   Stage 0 (Ọmọ Ayé):  ase ≤ 100
            //   Stage 1 (Akẹ́kọ̀ọ́): ase ≤ 1 500
            //   Stage 2 (Awo):      ase ≤ 5 500
            //   Stage 3 (Aláàṣẹ):  ase ≤ 100 000
            //   Stage 4 (Àgbà):    ase ≤ 750 000
            //   Stage 5 (Aṣẹ́gun):  ase < 25 000 000
            for (int i = 0; i < AllMilestonesAse.Length; i++)
            {
                int stage = StageIndexFor(AllMilestonesAse[i]);
                Assert.AreEqual(i, stage,
                    $"milestone[{i}]={AllMilestonesAse[i]:N0} must fire in stage {i} (not {stage})");
            }
        }

        [Test]
        public void CrossroadsMilestones_AllBelowTribulationGate()
        {
            foreach (double m in AllMilestonesAse)
            {
                Assert.Less(m, TribulationGate,
                    $"milestone {m:N0} must fire before the 25M tribulation gate — crossroads are life-road events, not capstones");
            }
        }

        // ---- §4: First-Crossing pacing ----

        [Test]
        public void FirstCrossing_AneOfflineNight_ArmsTheTribulationGate()
        {
            // Stage-6 rate (Ane path, gen-1, no council): 1 × 1250 × 1.0 = 1250 Àṣẹ/s.
            // Offline modifier 1.5. 8h cap = 28 800s.
            // Banked = 1250 × 1.5 × 28800 = 54 000 000 > 25M gate.
            const double stage6RateAne = 1250.0 * 1.0; // ×1.0 online modifier, no council
            const double offlineModAne = 1.5;
            const double offlineCap = 28_800.0;
            double banked = stage6RateAne * offlineModAne * offlineCap;
            Assert.Greater(banked, TribulationGate,
                "Ane: one overnight at Stage 6 banks 54M — first Crossing is ready in the morning (≤ 1 day idle)");
        }

        [Test]
        public void FirstCrossing_SangoOfflineNight_ArmsTheTribulationGate()
        {
            // Sango: online=×2 (cached rate = 2500/s), offline modifier=×0.5 (net ×1.0).
            // Banked = 2500 × 0.5 × 28800 = 36 000 000 > 25M gate.
            const double stage6RateSango = 1250.0 * 2.0; // ×2.0 online, cached at last save
            const double offlineModSango = 0.5;
            const double offlineCap = 28_800.0;
            double banked = stage6RateSango * offlineModSango * offlineCap;
            Assert.Greater(banked, TribulationGate,
                "Sango: even with ×0.5 offline the storm banks 36M — first Crossing within a day for all paths");
        }

        [Test]
        public void FirstCrossing_OsunOfflineNight_ArmsTheTribulationGate()
        {
            // Osun: online=×1.0, offline modifier=×1.0 (river path; council bonus ×2 is a
            // multiplier on the lineage term, not the base rate, so gen-1 with no council is
            // the conservative case). Banked = 1250 × 1.0 × 28800 = 36 000 000 > 25M gate.
            const double stage6RateOsun = 1250.0 * 1.0; // ×1.0 online, no council gen-1
            const double offlineModOsun = 1.0;
            const double offlineCap = 28_800.0;
            double banked = stage6RateOsun * offlineModOsun * offlineCap;
            Assert.Greater(banked, TribulationGate,
                "Osun: river path banks 36M overnight — first Crossing within a day for all three paths");
        }

        // ---- §5: Dynasty pacing (weeks) ----

        [Test]
        public void DynastyWeeks_FullAscendedCouncil_RateMoreThanDoubles()
        {
            // 5 ascended ancestors in council (W=0.25, bonusMultiplier=1.0, non-Osun path):
            // lineageFactor = 1 + 0.25 × 5 × 1.0 = 2.25 — more than doubling the base rate.
            const double W = 0.25;
            const int councilSize = 5;
            double lineageFactor = 1.0 + W * councilSize * 1.0;
            Assert.AreEqual(2.25, lineageFactor, 1e-9,
                "full ascended council gives a 2.25× rate multiplier — dynasty is meaningfully stronger");
            Assert.Greater(lineageFactor, 2.0,
                "a full council must at least double the rate — visible compounding keeps the dynasty engaging");
        }

        [Test]
        public void DynastyWeeks_FifthGeneration_IsAtLeastHalfFasterThanFirst()
        {
            // Gen 5 enters with 4 ascended ancestors. Tier-0 (stages 1–3, path-less) time:
            // Base: 100 + 280 + 200 = 580s. Gen 5 factor: 1 + 4×0.25 = 2.0 → 290s.
            const double W = 0.25;
            const double tier0BaseSeconds = 100.0 + 280.0 + 200.0; // GAMEPLAY §2.4
            double gen5Factor = 1.0 + 4 * W;
            double tier0Gen5 = tier0BaseSeconds / gen5Factor;

            Assert.AreEqual(290.0, tier0Gen5, 0.01,
                "gen 5 clears Tier-0 in 290s (half of gen 1's 580s) — bloodline acceleration is visceral");
            Assert.Less(tier0Gen5, tier0BaseSeconds * 0.6,
                "gen 5 must clear Tier-0 at least 40% faster than gen 1 — dynasty visibly accelerates over weeks");
        }

        [Test]
        public void DynastyWeeks_SixCrossroadsPerLife_SteadfastnessSampleIsReliable()
        {
            // With 6 crossroads per life, the steadfastness tally (held/trials) is
            // a sample of 6 data points. At the midpoint (3/6 = 0.5), the AscendChance
            // is 0.575 — close enough to the 0.60 anchor to feel "about even odds."
            // At 4/6 (a single drift above midpoint), chance rises to 0.683 — noticeably better.
            const int crossroadsPerLife = 6;
            double midpointChance = Floor + (Ceiling - Floor) * (3.0 / crossroadsPerLife);
            Assert.AreEqual(0.575, midpointChance, 1e-9,
                "3-of-6 steadfastness → 57.5% ascend chance — honest midpoint sample, close to the 60% anchor");
            double aboveMidChance = Floor + (Ceiling - Floor) * (4.0 / crossroadsPerLife);
            Assert.Greater(aboveMidChance, midpointChance + 0.05,
                "4-of-6 steadfastness must be noticeably better than 3-of-6 — each crossroads choice matters");
        }

        // ---- §6: Config asset guards (require built assets) ----

        [Test]
        public void CrossroadsConfig_HasSixMilestones()
        {
            var config = AssetDatabase.LoadAssetAtPath<CrossroadsConfig>("Assets/Configs/CrossroadsConfig.asset");
            Assert.IsNotNull(config, "CrossroadsConfig.asset missing — run SceneBuilder.BuildAll");

            // All 6 milestones must have been crossed at the 25M tribulation gate.
            var gate = new BigNumber(25.0, 6);
            int count = config.CountMilestonesCrossed(gate);
            Assert.AreEqual(6, count,
                "balance-pass derived: exactly 6 milestones per life — run SceneBuilder.BuildAll to regenerate");
        }

        [Test]
        public void CrossroadsConfig_FirstMilestone_IsStage1Boundary()
        {
            var config = AssetDatabase.LoadAssetAtPath<CrossroadsConfig>("Assets/Configs/CrossroadsConfig.asset");
            Assert.IsNotNull(config, "CrossroadsConfig.asset missing — run SceneBuilder.BuildAll");

            Assert.AreEqual(new BigNumber(1.0, 2), config.GetMilestone(),
                "first milestone must be 100 Àṣẹ — the Stage 1 boundary, earliest possible hook");
        }

        [Test]
        public void CrossroadsConfig_LastMilestone_IsInStage6Grind()
        {
            var config = AssetDatabase.LoadAssetAtPath<CrossroadsConfig>("Assets/Configs/CrossroadsConfig.asset");
            Assert.IsNotNull(config, "CrossroadsConfig.asset missing — run SceneBuilder.BuildAll");

            var stage6Start = new BigNumber(7.5, 5); // 750 000 Àṣẹ
            var tribGate = new BigNumber(2.5, 7);    // 25 000 000 Àṣẹ
            bool hasStage6Milestone = false;
            foreach (var m in config.GetAllMilestones())
            {
                if (m >= stage6Start && m < tribGate)
                    hasStage6Milestone = true;
            }
            Assert.IsTrue(hasStage6Milestone,
                "one milestone must fall in the Stage 6 grind (750K–25M) — the long climb needs a crossroads");
        }

        [Test]
        public void TribulationConfig_LineLegacyBounds_AreContractValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<TribulationConfig>("Assets/Configs/TribulationConfig.asset");
            Assert.IsNotNull(config, "TribulationConfig.asset missing — run SceneBuilder.BuildAll");

            Assert.AreEqual(LineLegacyBonusPerGen, config.lineLegacyBonusPerGen, 1e-12,
                "balance-pass derived: 0.05 per-generation line-legacy bonus (ADR-0005)");
            Assert.AreEqual(LineLegacyMaxBonus, config.lineLegacyMaxBonus, 1e-12,
                "balance-pass derived: 0.15 max line-legacy cap — 3-gen faithful streak, then bounded (ADR-0005)");
        }

        // ---- helpers ----

        /// <summary>Returns the 0-indexed stage index for a cumulative Àṣẹ value.
        /// Mirrors GAMEPLAY §2.2 advance thresholds: stage k ends when Àṣẹ exceeds StageStarts[k+1].</summary>
        private static int StageIndexFor(double ase)
        {
            // Stage advance thresholds (cumulative Àṣẹ to leave stage k):
            // Stage 0 → 1 at 100; Stage 1 → 2 at 1500; Stage 2 → 3 at 5500;
            // Stage 3 → 4 at 100000; Stage 4 → 5 at 750000.
            if (ase <= 100) return 0;
            if (ase <= 1_500) return 1;
            if (ase <= 5_500) return 2;
            if (ase <= 100_000) return 3;
            if (ase <= 750_000) return 4;
            return 5;
        }
    }
}
