using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// GrantBonusAse (rewarded-ad double-offline, feat/ads): a one-time TOTAL
    /// grant. The cached rate must never move — RecalculateRate stays the sole
    /// writer of asePerSecond (TECH_DESIGN §4).
    /// </summary>
    public class AseBonusGrantTests
    {
        private GameObject _host;
        private AseGenerationSystem _aseGen;
        private SaveData _save;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("World");
            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            _save = new SaveData();
            _aseGen.Begin(_save);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        [Test]
        public void GrantBonusAse_AddsToTotalOnly_ZeroIsNoOp_RateUntouched()
        {
            _save.SetAse(BigNumber.FromDouble(40.0));
            _save.SetAsePerSecond(BigNumber.FromDouble(3.0));
            BigNumber rateBefore = _save.GetAsePerSecond();
            int aseChangedCount = 0;
            _aseGen.OnAseChanged += _ => aseChangedCount++;

            _aseGen.GrantBonusAse(BigNumber.FromDouble(40.0)); // the "double offline" grant

            Assert.AreEqual(BigNumber.FromDouble(80.0), _save.GetAse(), "bonus must add to the total");
            Assert.AreEqual(1, aseChangedCount, "grant announces via OnAseChanged");

            _aseGen.GrantBonusAse(BigNumber.Zero);

            Assert.AreEqual(BigNumber.FromDouble(80.0), _save.GetAse(), "zero bonus must be a no-op");
            Assert.AreEqual(1, aseChangedCount, "zero bonus must not announce");
            Assert.AreEqual(rateBefore, _save.GetAsePerSecond(), "cached rate must be untouched by grants");
        }
    }
}
