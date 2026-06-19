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

        /// <summary>Crossroads seed deck with the supplied cards and the
        /// default milestone (Àṣẹ 1 000). Cards can be omitted to get an
        /// empty-deck config for guard-rail tests.</summary>
        public static CrossroadsConfig MakeCrossroadsConfig(params CrossroadsCard[] cards)
        {
            var config = ScriptableObject.CreateInstance<CrossroadsConfig>();
            config.milestoneMantissa = 1.0;
            config.milestoneExponent = 3; // 1 000 Àṣẹ
            config.deck = cards ?? new CrossroadsCard[0];
            return config;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException(message);
        }
    }
}
