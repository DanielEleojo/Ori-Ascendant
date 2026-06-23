namespace OriAscendant.Systems
{
    /// <summary>
    /// Pure mapping from accumulated lineage renown (SaveData.lineage.renown) to its
    /// CAPPED contribution to the production rate (GAMEPLAY §2.1, the 7th additive term).
    /// Renown itself is uncapped — it will drive marketplace standing — but its Àṣẹ bonus
    /// is capped so contending is never mandatory. The bonus enters the rate OUTSIDE the
    /// councilBonusModifier (Osun ×2) wrap — Osun never amplifies it, so retirement stays
    /// Àṣẹ-neutral — but it still rides the lineage factor like the council bonuses.
    /// </summary>
    public static class LineageRenown
    {
        // ponytail: plain clamp. Renown is floored at 0 at its source (loss can't push it
        // negative), so no Max(0,...) guard here — consistent with how the rate formula
        // already trusts the other save-side doubles.
        public static double ToBonus(double renown, double cap) =>
            System.Math.Min(renown, cap);
    }
}
