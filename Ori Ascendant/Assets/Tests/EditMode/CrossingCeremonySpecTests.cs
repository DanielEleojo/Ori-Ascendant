using System.Reflection;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.UI;
using OriAscendant.UI.Screens;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Issue #34: Crossing ceremony — vessel light rises to ignite a new star (PRD W3).
    /// Headless gate — CrossingCeremonySpec is pure math, no scene, no MonoBehaviour.
    /// Tests pin: trigger alignment with column apex, new-star alpha hierarchy,
    /// star ignition flash curve, column exit curve, IsActive predicate, and
    /// reflection gates confirming MainScreenSkin wires the ceremony fields.
    /// </summary>
    public class CrossingCeremonySpecTests
    {
        // ---- Trigger alignment ----

        [Test]
        public void TriggerFraction_AlignsWithColumnApex() =>
            Assert.AreEqual(CrossingColumnSpec.ApexFraction, CrossingCeremonySpec.TriggerFraction, 1e-6,
                "Ceremony must fire when the column reaches its apex (tribulation eligibility)");

        // ---- New-star alpha hierarchy ----

        [Test]
        public void NewStarAlpha_Ascended_IsHigherThan_Fallen() =>
            Assert.Greater(CrossingCeremonySpec.NewStarAlpha(true),
                CrossingCeremonySpec.NewStarAlpha(false),
                "Ascended star must outshine the ember star");

        [Test]
        public void NewStarAlpha_Fallen_IsPositive() =>
            Assert.Greater(CrossingCeremonySpec.NewStarAlpha(false), 0f,
                "Ember star must be visible — a fall is never a dead end");

        [Test]
        public void NewStarAlpha_Ascended_IsFull() =>
            Assert.AreEqual(1f, CrossingCeremonySpec.NewStarAlpha(true), 1e-4f,
                "An Ascended star must reach full alpha — bright in path colour");

        [Test]
        public void EmberStarAlpha_IsPositive() =>
            Assert.Greater(CrossingCeremonySpec.EmberStarAlpha, 0f,
                "EmberStarAlpha must be positive — a fallen cultivator still produces an ancestor");

        // ---- Star ignition flash curve ----

        [Test]
        public void StarIgnitionAlpha_ZeroElapsed_IsZero() =>
            Assert.AreEqual(0f,
                CrossingCeremonySpec.StarIgnitionAlpha(0f, CrossingCeremonySpec.StarIgnitionSeconds, true),
                1e-4f,
                "Star must start invisible — the flash begins from nothing");

        [Test]
        public void StarIgnitionAlpha_PostDuration_Ascended_SettlesAtAscendedAlpha()
        {
            float result = CrossingCeremonySpec.StarIgnitionAlpha(
                CrossingCeremonySpec.StarIgnitionSeconds + 1f,
                CrossingCeremonySpec.StarIgnitionSeconds, true);
            Assert.AreEqual(CrossingCeremonySpec.AscendedStarAlpha, result, 1e-4f,
                "After the flash, an Ascended star must settle at AscendedStarAlpha");
        }

        [Test]
        public void StarIgnitionAlpha_PostDuration_Fallen_SettlesAtEmberAlpha()
        {
            float result = CrossingCeremonySpec.StarIgnitionAlpha(
                CrossingCeremonySpec.StarIgnitionSeconds + 1f,
                CrossingCeremonySpec.StarIgnitionSeconds, false);
            Assert.AreEqual(CrossingCeremonySpec.EmberStarAlpha, result, 1e-4f,
                "After the flash, a fallen star must settle at EmberStarAlpha");
        }

        [Test]
        public void StarIgnitionAlpha_DuringRise_IsPositiveAndAtMostOne()
        {
            float rising = CrossingCeremonySpec.StarIgnitionAlpha(
                CrossingCeremonySpec.StarIgnitionSeconds * 0.25f,
                CrossingCeremonySpec.StarIgnitionSeconds, true);
            Assert.Greater(rising, 0f,
                "Star must be rising at the first quarter of the ignition duration");
            Assert.LessOrEqual(rising, 1f,
                "Star alpha must never exceed 1 during the flash");
        }

        [Test]
        public void StarIgnitionAlpha_Ascended_SettlesHigherThan_Fallen()
        {
            float settleAscend = CrossingCeremonySpec.StarIgnitionAlpha(
                CrossingCeremonySpec.StarIgnitionSeconds * 2f,
                CrossingCeremonySpec.StarIgnitionSeconds, true);
            float settleFall = CrossingCeremonySpec.StarIgnitionAlpha(
                CrossingCeremonySpec.StarIgnitionSeconds * 2f,
                CrossingCeremonySpec.StarIgnitionSeconds, false);
            Assert.Greater(settleAscend, settleFall,
                "Settled Ascended star must be brighter than settled ember");
        }

        // ---- Column exit curve ----

        [Test]
        public void ColumnExitAlpha_ZeroElapsed_IsOne() =>
            Assert.AreEqual(1f,
                CrossingCeremonySpec.ColumnExitAlpha(0f, CrossingCeremonySpec.ColumnFadeSeconds),
                1e-4f,
                "Column must be at full alpha at the start of the ceremony fade");

        [Test]
        public void ColumnExitAlpha_PostDuration_IsZero() =>
            Assert.AreEqual(0f,
                CrossingCeremonySpec.ColumnExitAlpha(
                    CrossingCeremonySpec.ColumnFadeSeconds + 1f,
                    CrossingCeremonySpec.ColumnFadeSeconds),
                1e-4f,
                "Column must be fully gone after the fade duration");

        [Test]
        public void ColumnExitAlpha_IsDecreasing()
        {
            float early = CrossingCeremonySpec.ColumnExitAlpha(
                CrossingCeremonySpec.ColumnFadeSeconds * 0.2f,
                CrossingCeremonySpec.ColumnFadeSeconds);
            float late = CrossingCeremonySpec.ColumnExitAlpha(
                CrossingCeremonySpec.ColumnFadeSeconds * 0.7f,
                CrossingCeremonySpec.ColumnFadeSeconds);
            Assert.Greater(early, late,
                "Column exit alpha must decrease monotonically — the column fades out, never back in");
        }

        // ---- IsActive predicate ----

        [Test]
        public void IsActive_ZeroElapsed_IsTrue() =>
            Assert.IsTrue(CrossingCeremonySpec.IsActive(0f),
                "Ceremony must be active immediately after the Crossing fires");

        [Test]
        public void IsActive_PostDuration_IsFalse() =>
            Assert.IsFalse(CrossingCeremonySpec.IsActive(CrossingCeremonySpec.StarIgnitionSeconds),
                "Ceremony must end when the star ignition duration elapses");

        // ---- Timing constants ----

        [Test]
        public void StarIgnitionSeconds_IsPositive() =>
            Assert.Greater(CrossingCeremonySpec.StarIgnitionSeconds, 0f,
                "StarIgnitionSeconds must be a positive duration");

        [Test]
        public void ColumnFadeSeconds_IsPositive() =>
            Assert.Greater(CrossingCeremonySpec.ColumnFadeSeconds, 0f,
                "ColumnFadeSeconds must be a positive duration");

        // ---- Reflection gates: MainScreenSkin must wire the ceremony ----

        [Test]
        public void MainScreenSkin_Has_CrossingNewStarField()
        {
            var field = typeof(MainScreenSkin)
                .GetField("_crossingNewStar", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                "_crossingNewStar must exist in MainScreenSkin — ceremony ignites the new star (issue #34)");
        }

        [Test]
        public void MainScreenSkin_Has_CrossingCeremonyElapsedField()
        {
            var field = typeof(MainScreenSkin)
                .GetField("_crossingCeremonyElapsed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                "_crossingCeremonyElapsed must exist in MainScreenSkin — tracks ceremony animation progress (issue #34)");
        }

        // ---- issue #4: ceremony plays after overlay, not beneath it ----

        [Test]
        public void TribulationScreen_HasOnCeremonyClosed_Event()
        {
            var evt = typeof(TribulationScreen)
                .GetEvent("OnCeremonyClosed", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(evt,
                "TribulationScreen must expose public event OnCeremonyClosed — fires in Finish() " +
                "so MainScreenSkin can start the star-ignition after the overlay closes (#4)");
        }

        [Test]
        public void TribulationScreen_Finish_FiresOnCeremonyClosed()
        {
            ServiceLocator.Clear();
            var host = new GameObject("TribScreenTest");
            try
            {
                var screen = host.AddComponent<TribulationScreen>();
                bool fired = false;
                screen.OnCeremonyClosed += () => fired = true;

                var finish = typeof(TribulationScreen)
                    .GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(finish, "TribulationScreen.Finish() must exist");
                finish.Invoke(screen, null);

                Assert.IsTrue(fired,
                    "Finish() must fire OnCeremonyClosed — ceremony must play after overlay, not beneath it (#4)");
            }
            finally
            {
                Object.DestroyImmediate(host);
                ServiceLocator.Clear();
            }
        }

        [Test]
        public void MainScreenSkin_HasCeremonyStashFields()
        {
            var didAscend = typeof(MainScreenSkin)
                .GetField("_crossingCeremonyDidAscend", BindingFlags.Instance | BindingFlags.NonPublic);
            var path = typeof(MainScreenSkin)
                .GetField("_crossingCeremonyPath", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(didAscend,
                "_crossingCeremonyDidAscend must exist — stashed by OnCeremonyFired, used when overlay closes");
            Assert.IsNotNull(path,
                "_crossingCeremonyPath must exist — stashed by OnCeremonyFired, used when overlay closes");
        }
    }
}
