using System.Reflection;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Regression gate for issue #2 — Crossroads double-tap state corruption.
    /// When a queued crossroads is promoted on MakeChoice(), a rapid second Confirm()
    /// must not resolve the newly-promoted card. The guard resets _selectedIndex before
    /// the system call, so a concurrent second tap finds index == -1 and returns early.
    /// </summary>
    public class ConfirmGuardTests
    {
        private GameObject _host;
        private CrossroadsSystem _crossroads;
        private AseGenerationSystem _aseGen;
        private SaveData _save;

        private CrossroadsCard CardA => new CrossroadsCard
        {
            id = "guard_a",
            prompt = "First test of resolve.",
            options = new[]
            {
                new CrossroadsOption { virtueIndex = 0, optionText = "Hold." },
                new CrossroadsOption { virtueIndex = 1, optionText = "Waver." },
            }
        };

        private CrossroadsCard CardB => new CrossroadsCard
        {
            id = "guard_b",
            prompt = "Second test of resolve.",
            options = new[]
            {
                new CrossroadsOption { virtueIndex = 0, optionText = "Persist." },
                new CrossroadsOption { virtueIndex = 1, optionText = "Yield." },
            }
        };

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("ConfirmGuardTestHost");

            _aseGen = _host.AddComponent<AseGenerationSystem>();
            EditModeTestHelpers.Inject(_aseGen, "_config", EditModeTestHelpers.MakeGameplayConfig());
            ServiceLocator.Register(_aseGen);

            var config = EditModeTestHelpers.MakeTwoMilestoneCrossroadsConfig(CardA, CardB);
            _crossroads = _host.AddComponent<CrossroadsSystem>();
            EditModeTestHelpers.Inject(_crossroads, "_config", config);
            ServiceLocator.Register(_crossroads);

            _save = new SaveData { chosenOri = 0 };
            _aseGen.Begin(_save);
            _crossroads.Begin(_save);

            // Trigger both milestones: card_a active, card_b queued
            _crossroads.SetRandomSource(new FakeRandom(0.0, 0.5));
            _save.SetAse(BigNumber.FromDouble(10_000));
            _crossroads.EvaluateMilestone();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        [Test]
        public void CrossroadsView_Confirm_DoubleCall_ResolvesCardOnce()
        {
            var viewHost = new GameObject("CrossroadsViewHost");
            try
            {
                var view = viewHost.AddComponent<CrossroadsScreenView>();
                SetField(view, "_crossroadsSystem", _crossroads);
                SetField(view, "_selectedIndex", 0);

                var confirm = GetConfirm(typeof(CrossroadsScreenView));
                confirm.Invoke(view, null); // first tap → guard_a resolved, guard_b promoted
                confirm.Invoke(view, null); // second tap → must be no-op

                Assert.AreEqual(1, _save.deeds.Count,
                    "double-confirm must resolve exactly one card — guard resets index before MakeChoice (#2)");
            }
            finally
            {
                Object.DestroyImmediate(viewHost);
            }
        }

        // ---- helpers ----

        private static MethodInfo GetConfirm(System.Type type) =>
            type.GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void SetField(object target, string name, object value)
        {
            var fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"field '{name}' not found on {target.GetType().Name}");
            fi.SetValue(target, value);
        }
    }
}
