using NUnit.Framework;
using OriAscendant.Core;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: the frame-accumulator that drives the 1s logical tick — remainder
    /// carry (zero long-run drift) and defensive handling of bad deltas.
    /// </summary>
    public class TickAccumulatorTests
    {
        [Test]
        public void TwoHalves_MakeOneTick()
        {
            var acc = new TickAccumulator();
            Assert.AreEqual(0, acc.Advance(0.5));
            Assert.AreEqual(1, acc.Advance(0.5));
            Assert.AreEqual(0.0, acc.Remainder, 1e-9);
        }

        [Test]
        public void RemainderCarries_NoDrift()
        {
            // 3 × 0.4s = 1.2s → exactly one tick with 0.2s carried, not zero ticks.
            var acc = new TickAccumulator();
            Assert.AreEqual(0, acc.Advance(0.4));
            Assert.AreEqual(0, acc.Advance(0.4));
            Assert.AreEqual(1, acc.Advance(0.4));
            Assert.AreEqual(0.2, acc.Remainder, 1e-9);
        }

        [Test]
        public void LargeDelta_YieldsMultipleTicks_AndKeepsRemainder()
        {
            // A 5.7s frame hitch (or post-resume frame) grants all whole seconds.
            var acc = new TickAccumulator();
            Assert.AreEqual(5, acc.Advance(5.7));
            Assert.AreEqual(0.7, acc.Remainder, 1e-9);
            Assert.AreEqual(1, acc.Advance(0.3));
        }

        [Test]
        public void LongRun_TickCountMatchesWallClock()
        {
            // 10,000 frames at 16.7ms ≈ 167.0s must yield exactly floor ticks.
            var acc = new TickAccumulator();
            int ticks = 0;
            for (int i = 0; i < 10_000; i++) ticks += acc.Advance(0.0167);

            double total = 10_000 * 0.0167; // 167.0
            Assert.AreEqual((int)total, ticks);
            Assert.AreEqual(total - (int)total, acc.Remainder, 1e-6);
        }

        [TestCase(-1.0)]
        [TestCase(0.0)]
        [TestCase(double.NaN)]
        public void BadDeltas_AreIgnored(double delta)
        {
            var acc = new TickAccumulator();
            acc.Advance(0.9);

            Assert.AreEqual(0, acc.Advance(delta));
            Assert.AreEqual(0.9, acc.Remainder, 1e-9, "bad delta must not corrupt state");
        }

        [Test]
        public void Reset_DropsPartialProgress()
        {
            var acc = new TickAccumulator();
            acc.Advance(0.9);
            acc.Reset();

            Assert.AreEqual(0.0, acc.Remainder, 1e-9);
            Assert.AreEqual(0, acc.Advance(0.5));
        }
    }
}
