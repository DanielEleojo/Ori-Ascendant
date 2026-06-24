using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Behavior coverage for the Crossing-ceremony clock + stashed outcome (issue #34).
    /// Replaces the former MainScreenSkin reflection gates that pinned the private
    /// _crossingCeremonyElapsed / _crossingCeremonyDidAscend / _crossingCeremonyPath fields —
    /// that state now lives in CeremonyDriver and is asserted here by behavior, not by name.
    /// </summary>
    public class CeremonyDriverTests
    {
        [Test]
        public void Default_IsInactive()
        {
            var d = new CeremonyDriver();
            Assert.IsFalse(d.IsActive, "A fresh driver must be inactive — no ceremony before a Crossing");
        }

        [Test]
        public void Stash_AloneDoesNotIgnite()
        {
            // OnTribulationComplete stashes while the overlay is still opaque; ignition waits for Start (#4).
            var d = new CeremonyDriver();
            d.Stash(didAscend: true, path: 1);
            bool active = d.Tick(0.016f, out _, out _, out _);
            Assert.IsFalse(active, "Stash alone must not ignite — the flash waits for the overlay to close");
            Assert.IsFalse(d.IsActive);
        }

        [Test]
        public void StartWithoutStash_IsNoOp()
        {
            var d = new CeremonyDriver();
            d.Start();
            Assert.IsFalse(d.IsActive, "Start with nothing stashed must be a no-op");
        }

        [Test]
        public void StashThenStart_Ignites()
        {
            var d = new CeremonyDriver();
            d.Stash(true, 2);
            d.Start();
            Assert.IsTrue(d.IsActive, "Stash + Start must ignite the ceremony");
            bool active = d.Tick(0.016f, out float starAlpha, out _, out _);
            Assert.IsTrue(active);
            Assert.GreaterOrEqual(starAlpha, 0f);
        }

        [Test]
        public void Ascended_StarTakesPathColour()
        {
            var d = new CeremonyDriver();
            d.Stash(didAscend: true, path: 1); // Sango / Thunder
            d.Start();
            d.Tick(CrossingCeremonySpec.StarIgnitionSeconds * 0.25f, out _, out var starBase, out _);
            var expected = PathMotif.ColorOf(1);
            Assert.AreEqual(expected.r, starBase.r, 1e-4f, "An Ascended star must take the path colour");
            Assert.AreEqual(expected.g, starBase.g, 1e-4f);
            Assert.AreEqual(expected.b, starBase.b, 1e-4f);
        }

        [Test]
        public void Fallen_StarIsEmber_NeverPathColour()
        {
            var d = new CeremonyDriver();
            d.Stash(didAscend: false, path: 1);
            d.Start();
            d.Tick(CrossingCeremonySpec.StarIgnitionSeconds * 0.25f, out _, out var starBase, out _);
            Assert.AreEqual(PathMotif.Ember.r, starBase.r, 1e-4f, "A fall settles into ember — never a dead end, never a path colour");
            Assert.AreEqual(PathMotif.Ember.g, starBase.g, 1e-4f);
            Assert.AreEqual(PathMotif.Ember.b, starBase.b, 1e-4f);
        }

        [Test]
        public void GoesInactive_AfterIgnitionDuration()
        {
            var d = new CeremonyDriver();
            d.Stash(true, 0);
            d.Start();
            bool active = d.Tick(CrossingCeremonySpec.StarIgnitionSeconds + 0.1f, out _, out _, out _);
            Assert.IsFalse(active, "Ceremony must end once the ignition duration elapses");
            Assert.IsFalse(d.IsActive);
        }
    }
}
