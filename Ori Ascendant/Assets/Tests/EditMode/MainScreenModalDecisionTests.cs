using NUnit.Framework;
using OriAscendant.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #16 (⑤a): exhaustive coverage of the modal-gate matrix
    /// (Ori vowed? × crossroads pending?) → expected modal. Host-free —
    /// the whole point of extracting the gate out of the Update loop is
    /// that "which modal" becomes a pure decision the tests can pin.
    /// Precedence rule (per dynasty-redesign Update sequencing): an
    /// unvowed Ori always wins, so the two modals never contend.
    /// </summary>
    public class MainScreenModalDecisionTests
    {
        [Test]
        public void UnvowedOri_NoCrossroads_ShowsOriVow()
        {
            Assert.AreEqual(MainScreenModal.OriVow,
                MainScreenModalDecision.Decide(isOriVowed: false, hasCrossroadsPending: false));
        }

        [Test]
        public void UnvowedOri_WithCrossroads_StillShowsOriVow()
        {
            // Birth vow blocks the climb beneath; the crossroads waits its turn.
            Assert.AreEqual(MainScreenModal.OriVow,
                MainScreenModalDecision.Decide(isOriVowed: false, hasCrossroadsPending: true));
        }

        [Test]
        public void VowedOri_NoCrossroads_ShowsNone()
        {
            Assert.AreEqual(MainScreenModal.None,
                MainScreenModalDecision.Decide(isOriVowed: true, hasCrossroadsPending: false));
        }

        [Test]
        public void VowedOri_WithCrossroads_ShowsCrossroads()
        {
            Assert.AreEqual(MainScreenModal.Crossroads,
                MainScreenModalDecision.Decide(isOriVowed: true, hasCrossroadsPending: true));
        }

        [Test]
        public void OriPrecedesCrossroads_IsTheOnlyOrdering()
        {
            // A regression test for the rule itself: whenever the Ori is unvowed,
            // crossroads-pending is irrelevant — the answer is always OriVow.
            for (int crossroads = 0; crossroads < 2; crossroads++)
            {
                Assert.AreEqual(MainScreenModal.OriVow,
                    MainScreenModalDecision.Decide(isOriVowed: false, hasCrossroadsPending: crossroads == 1),
                    $"unvowed + crossroads={crossroads == 1} must be OriVow");
            }
        }
    }
}
