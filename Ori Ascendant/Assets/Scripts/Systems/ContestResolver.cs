using OriAscendant.Data;

namespace OriAscendant.Systems
{
    /// <summary>The three contest stances. Triangle (issue #37): Strike ▸ Endure ▸ Flow ▸ Strike —
    /// Strike beats Endure, Endure beats Flow, Flow beats Strike. Direction is tunable in #40.</summary>
    public enum Stance { Strike, Endure, Flow }

    /// <summary>The resolved clash. RenownDelta is RAW — the caller floors lineage.renown at 0
    /// when applying it (losing is sandboxed from core progression and never goes negative).</summary>
    public readonly struct ContestOutcome
    {
        public readonly bool Won;
        public readonly double RenownDelta;
        public readonly double Odds;       // the disclosed win chance this was resolved against
        public ContestOutcome(bool won, double renownDelta, double odds)
        {
            Won = won; RenownDelta = renownDelta; Odds = odds;
        }
    }

    /// <summary>
    /// Pure resolution of a Marketplace contest (issue #37). ComputeOdds is shown to the player
    /// BEFORE they commit; Resolve rolls against that exact disclosed value (no recompute drift,
    /// no near-miss — honest design). Win pays more the worse the odds you accepted (risk-reward);
    /// a loss is softer and, once floored at the apply site, never costs core progression.
    /// </summary>
    public static class ContestResolver
    {
        /// <summary>+1 if a beats b, -1 if b beats a, 0 on a tie. Strike▸Endure▸Flow▸Strike.</summary>
        public static int StanceCompare(Stance a, Stance b)
        {
            if (a == b) return 0;
            bool aWins = (a == Stance.Strike && b == Stance.Endure)
                      || (a == Stance.Endure && b == Stance.Flow)
                      || (a == Stance.Flow   && b == Stance.Strike);
            return aWins ? 1 : -1;
        }

        /// <summary>Disclosed win chance: 0.5, tilted by the stance triangle and the power gap
        /// (a weaker house — ratio &lt; 1 — favors the player), clamped to [oddsMin, oddsMax].</summary>
        public static double ComputeOdds(Stance player, Stance house, double powerRatio, ContestConfig config)
        {
            double odds = 0.5
                + StanceCompare(player, house) * config.stanceTilt
                + config.powerWeight * (1.0 - powerRatio);
            return System.Math.Min(config.oddsMax, System.Math.Max(config.oddsMin, odds));
        }

        /// <summary>Rolls once against the disclosed odds. Stake scales with the odds accepted
        /// (worse odds → bigger swing); a loss is softened by config.lossSoftness.</summary>
        public static ContestOutcome Resolve(double odds, ContestConfig config, IRandomSource random)
        {
            bool won = random.NextDouble() < odds;
            double stake = config.stakeBase * (1.0 - odds);
            double delta = won ? stake : -stake * config.lossSoftness;
            return new ContestOutcome(won, delta, odds);
        }
    }
}
