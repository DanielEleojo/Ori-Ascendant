using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate D: the cloud-vs-local conflict rule truth table (higher generation,
    /// then higher Àṣẹ, ties keep local, null cloud keeps local). Monotonic.
    /// </summary>
    public class SaveConflictResolverTests
    {
        private static SaveData Save(int generation, double ase)
        {
            var s = new SaveData();
            s.lineage.generationCount = generation;
            s.SetAse(BigNumber.FromDouble(ase));
            return s;
        }

        [Test]
        public void NullCloud_KeepsLocal()
        {
            Assert.AreEqual(ConflictWinner.Local, SaveConflictResolver.Resolve(Save(3, 100), null));
        }

        [Test]
        public void NullLocal_TakesCloud()
        {
            Assert.AreEqual(ConflictWinner.Cloud, SaveConflictResolver.Resolve(null, Save(0, 0)));
        }

        [Test]
        public void HigherGenerationWins_RegardlessOfAse()
        {
            // Cloud is a later generation but lower Àṣẹ — generation still wins.
            Assert.AreEqual(ConflictWinner.Cloud, SaveConflictResolver.Resolve(Save(2, 9_000_000), Save(3, 10)));
            Assert.AreEqual(ConflictWinner.Local, SaveConflictResolver.Resolve(Save(4, 10), Save(3, 9_000_000)));
        }

        [Test]
        public void SameGeneration_HigherAseWins()
        {
            Assert.AreEqual(ConflictWinner.Cloud, SaveConflictResolver.Resolve(Save(3, 500), Save(3, 600)));
            Assert.AreEqual(ConflictWinner.Local, SaveConflictResolver.Resolve(Save(3, 600), Save(3, 500)));
        }

        [Test]
        public void ExactTie_KeepsLocal_NoOp()
        {
            Assert.AreEqual(ConflictWinner.Local, SaveConflictResolver.Resolve(Save(3, 500), Save(3, 500)));
        }

        [Test]
        public void Pick_ReturnsTheWinningInstance()
        {
            var local = Save(1, 100);
            var cloud = Save(5, 100);
            Assert.AreSame(cloud, SaveConflictResolver.Pick(local, cloud));
            Assert.AreSame(local, SaveConflictResolver.Pick(local, null));
        }
    }
}
