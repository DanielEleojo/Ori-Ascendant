using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pure geometry for the title-screen procedural dress (ART_BIBLE §5.1):
    /// "a single thread of Àṣẹ gold rising into a faint constellation." All fns
    /// return plain structs and primitives — no MonoBehaviour, testable headlessly.
    /// </summary>
    public static class TitleArc
    {
        /// <summary>A single star in the title constellation.</summary>
        public readonly struct StarPoint
        {
            public readonly Vector2 Pos;   // normalised screen coords (0..1, y up)
            public readonly float Size;    // radius in normalised units (~0.008..0.016)
            public readonly float Alpha;

            public StarPoint(Vector2 pos, float size, float alpha)
            { Pos = pos; Size = size; Alpha = alpha; }
        }

        /// <summary>Parametric S-curve from bottom-centre (t=0) to near-top-centre (t=1).
        /// Small horizontal wobble keeps it feeling alive rather than mechanical.</summary>
        public static Vector2 ThreadPoint(float t)
        {
            float x = 0.5f + Mathf.Sin(t * Mathf.PI * 1.4f) * 0.055f;
            float y = Mathf.Lerp(0.04f, 0.91f, t);
            return new Vector2(x, y);
        }

        /// <summary>Five stars forming a gentle arc in the upper portion of the screen,
        /// centred on the thread's apex. The centre star is brightest (ART_BIBLE §5.1
        /// "faint constellation" at the top of the rising thread).</summary>
        public static StarPoint[] ConstellationPoints() => new[]
        {
            new StarPoint(new Vector2(0.28f, 0.88f), 0.012f, 0.48f),
            new StarPoint(new Vector2(0.40f, 0.92f), 0.008f, 0.33f),
            new StarPoint(new Vector2(0.50f, 0.95f), 0.016f, 0.65f), // apex
            new StarPoint(new Vector2(0.60f, 0.92f), 0.008f, 0.33f),
            new StarPoint(new Vector2(0.72f, 0.88f), 0.012f, 0.48f),
        };
    }
}
