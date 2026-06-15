using OriAscendant.Data;
using UnityEditor;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Shared helpers for host-level EditMode tests: serialized-field injection
    /// (the builder pattern, minus the scene) and config factories carrying the
    /// GAMEPLAY §2 table values.
    /// </summary>
    public static class EditModeTestHelpers
    {
        public static void Inject(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            Assert(prop != null, $"{component.GetType().Name}.{field} not found");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void InjectArray(Component component, string field, Object[] values)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            Assert(prop != null, $"{component.GetType().Name}.{field} not found");
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameplayConfig MakeGameplayConfig()
        {
            var config = ScriptableObject.CreateInstance<GameplayConfig>();
            config.baseRate = 1.0;
            config.tapChannelSeconds = 5.0;
            config.welcomeBackMinSeconds = 60;
            config.autosaveIntervalSeconds = 30;
            return config;
        }

        public static CultivationStageConfig MakeStage(string name, double multiplier,
            double thresholdMantissa, int tier)
        {
            var stage = ScriptableObject.CreateInstance<CultivationStageConfig>();
            stage.stageName = name;
            stage.productionMultiplier = multiplier;
            stage.aseThresholdMantissa = thresholdMantissa;
            stage.aseThresholdExponent = 0;
            stage.tier = tier;
            return stage;
        }

        /// <summary>The six GAMEPLAY §2.2 stages.</summary>
        public static CultivationStageConfig[] MakeStageTable() => new[]
        {
            MakeStage("Ọmọ Ayé", 1, 100, 0),
            MakeStage("Akẹ́kọ̀ọ́", 5, 1500, 0),
            MakeStage("Awo", 20, 5500, 0),
            MakeStage("Aláàṣẹ", 80, 100000, 1),
            MakeStage("Àgbà", 320, 750000, 1),
            MakeStage("Aṣẹ́gun", 1250, 0, 1),
        };

        public static PathConfig MakePath(string name, double online, double offline, double council)
        {
            var path = ScriptableObject.CreateInstance<PathConfig>();
            path.pathName = name;
            path.aseGenerationModifier = online;
            path.offlineRateModifier = offline;
            path.councilBonusModifier = council;
            return path;
        }

        /// <summary>Ane / Sango / Osun per GAMEPLAY §2.3.</summary>
        public static PathConfig[] MakePathTable() => new[]
        {
            MakePath("Ane", 1.0, 1.5, 1.0),
            MakePath("Sango", 2.0, 0.5, 1.0),
            MakePath("Osun", 1.0, 1.0, 2.0),
        };

        public static OriConfig MakeOri(string name)
        {
            var ori = ScriptableObject.CreateInstance<OriConfig>();
            ori.oriName = name;
            return ori;
        }

        /// <summary>Placeholder virtue-vows (pre-§7.10 content review).</summary>
        public static OriConfig[] MakeOriTable() => new[]
        {
            MakeOri("Mercy"),
            MakeOri("Resolve"),
            MakeOri("Cunning"),
            MakeOri("Devotion"),
        };

        private static CrossroadsBeat MakeBeat(string id) => new CrossroadsBeat
        {
            id = id,
            prompt = id + " prompt",
            options = new[]
            {
                new CrossroadsOption { oriIndex = 0, text = "mercy" },
                new CrossroadsOption { oriIndex = 1, text = "resolve" },
                new CrossroadsOption { oriIndex = 2, text = "cunning" },
                new CrossroadsOption { oriIndex = 3, text = "devotion" },
            },
        };

        /// <summary>A small deck where every beat offers one option per Ori.</summary>
        public static CrossroadsDeckConfig MakeCrossroadsDeck()
        {
            var deck = ScriptableObject.CreateInstance<CrossroadsDeckConfig>();
            deck.beats = new[] { MakeBeat("c0"), MakeBeat("c1"), MakeBeat("c2") };
            return deck;
        }

        public static TribulationConfig MakeTribulationConfig()
        {
            var config = ScriptableObject.CreateInstance<TribulationConfig>();
            config.baseAscendChance = 0.60;
            config.ascendFloor = 0.25;
            config.ascendCeiling = 0.90;
            config.aseThresholdMantissa = 25.0;
            config.aseThresholdExponent = 6;
            return config;
        }

        public static CouncilConfig MakeCouncilConfig()
        {
            var config = ScriptableObject.CreateInstance<CouncilConfig>();
            config.ancestorBaseBonus = 0.25;
            config.maxCouncil = 5;
            return config;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException(message);
        }
    }
}
