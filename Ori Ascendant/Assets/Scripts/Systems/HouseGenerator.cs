using OriAscendant.Data;

namespace OriAscendant.Systems
{
    /// <summary>A generated rival House (ilé) for a Marketplace contest (issue #37).
    /// Power is RELATIVE to the player's current asePerSecond, so difficulty self-balances
    /// as the bloodline grows — there is no absolute power ladder to tune.</summary>
    public readonly struct House
    {
        public readonly string Name;
        public readonly int PathIndex;
        public readonly double PowerRatio;   // house power ÷ player power (1.0 = even)
        public readonly Stance Stance;

        public House(string name, int pathIndex, double powerRatio, Stance stance)
        {
            Name = name; PathIndex = pathIndex; PowerRatio = powerRatio; Stance = stance;
        }
    }

    /// <summary>
    /// Pure, deterministic generation of a rival House from a random source (issue #37).
    /// The same IRandomSource sequence always yields the same House — so tests pin it with
    /// a FakeRandom and production seeds it from UnityRandomSource. Draws, in order:
    /// name, path, power, stance.
    /// </summary>
    public static class HouseGenerator
    {
        public static House Generate(string[] namePool, int pathCount, ContestConfig config, IRandomSource random)
        {
            string name = (namePool != null && namePool.Length > 0)
                ? namePool[Index(random.NextDouble(), namePool.Length)]
                : "";
            int pathIndex = pathCount > 0 ? Index(random.NextDouble(), pathCount) : 0;
            double powerRatio = config.housePowerMin
                + random.NextDouble() * (config.housePowerMax - config.housePowerMin);
            var stance = (Stance)Index(random.NextDouble(), 3);
            return new House(name, pathIndex, powerRatio, stance);
        }

        // NextDouble() is contractually [0,1); the Min clamp defends against a test source
        // returning exactly 1.0 (mirrors CrossroadsSystem.DrawFromDeck).
        private static int Index(double roll, int count) =>
            System.Math.Min((int)(roll * count), count - 1);
    }
}
