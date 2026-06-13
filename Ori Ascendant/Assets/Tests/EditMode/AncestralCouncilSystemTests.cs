using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate C: council bonus math and the Àṣẹ-neutral retirement rule —
    /// including under Osun's councilBonusModifier 2.0, which is the whole
    /// reason the modifier wraps the permanent and active terms together.
    /// </summary>
    public class AncestralCouncilSystemTests
    {
        private GameObject _host;
        private AncestralCouncilSystem _council;
        private SaveData _save;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("TestHost");
            _council = _host.AddComponent<AncestralCouncilSystem>();
            EditModeTestHelpers.Inject(_council, "_config", EditModeTestHelpers.MakeCouncilConfig());
            ServiceLocator.Register(_council); // EditMode: Awake doesn't run
            _save = new SaveData();
            _council.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        private static AncestorData Ancestor(bool ascended, long timestamp) => new AncestorData
        {
            peakStage = 5,
            path = 1,
            didAscend = ascended,
            bonusMultiplier = ascended ? 1.0 : 0.4,
            completedTimestamp = timestamp,
        };

        [Test]
        public void ActiveSum_MixedCouncil()
        {
            _save.council.Add(Ancestor(true, 100));
            _save.council.Add(Ancestor(true, 200));
            _save.council.Add(Ancestor(false, 300));

            // 0.25 × (1.0 + 1.0 + 0.4) = 0.6
            Assert.AreEqual(0.6, _council.ActiveCouncilSum, 1e-12);
        }

        [Test]
        public void Induct_BelowMax_AddsWithoutRetirement()
        {
            var retired = _council.InductAncestor(Ancestor(true, 100));

            Assert.IsNull(retired);
            Assert.AreEqual(1, _save.council.Count);
            Assert.AreEqual(0.0, _save.lineage.permanentAseBonus);
        }

        [Test]
        public void Induct_AtMax_RetiresOldestByTimestamp_NotListOrder()
        {
            // Shuffled timestamps: list order ≠ age order. Oldest is ts=50 at index 2.
            _save.council.Add(Ancestor(true, 300));
            _save.council.Add(Ancestor(false, 400));
            _save.council.Add(Ancestor(true, 50));
            _save.council.Add(Ancestor(true, 200));
            _save.council.Add(Ancestor(false, 100));

            var retired = _council.InductAncestor(Ancestor(true, 999));

            Assert.IsNotNull(retired);
            Assert.AreEqual(50, retired.completedTimestamp, "must retire the OLDEST by timestamp");
            Assert.AreEqual(5, _save.council.Count, "council stays at max");
            Assert.AreEqual(0.25 * 1.0, _save.lineage.permanentAseBonus, 1e-12,
                "the retiree's W × bonus settles into the foundation");
        }

        [TestCase(1.0)]
        [TestCase(2.0)] // Osun — the joint wrap is load-bearing here
        public void Retirement_IsExactlyAseNeutral(double councilModifier)
        {
            for (int i = 0; i < 5; i++) _save.council.Add(Ancestor(i % 2 == 0, 100 + i));

            double termBefore = _save.lineage.permanentAseBonus + _council.ActiveCouncilSum;
            var rateBefore = RateCalculator.ComputeRate(1.0, 320.0, 1.0, councilModifier,
                _save.lineage.permanentAseBonus, _council.ActiveCouncilSum);

            var inducted = Ancestor(true, 999);
            _council.InductAncestor(inducted);

            // Neutrality: the lineage term changed ONLY by the new member's
            // contribution — retirement itself moved value, never destroyed it.
            double termAfter = _save.lineage.permanentAseBonus + _council.ActiveCouncilSum;
            Assert.AreEqual(termBefore + _council.W * inducted.bonusMultiplier, termAfter, 1e-12);

            // Equivalent rate check: recompute as if the new member hadn't joined.
            var rateAfterMinusNew = RateCalculator.ComputeRate(1.0, 320.0, 1.0, councilModifier,
                _save.lineage.permanentAseBonus,
                _council.ActiveCouncilSum - _council.W * inducted.bonusMultiplier);
            Assert.AreEqual(rateBefore, rateAfterMinusNew,
                $"retirement changed the rate at councilModifier={councilModifier}");
        }

        [Test]
        public void Retirement_RaisesEvent_WithTheRetiree()
        {
            for (int i = 0; i < 5; i++) _save.council.Add(Ancestor(true, 100 + i));

            AncestorData announced = null;
            _council.OnAncestorRetired += a => announced = a;
            _council.InductAncestor(Ancestor(false, 999));

            Assert.IsNotNull(announced);
            Assert.AreEqual(100, announced.completedTimestamp);
        }
    }
}
