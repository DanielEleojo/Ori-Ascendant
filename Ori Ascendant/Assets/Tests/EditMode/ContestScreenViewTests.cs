using NUnit.Framework;
using OriAscendant.UI.Screens;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Unit tests for the pure helper methods on ContestScreenView (issue #43).
    /// These test the internal static helpers — no MonoBehaviour, no scene needed.
    /// </summary>
    public class ContestScreenViewTests
    {
        // ── PowerTierLabel ────────────────────────────────────────────────────────

        [Test]
        public void PowerTierLabel_WeakerRival_ContainsWeaker()
        {
            string label = ContestScreenView.PowerTierLabel(0.80);
            StringAssert.Contains("Weaker", label);
        }

        [Test]
        public void PowerTierLabel_StrongerRival_ContainsStronger()
        {
            string label = ContestScreenView.PowerTierLabel(1.20);
            StringAssert.Contains("Stronger", label);
        }

        [Test]
        public void PowerTierLabel_EvenlyMatched_ContainsEvenly()
        {
            string label = ContestScreenView.PowerTierLabel(1.00);
            StringAssert.Contains("Evenly", label);
        }

        [Test]
        public void PowerTierLabel_ExactLowerBoundary_ContainsWeaker()
        {
            // 0.84 is still below 0.85 — must still be "Weaker"
            string label = ContestScreenView.PowerTierLabel(0.84);
            StringAssert.Contains("Weaker", label);
        }

        [Test]
        public void PowerTierLabel_ExactUpperBoundary_ContainsStronger()
        {
            // 1.16 is above 1.15 — must be "Stronger"
            string label = ContestScreenView.PowerTierLabel(1.16);
            StringAssert.Contains("Stronger", label);
        }

        [Test]
        public void PowerTierLabel_AtBoundaryLow_ContainsEvenly()
        {
            // Exactly 0.85 is not < 0.85 — falls into "Evenly matched"
            string label = ContestScreenView.PowerTierLabel(0.85);
            StringAssert.Contains("Evenly", label);
        }

        [Test]
        public void PowerTierLabel_AtBoundaryHigh_ContainsEvenly()
        {
            // Exactly 1.15 is not > 1.15 — falls into "Evenly matched"
            string label = ContestScreenView.PowerTierLabel(1.15);
            StringAssert.Contains("Evenly", label);
        }

        // ── RevealTitle ───────────────────────────────────────────────────────────

        [Test]
        public void RevealTitle_Won_IsVictory()
        {
            Assert.AreEqual("VICTORY", ContestScreenView.RevealTitle(true));
        }

        [Test]
        public void RevealTitle_Lost_IsHouseStoodFirm()
        {
            Assert.AreEqual("THE HOUSE STOOD FIRM", ContestScreenView.RevealTitle(false));
        }

        [Test]
        public void RevealTitle_Lost_DoesNotContainDefeat()
        {
            string title = ContestScreenView.RevealTitle(false);
            StringAssert.DoesNotContain("defeat", title.ToLowerInvariant());
        }

        [Test]
        public void RevealTitle_Lost_DoesNotContainLoss()
        {
            // "loss" must not appear anywhere in the reveal text (case-insensitive)
            string title = ContestScreenView.RevealTitle(false);
            StringAssert.DoesNotContain("loss", title.ToLowerInvariant());
        }
    }
}
