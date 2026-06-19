using NUnit.Framework;
using OriAscendant.UI;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate Wave 3 / Track B: pure-geometry contracts for TitleArc
    /// (ART_BIBLE §5.1 "a single thread of Àṣẹ gold rising into a faint constellation").
    /// </summary>
    public class TitleArcTests
    {
        // ---- ThreadPoint ----

        [Test]
        public void ThreadPoint_AtZero_IsNearBottom()
        {
            Vector2 p = TitleArc.ThreadPoint(0f);
            Assert.Less(p.y, 0.15f, "Thread must start near the bottom of the screen");
        }

        [Test]
        public void ThreadPoint_AtOne_IsNearTop()
        {
            Vector2 p = TitleArc.ThreadPoint(1f);
            Assert.Greater(p.y, 0.85f, "Thread must reach near the top of the screen");
        }

        [Test]
        public void ThreadPoint_IsMonotoneInY()
        {
            for (int i = 0; i < 9; i++)
            {
                float t0 = i / 10f, t1 = (i + 1) / 10f;
                Assert.Less(
                    TitleArc.ThreadPoint(t0).y,
                    TitleArc.ThreadPoint(t1).y,
                    $"y must increase monotonically: t={t0:F1}→{t1:F1}");
            }
        }

        [Test]
        public void ThreadPoint_StaysInHorizontalBand()
        {
            for (int i = 0; i <= 10; i++)
            {
                Vector2 p = TitleArc.ThreadPoint(i / 10f);
                float t = i / 10f;
                Assert.GreaterOrEqual(p.x, 0.25f,
                    $"t={t:F1}: thread drifted too far left (x={p.x:F3})");
                Assert.LessOrEqual(p.x, 0.75f,
                    $"t={t:F1}: thread drifted too far right (x={p.x:F3})");
            }
        }

        // ---- ConstellationPoints ----

        [Test]
        public void ConstellationPoints_ReturnsFiveStars()
        {
            Assert.AreEqual(5, TitleArc.ConstellationPoints().Length);
        }

        [Test]
        public void ConstellationPoints_AllInUpperZone()
        {
            foreach (var pt in TitleArc.ConstellationPoints())
                Assert.Greater(pt.Pos.y, 0.70f,
                    $"Star at y={pt.Pos.y:F2} must be in the upper 30% of the screen");
        }

        [Test]
        public void ConstellationPoints_AllHavePositiveAlpha()
        {
            foreach (var pt in TitleArc.ConstellationPoints())
                Assert.Greater(pt.Alpha, 0f);
        }

        [Test]
        public void ConstellationPoints_ApexIsBrightest()
        {
            float maxAlpha = 0f;
            Vector2 apexPos = Vector2.zero;
            foreach (var pt in TitleArc.ConstellationPoints())
            {
                if (pt.Alpha > maxAlpha) { maxAlpha = pt.Alpha; apexPos = pt.Pos; }
            }
            Assert.AreEqual(0.5f, apexPos.x, 0.12f,
                "Brightest star (apex) must sit near the horizontal centre");
        }
    }
}
