using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OriAscendant.Tests.PlayMode
{
    /// <summary>
    /// Real-runtime verification the EditMode suite cannot do: the 1s accumulator
    /// tick actually accrues Àṣẹ over wall-clock in the player loop, and the
    /// built Main scene boots with every system wired through ServiceLocator
    /// (catches broken serialized refs / Awake-Start order). Uses reflection for
    /// field injection (no UnityEditor dependency in a PlayMode assembly).
    /// </summary>
    public class RuntimeLoopPlayTests
    {
        private static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} not found");
            f.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator Tick_AccruesAseOverRealTime()
        {
            ServiceLocator.Clear();
            var host = new GameObject("PlayHost");

            var config = ScriptableObject.CreateInstance<GameplayConfig>();
            config.baseRate = 1.0;
            config.tapChannelSeconds = 5.0;

            // In PlayMode (unlike EditMode) AddComponent runs Awake, so the system
            // registers itself; with no CultivationSystem present the rate is just
            // baseRate (stage/path read as neutral).
            var aseGen = host.AddComponent<AseGenerationSystem>();
            SetField(aseGen, "_config", config);

            var save = new SaveData();
            aseGen.Begin(save);
            aseGen.RecalculateRate();
            Assert.AreEqual(BigNumber.One, aseGen.CurrentRate, "stage-1 rate should be 1.0/s");

            BigNumber before = aseGen.CurrentAse;
            yield return new WaitForSecondsRealtime(2.3f);
            BigNumber delta = aseGen.CurrentAse - before;

            Assert.IsTrue(delta >= BigNumber.FromDouble(2.0),
                $"expected at least 2 ticks of real-time accrual, got {delta}");
            Assert.IsTrue(delta <= BigNumber.FromDouble(3.0),
                $"expected at most 3 ticks in 2.3s, got {delta}");

            Object.Destroy(host);
            ServiceLocator.Clear();
        }

        [UnityTest]
        public IEnumerator MainScene_Boots_AllSystemsWired_AndTicks()
        {
            // Deterministic fresh start.
            string savePath = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(savePath)) File.Delete(savePath);
            ServiceLocator.Clear();

            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null; // Awake
            yield return null; // Start (GameManager orchestrates load -> begin -> recalc)

            Assert.IsTrue(ServiceLocator.TryGet(out AseGenerationSystem aseGen), "AseGenerationSystem not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out CultivationSystem _), "CultivationSystem not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out TribulationSystem _), "TribulationSystem not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out AncestralCouncilSystem _), "AncestralCouncilSystem not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out SaveManager _), "SaveManager not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out CloudSaveManager _), "CloudSaveManager not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out CrossroadsSystem crossroads), "CrossroadsSystem not wired");
            Assert.IsFalse(crossroads.HasPending, "a fresh boot has no pending crossroads");

            // Fresh save → Stage 1, rate 1.0/s (no path, no council).
            Assert.AreEqual(BigNumber.One, aseGen.CurrentRate,
                "a fresh boot should compute the Stage-1 base rate");

            BigNumber before = aseGen.CurrentAse;
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.IsTrue(aseGen.CurrentAse > before, "Àṣẹ must tick up in the live scene");
        }

        [UnityTest]
        public IEnumerator MainScene_CrossroadsDrawsAtAseMilestone()
        {
            // The full live trigger chain, in the real scene: banking Àṣẹ past the first
            // stage advance-threshold (100) must draw a crossroads via OnAseChanged →
            // CheckMilestones. This is the one path the boot/EditMode gates can't reach —
            // it proves both the OnAseChanged subscription AND the scene-wired _stages
            // milestone schedule (an unwired/empty _stages would silently never draw).
            string savePath = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(savePath)) File.Delete(savePath);
            ServiceLocator.Clear();

            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null; // Awake
            yield return null; // Start

            Assert.IsTrue(ServiceLocator.TryGet(out CrossroadsSystem crossroads), "CrossroadsSystem not wired");
            Assert.IsTrue(ServiceLocator.TryGet(out SaveManager saveManager), "SaveManager not wired");
            Assert.IsFalse(crossroads.HasPending, "no crossroads before any Àṣẹ is banked");

            // Bank past the Stage-1 advance threshold (100 Àṣẹ); the next tick raises
            // OnAseChanged on the shared save, which the crossroads system catches.
            saveManager.Current.SetAse(BigNumber.FromDouble(150));
            yield return new WaitForSecondsRealtime(1.2f);

            Assert.IsTrue(crossroads.HasPending,
                "crossing an Àṣẹ milestone draws a crossroads via the live subscription + scene-wired _stages");
        }
    }
}
