using NUnit.Framework;
using OriAscendant.Data;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HouseGenerator (issue #37): determinism, pool selection, bounds, and
    /// draw order (name→path→power→stance). Degradation under empty/zero inputs also covered.
    /// All doubles within 1e-12.
    /// </summary>
    public class HouseGeneratorTests
    {
        private const double Tol = 1e-12;
        private static readonly string[] Pool = { "Adé", "Bàbá" };
        private ContestConfig _config;

        [SetUp]
        public void SetUp() => _config = EditModeTestHelpers.MakeContestConfig();

        // ── Determinism ───────────────────────────────────────────────────────────

        [Test]
        public void Generate_SameSequence_IdenticalHouse()
        {
            var rng1 = new FakeRandom(0.1, 0.2, 0.3, 0.4);
            var rng2 = new FakeRandom(0.1, 0.2, 0.3, 0.4);
            var h1 = HouseGenerator.Generate(Pool, 3, _config, rng1);
            var h2 = HouseGenerator.Generate(Pool, 3, _config, rng2);

            Assert.AreEqual(h1.Name,       h2.Name);
            Assert.AreEqual(h1.PathIndex,  h2.PathIndex);
            Assert.AreEqual(h1.PowerRatio, h2.PowerRatio, Tol);
            Assert.AreEqual(h1.Stance,     h2.Stance);
        }

        // ── Draw order: name → path → power → stance ─────────────────────────────

        [Test]
        public void Generate_DrawOrder_NameThenPathThenPowerThenStance()
        {
            // Pool = {"Adé","Bàbá"}, pathCount=3
            // roll[0]=0.0 → name index Min(0,1)=0 → "Adé"
            // roll[1]=0.9 → path index Min(2,2)=2
            // roll[2]=0.5 → power = 0.75 + 0.5*0.50 = 1.0
            // roll[3]=0.4 → stance index Min(1,2)=1 → Endure
            var house = HouseGenerator.Generate(Pool, 3, _config, new FakeRandom(0.0, 0.9, 0.5, 0.4));

            Assert.AreEqual("Adé",        house.Name);
            Assert.AreEqual(2,            house.PathIndex);
            Assert.AreEqual(1.0,          house.PowerRatio, Tol);
            Assert.AreEqual(Stance.Endure, house.Stance);
        }

        // ── Name selection ────────────────────────────────────────────────────────

        [Test]
        public void Generate_NameFromPool_FirstEntry()
        {
            // roll 0.0 → index 0 → "Adé"
            var house = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            Assert.AreEqual("Adé", house.Name);
        }

        [Test]
        public void Generate_NameFromPool_SecondEntry()
        {
            // roll 0.5 → index Min(1,1)=1 → "Bàbá"
            var house = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.5, 0.0, 0.0, 0.0));
            Assert.AreEqual("Bàbá", house.Name);
        }

        // ── PathIndex bounds ──────────────────────────────────────────────────────

        [Test]
        public void Generate_PathIndex_WithinRange()
        {
            // Check all three paths with pathCount=3
            for (int i = 0; i < 3; i++)
            {
                double roll = i / 3.0; // 0.0, 0.33, 0.66 → indices 0,1,2
                var house = HouseGenerator.Generate(Pool, 3, _config, new FakeRandom(0.0, roll, 0.0, 0.0));
                Assert.GreaterOrEqual(house.PathIndex, 0);
                Assert.Less(house.PathIndex, 3);
            }
        }

        // ── PowerRatio bounds ─────────────────────────────────────────────────────

        [Test]
        public void Generate_PowerRatio_WithinConfigRange()
        {
            // roll 0.0 → min; roll 0.999 → near max
            var houseMin = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            var houseMax = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.999, 0.0));

            Assert.AreEqual(_config.housePowerMin, houseMin.PowerRatio, Tol);
            Assert.GreaterOrEqual(houseMax.PowerRatio, _config.housePowerMin);
            Assert.LessOrEqual(houseMax.PowerRatio, _config.housePowerMax);
        }

        // ── Stance bounds ─────────────────────────────────────────────────────────

        [Test]
        public void Generate_Stance_AllThreeReachable()
        {
            // rolls 0.0, 0.4, 0.7 → indices 0,1,2 → Strike, Endure, Flow
            var s0 = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            var s1 = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.4));
            var s2 = HouseGenerator.Generate(Pool, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.7));

            Assert.AreEqual(Stance.Strike, s0.Stance);
            Assert.AreEqual(Stance.Endure, s1.Stance);
            Assert.AreEqual(Stance.Flow,   s2.Stance);
        }

        // ── Degradation ───────────────────────────────────────────────────────────

        [Test]
        public void Generate_EmptyNamePool_ReturnsEmptyName()
        {
            var house = HouseGenerator.Generate(new string[0], 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            Assert.AreEqual("", house.Name);
        }

        [Test]
        public void Generate_NullNamePool_ReturnsEmptyName()
        {
            var house = HouseGenerator.Generate(null, 1, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            Assert.AreEqual("", house.Name);
        }

        [Test]
        public void Generate_ZeroPathCount_ReturnsPathIndexZero()
        {
            var house = HouseGenerator.Generate(Pool, 0, _config, new FakeRandom(0.0, 0.0, 0.0, 0.0));
            Assert.AreEqual(0, house.PathIndex);
        }
    }
}
