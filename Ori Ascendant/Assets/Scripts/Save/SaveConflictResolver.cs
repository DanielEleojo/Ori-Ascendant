namespace OriAscendant.Save
{
    public enum ConflictWinner { Local, Cloud }

    /// <summary>
    /// The locked cloud-vs-local conflict rule (TECH_DESIGN §4): higher
    /// lineage.generationCount wins; on a tie, higher aseAmount wins; any
    /// remaining tie keeps Local (prefer the no-op, avoids needless adoption).
    /// Pure and deterministic — drives both the launch reconcile and the GameKit
    /// ResolveConflictingSavedGames path. Monotonic: progress never goes
    /// backwards across a device swap.
    /// </summary>
    public static class SaveConflictResolver
    {
        public static ConflictWinner Resolve(SaveData local, SaveData cloud)
        {
            if (cloud == null) return ConflictWinner.Local;
            if (local == null) return ConflictWinner.Cloud;

            if (cloud.lineage.generationCount != local.lineage.generationCount)
            {
                return cloud.lineage.generationCount > local.lineage.generationCount
                    ? ConflictWinner.Cloud
                    : ConflictWinner.Local;
            }

            int byAse = cloud.GetAse().CompareTo(local.GetAse());
            return byAse > 0 ? ConflictWinner.Cloud : ConflictWinner.Local;
        }

        /// <summary>Convenience: returns the winning SaveData instance.</summary>
        public static SaveData Pick(SaveData local, SaveData cloud) =>
            Resolve(local, cloud) == ConflictWinner.Cloud ? cloud : local;
    }
}
