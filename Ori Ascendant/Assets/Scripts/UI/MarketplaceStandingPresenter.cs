namespace OriAscendant.UI
{
    /// <summary>One-line standing in the society of Houses (Ọjà, issue #39).</summary>
    public struct MarketplaceStanding
    {
        /// <summary>1 = top of the notional marketplace; higher = further down.</summary>
        public int Rank;
        /// <summary>"Renown 0.30 — 70th in the marketplace".</summary>
        public string Line;
    }

    /// <summary>
    /// Pure mapping from lineage renown to a computed marketplace standing (issue #39).
    /// No MonoBehaviour, no scene, no stored roster — the rank is DERIVED from renown alone,
    /// so the same renown always yields the same standing. Renown rises at the Crossing
    /// (#36) and in contests (#37); any refresh of this presenter shows the current standing.
    ///
    /// The marketplace size and climb rate are PLACEHOLDERS, tuned in the balance pass (#40).
    /// </summary>
    public static class MarketplaceStandingPresenter
    {
        private const int NotionalHouses = 100;     // the society you climb toward 1st (placeholder, #40)
        private const double RenownPerRank = 0.01;  // renown to climb one rank (placeholder, #40)

        public static MarketplaceStanding Map(double renown)
        {
            int climbed = renown <= 0.0 ? 0 : (int)(renown / RenownPerRank);
            int rank = System.Math.Max(1, NotionalHouses - climbed);
            return new MarketplaceStanding
            {
                Rank = rank,
                Line = $"Renown {renown:0.00} — {Ordinal(rank)} in the marketplace",
            };
        }

        /// <summary>English ordinal: 1st, 2nd, 3rd, 4th … 11th/12th/13th special-cased.</summary>
        public static string Ordinal(int n)
        {
            int mod100 = n % 100;
            if (mod100 >= 11 && mod100 <= 13) return n + "th";
            switch (n % 10)
            {
                case 1:  return n + "st";
                case 2:  return n + "nd";
                case 3:  return n + "rd";
                default: return n + "th";
            }
        }
    }
}
