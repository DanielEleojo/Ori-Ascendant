using NUnit.Framework;
using OriAscendant.Systems;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate A: the renown→rate-bonus cap (issue #35). Stored renown is uncapped;
    /// only its production-rate contribution is clamped, so contending is never mandatory.
    /// </summary>
    public class LineageRenownTests
    {
        [Test]
        public void Renown_BelowCap_PassesThrough()
        {
            Assert.AreEqual(0.1, LineageRenown.ToBonus(0.1, 0.25), 1e-12);
        }

        [Test]
        public void Renown_AboveCap_Clamps()
        {
            Assert.AreEqual(0.25, LineageRenown.ToBonus(0.9, 0.25), 1e-12);
        }

        [Test]
        public void Renown_AtCap_ReturnsCap()
        {
            Assert.AreEqual(0.25, LineageRenown.ToBonus(0.25, 0.25), 1e-12);
        }
    }
}
