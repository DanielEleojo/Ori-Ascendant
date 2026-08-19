using System.IO;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI;
using OriAscendant.UI.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OriAscendant.EditorTools
{
    /// <summary>
    /// Deterministic, replayable construction of the Main scene (GAMEPLAY §3.2/§3.3)
    /// and ALL config assets with the playtest-locked GAMEPLAY §2 values.
    /// Idempotent: assets are updated in place (GUIDs preserved → scene refs survive);
    /// the scene regenerates from scratch each run — never hand-edit Main.unity.
    /// Run headless:
    ///   Unity -batchmode -nographics -quit -projectPath ... -executeMethod OriAscendant.EditorTools.SceneBuilder.BuildAll
    /// </summary>
    public static class SceneBuilder
    {
        private const string ConfigFolder = "Assets/Configs";
        private const string GameplayConfigPath = ConfigFolder + "/GameplayConfig.asset";
        private const string TribulationConfigPath = ConfigFolder + "/TribulationConfig.asset";
        private const string CouncilConfigPath = ConfigFolder + "/CouncilConfig.asset";
        private const string OriConfigPath = ConfigFolder + "/OriConfig.asset";
        private const string CrossroadsConfigPath = ConfigFolder + "/CrossroadsConfig.asset";
        private const string RemembranceConfigPath = ConfigFolder + "/RemembranceConfig.asset";
        private const string CrossroadsDeckConfigPath = ConfigFolder + "/CrossroadsDeckConfig.asset";
        private const string ContestConfigPath = ConfigFolder + "/ContestConfig.asset";
        private const string StageFolder = "Assets/Resources/StageConfigs";
        private const string PathFolder = "Assets/Resources/PathConfigs";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpEssentialsPackage =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // Palette aliases — single source of truth is Palette.cs (ADR-0007 Unit 2).
        // ponytail: local names kept so no downstream churn is needed.
        private static readonly Color Bg       = Palette.IndigoBase;
        private static readonly Color Panel    = Palette.IndigoLift;
        private static readonly Color PanelLine = Palette.DuskViolet;
        private static readonly Color Gold     = Palette.AseGold;
        private static readonly Color Text     = Palette.TextPrimary;
        private static readonly Color TextDim  = Palette.TextSecondary;

        // ---- GAMEPLAY §2.2 stage table (playtest-locked; /Resources/StageConfigs/ rule) ----
        private struct StageRow
        {
            public string File, Name, Description;
            public double Multiplier, ThresholdMantissa;
            public int ThresholdExponent, Tier;
        }

        private static readonly StageRow[] Stages =
        {
            new StageRow { File = "Stage1", Name = "Ọmọ Ayé", Description = "A newborn soul in the world's great marketplace, feeling the first stir of àṣẹ.", Multiplier = 1, ThresholdMantissa = 100, ThresholdExponent = 0, Tier = 0 },
            new StageRow { File = "Stage2", Name = "Akẹ́kọ̀ọ́", Description = "A devoted learner, training breath and tongue to carry àṣẹ.", Multiplier = 5, ThresholdMantissa = 1500, ThresholdExponent = 0, Tier = 0 },
            new StageRow { File = "Stage3", Name = "Awo", Description = "Admitted to the mysteries at last — the initiate must now choose a path.", Multiplier = 20, ThresholdMantissa = 5500, ThresholdExponent = 0, Tier = 0 },
            new StageRow { File = "Stage4", Name = "Aláàṣẹ", Description = "One whose word makes things happen; àṣẹ answers when they speak.", Multiplier = 80, ThresholdMantissa = 100000, ThresholdExponent = 0, Tier = 1 },
            new StageRow { File = "Stage5", Name = "Àgbà", Description = "An elder of weight; the whole lineage steadies itself around their presence.", Multiplier = 320, ThresholdMantissa = 750000, ThresholdExponent = 0, Tier = 1 },
            new StageRow { File = "Stage6", Name = "Aṣẹ́gun", Description = "Victor of the mortal road, standing at the river's edge where ayé ends.", Multiplier = 1250, ThresholdMantissa = 0, ThresholdExponent = 0, Tier = 1 }, // gated by TribulationConfig
        };

        // ---- GAMEPLAY §2.3 path table ----
        private struct PathRow
        {
            public string File, Name, Tradition, Identity, Badge, OfflineBonusLabel, Description;
            public double Online, Offline, Council;
            public TribulationType Type;
        }

        private static readonly PathRow[] Paths =
        {
            new PathRow { File = "Ane", Name = "Ane — Path of Earth", Tradition = "Igala earth deity (Anẹ̀)", Identity = "The Mountain Endures — Àṣẹ gathers while you rest: ×1.5 offline", Badge = "OFFLINE ×1.5", OfflineBonusLabel = "Earth's Patience", Description = "Patience and rootedness — the land grows while you sleep.", Online = 1.0, Offline = 1.5, Council = 1.0, Type = TribulationType.Earth },
            new PathRow { File = "Sango", Name = "Sango — Path of Thunder", Tradition = "Yoruba thunder orisha (Ṣàngó)", Identity = "The Storm Strikes Now — ×2 Àṣẹ while you cultivate", Badge = "ACTIVE ×2", OfflineBonusLabel = "", Description = "Sudden, overwhelming force while you are present; the storm sleeps when you are away.", Online = 2.0, Offline = 0.5, Council = 1.0, Type = TribulationType.Storm },
            new PathRow { File = "Osun", Name = "Osun — Path of the River", Tradition = "Yoruba river orisha (Ọ̀ṣun)", Identity = "The River Remembers — ancestors' blessings flow twice as strong: council bonuses ×2", Badge = "COUNCIL ×2", OfflineBonusLabel = "", Description = "Flow and continuity — the mother of generations strengthens the bloodline.", Online = 1.0, Offline = 1.0, Council = 2.0, Type = TribulationType.River },
        };

        [MenuItem("Ori Ascendant/Build Main Scene")]
        public static void BuildAll()
        {
            EnsureTmpEssentials();
            TMP_FontAsset fallback = EnsureNotoFallback();
            AuditYorubaGlyphCoverage(fallback);
            GameplayConfig gameplay = BuildGameplayConfig();
            TribulationConfig tribulation = BuildTribulationConfig();
            CouncilConfig councilConfig = BuildCouncilConfig();
            OriConfig oriConfig = BuildOriConfig();
            CrossroadsConfig crossroadsConfig = BuildCrossroadsConfig();
            RemembranceConfig remembranceConfig = BuildRemembranceConfig();
            CrossroadsDeckConfig crossroadsDeck = BuildCrossroadsDeckConfig();
            ContestConfig contestConfig = BuildContestConfig();
            CultivationStageConfig[] stages = BuildStageConfigs();
            PathConfig[] paths = BuildPathConfigs();
            BuildMainScene(gameplay, tribulation, councilConfig, oriConfig, crossroadsConfig,
                remembranceConfig, crossroadsDeck, contestConfig, stages, paths);
            BuildConfigurator.Apply();
            Debug.Log("SceneBuilder: scene + 16 config assets built successfully.");
        }

        // ================= config assets =================

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(folder);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static GameplayConfig BuildGameplayConfig()
        {
            // MUST SetDirty (not just load): a clean, non-dirtied ScriptableObject
            // gets unloaded during EditorSceneManager.SaveScene, turning every
            // component reference to it into a fake-null that serializes as
            // {fileID: 0}. SetDirty pins it in memory through the save — the same
            // reason the other Build*Config assets always serialized correctly.
            var config = EnsureAsset<GameplayConfig>(GameplayConfigPath);
            config.baseRate = 1.0;
            config.tapChannelSeconds = 5.0;
            config.welcomeBackMinSeconds = 30;
            config.autosaveIntervalSeconds = 30;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static TribulationConfig BuildTribulationConfig()
        {
            var config = EnsureAsset<TribulationConfig>(TribulationConfigPath);
            config.baseAscendChance = 0.60;
            config.ascendFloor = 0.25;
            config.ascendCeiling = 0.90;
            config.aseThresholdMantissa = 25.0;
            config.aseThresholdExponent = 6;
            config.ambientFractions = new[] { 0.5f, 0.8f, 1.0f };
            config.holdToConfirmSeconds = 0.8;
            // Line-legacy compounding (issue #8, balance-pass locked issue #12).
            config.lineLegacyBonusPerGen = 0.05;
            config.lineLegacyMaxBonus = 0.15;
            // Ceremony beats — GAMEPLAY §3.5 timing table.
            config.transitionSeconds = 2.0f;
            config.stormWaveCount = 3;
            config.stormWaveIntervalSeconds = 1.0f;
            config.silenceHoldSeconds = 1.5f;
            config.revealSeconds = 2.5f;
            config.ancestorCardSeconds = 2.5f;
            config.finalBeatSeconds = 2.0f;
            // Line-legacy compounding (issue #8): provisional values, awaiting balance sim (#12).
            // Explicit assignment prevents pre-issue-8 assets from silently keeping 0.
            config.lineLegacyBonusPerGen = 0.05;
            config.lineLegacyMaxBonus = 0.15;
            // Crowned Ascended reveal (Phase 6, issue #11): slot left null until the
            // bespoke appearance-0 portrait ships (funded + §7.10 native-speaker-cleared).
            // When the art lands, assign it here; gold-FX overlay is the fallback.
            config.crownedAscendedRevealPortrait = null;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static CouncilConfig BuildCouncilConfig()
        {
            var config = EnsureAsset<CouncilConfig>(CouncilConfigPath);
            config.ancestorBaseBonus = 0.25;
            config.maxCouncil = 5;
            EditorUtility.SetDirty(config);
            return config;
        }

        // Dynasty PRD Phase 5 (issue #10): §7.10 native-speaker review pass.
        // Virtue names are attested Yoruba words (Sùúrù=patience, Ìgboyà=courage,
        // Àánú=compassion) — ordinary vocabulary, no initiatory titles. Indices are
        // the contract; vow lines authored to match the cultural tone of the game.
        // Final sign-off by a human native-speaker reviewer is required before ship
        // (ART_BIBLE §7.10 — the pipeline does not self-certify).
        private static OriConfig BuildOriConfig()
        {
            var config = EnsureAsset<OriConfig>(OriConfigPath);
            config.virtues = new[]
            {
                new OriVirtue { virtueName = "Sùúrù",  vowLine = "I will hold the long road; haste is not my master." },
                new OriVirtue { virtueName = "Ìgboyà", vowLine = "I will not turn from what stands before me." },
                new OriVirtue { virtueName = "Àánú",   vowLine = "I will spare what I could strike, when sparing is truly right." },
            };
            EditorUtility.SetDirty(config);
            return config;
        }

        // Dynasty PRD Phase 5 (issue #10): production crossroads deck, §7.10-reviewed.
        // Virtue indices: 0=Sùúrù (patience), 1=Ìgboyà (courage), 2=Àánú (mercy).
        // Beats are 1:1 with CrossroadsDeckConfig.beats (same order, same count).
        // Red lines honoured: no initiatory offices, no supreme deity, no witchcraft
        // framing; all scenarios are ordinary life-road dilemmas (ART_BIBLE §7).
        // Human native-speaker sign-off required before launch (ART_BIBLE §7.10).
        //
        // Milestone placement (balance-pass, issue #12): 6 milestones, one per stage tier,
        // so a crossroads surfaces throughout the life arc. Each milestone fires near the
        // midpoint of its stage's active play time, giving ~2 crossroads per daily check-in
        // for a casual idle player.
        private static CrossroadsConfig BuildCrossroadsConfig()
        {
            var config = EnsureAsset<CrossroadsConfig>(CrossroadsConfigPath);
            config.milestoneMantissa = 1.0;
            config.milestoneExponent = 2; // 100 Àṣẹ — Stage 1 boundary, earliest hook
            config.forebearSeedChance = 0.5f;
            config.extraMilestones = new[]
            {
                new CrossroadsMilestone { mantissa = 1.0, exponent = 3 }, // 1 000 — Stage 2
                new CrossroadsMilestone { mantissa = 4.0, exponent = 3 }, // 4 000 — Stage 3
                new CrossroadsMilestone { mantissa = 5.0, exponent = 4 }, // 50 000 — Stage 4
                new CrossroadsMilestone { mantissa = 4.0, exponent = 5 }, // 400 000 — Stage 5
                new CrossroadsMilestone { mantissa = 5.0, exponent = 6 }, // 5 000 000 — Stage 6
            };
            config.deck = new[]
            {
                new CrossroadsCard
                {
                    id = "road_stranger",
                    prompt = "A stranger sits blocking the narrow road, eyes closed. They do not move.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Sit beside them. The road will keep." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Speak firmly — this road must not be held." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Step around and leave them in peace." },
                    }
                },
                new CrossroadsCard
                {
                    id = "elder_call",
                    prompt = "Word comes that an elder of your lineage is ill and calls for you. The road you walk is long, and the task ahead is urgent.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Turn back — the elder may not wait; the task will." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Send word of comfort and press on — you cannot be in two places." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Begin the journey to the elder at once." },
                    }
                },
                new CrossroadsCard
                {
                    id = "market_debt",
                    prompt = "A market-woman calls to you — she says you owe a debt from a deal you cannot remember making.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Wait, and hear the full account before you answer." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Refuse clearly; a debt you did not make is not yours to carry." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Offer something small to close the matter, even if you owe nothing." },
                    }
                },
                new CrossroadsCard
                {
                    id = "market_accusation",
                    prompt = "A man of standing in the marketplace declares publicly that you stole from him. You know you did not. The crowd has heard.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Stand quietly — the truth will find its own weight." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Speak clearly: your name is not his to take without contest." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Ask to settle it in private, to spare him the shame of being wrong in public." },
                    }
                },
                new CrossroadsCard
                {
                    id = "hungry_child",
                    prompt = "A child you do not know sits in the road, clearly hungry. Your provisions are enough for yourself alone.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Sit with them and wait — someone who knows this child will come." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Speak into the market until you find who is responsible for this child." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Share what you have and go lighter for the rest of the road." },
                    }
                },
                new CrossroadsCard
                {
                    id = "brothers_land",
                    prompt = "Two brothers have brought their dispute to you — their father's land, unresolved before he crossed the river. Both have cause; both ask you to judge.",
                    options = new[]
                    {
                        new CrossroadsOption { virtueIndex = 0, optionText = "Hear them out fully, over days if needed, before you speak." },
                        new CrossroadsOption { virtueIndex = 1, optionText = "Name what is right and say it plainly, even if one brother will not thank you." },
                        new CrossroadsOption { virtueIndex = 2, optionText = "Find a middle road where neither brother fully wins, and neither fully loses." },
                    }
                },
            };
            // Forebear seeding (issue #8): explicit assignment prevents a pre-issue-8 asset
            // from silently keeping 0 and disabling the dynasty compounding feature in production.
            config.forebearSeedChance = 0.5f;
            EditorUtility.SetDirty(config);
            return config;
        }

        // Dynasty PRD Phase 5 (issue #10): production personal-name pool, §7.10-reviewed.
        // All names are attested common Yoruba given names — no initiatory titles, no
        // supreme-deity compounds, full diacritics (ART_BIBLE §7.2, §7.9).
        // faithfulFallLine honours a cultivator who held their Ori vow throughout yet
        // fell at the Crossing — warm and dignified, never punitive (ART_BIBLE §7.6).
        // Human native-speaker sign-off required before launch (ART_BIBLE §7.10).
        private static RemembranceConfig BuildRemembranceConfig()
        {
            var config = EnsureAsset<RemembranceConfig>(RemembranceConfigPath);
            config.personalNames = new[]
            {
                "Àyọ̀",      // joy
                "Ẹniọlá",   // person of honour
                "Abíọ́dún",  // born at the festival
                "Ọládélé",  // honour comes home
                "Adéọlá",   // the crown's honour
                "Ìdòwú",    // born after twins — perseverance
                "Bólájí",   // find honour in this
                "Fẹ́mi",    // one who is loved
                "Ọmọ́tọ́lá", // a child worthy of wealth
                "Dúpẹ́",    // give thanks
            };
            config.faithfulFallLine = "Who Faced the River Faithful";
            EditorUtility.SetDirty(config);
            return config;
        }

        // Dynasty PRD Phase 5 (issue #10): production fallen epithets, §7.10-reviewed.
        // One epithet per crossroads beat (1:1 with CrossroadsConfig.deck by index).
        // Each line describes the straying moment with warm dignity — never punitive
        // or shameful (ART_BIBLE §7.6: "Would a grieving family find this honoring?").
        // Human native-speaker sign-off required before launch (ART_BIBLE §7.10).
        private static CrossroadsDeckConfig BuildCrossroadsDeckConfig()
        {
            var config = EnsureAsset<CrossroadsDeckConfig>(CrossroadsDeckConfigPath);
            config.beats = new[]
            {
                new CrossroadsBeat { fallenEpithet = "Who Passed the Stranger By" },    // road_stranger
                new CrossroadsBeat { fallenEpithet = "Who Did Not Turn Back" },         // elder_call
                new CrossroadsBeat { fallenEpithet = "Who Gave Without Remembering" }, // market_debt
                new CrossroadsBeat { fallenEpithet = "Who Let the Word Stand" },        // market_accusation
                new CrossroadsBeat { fallenEpithet = "Who Kept Their Provision" },      // hungry_child
                new CrossroadsBeat { fallenEpithet = "Who Did Not Finish the Judgment" }, // brothers_land
            };
            EditorUtility.SetDirty(config);
            return config;
        }

        private static ContestConfig BuildContestConfig()
        {
            var config = EnsureAsset<ContestConfig>(ContestConfigPath);
            config.stanceTilt = 0.20;
            config.powerWeight = 0.30;
            config.oddsMin = 0.10;
            config.oddsMax = 0.90;
            config.stakeBase = 0.10;
            config.lossSoftness = 0.5;
            config.housePowerMin = 0.75;
            config.housePowerMax = 1.25;
            config.milestoneMantissa = 1.0;
            config.milestoneExponent = 3; // 1 000 Àṣẹ
            config.extraMilestones = new ContestMilestone[0];
            config.holdToConfirmSeconds = 0.8;
            config.revealSeconds = 2.0;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static CultivationStageConfig[] BuildStageConfigs()
        {
            var configs = new CultivationStageConfig[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                StageRow row = Stages[i];
                var config = EnsureAsset<CultivationStageConfig>($"{StageFolder}/{row.File}.asset");
                config.stageName = row.Name;
                config.stageDescription = row.Description;
                config.productionMultiplier = row.Multiplier;
                config.aseThresholdMantissa = row.ThresholdMantissa;
                config.aseThresholdExponent = row.ThresholdExponent;
                config.tier = row.Tier;
                EditorUtility.SetDirty(config);
                configs[i] = config;
            }
            return configs;
        }

        private static PathConfig[] BuildPathConfigs()
        {
            var configs = new PathConfig[Paths.Length];
            for (int i = 0; i < Paths.Length; i++)
            {
                PathRow row = Paths[i];
                var config = EnsureAsset<PathConfig>($"{PathFolder}/{row.File}.asset");
                config.pathName = row.Name;
                config.traditionLabel = row.Tradition;
                config.identityLine = row.Identity;
                config.hookBadge = row.Badge;
                config.offlineBonusLabel = row.OfflineBonusLabel;
                config.pathDescription = row.Description;
                config.aseGenerationModifier = row.Online;
                config.offlineRateModifier = row.Offline;
                config.councilBonusModifier = row.Council;
                config.tribulationType = row.Type;
                EditorUtility.SetDirty(config);
                configs[i] = config;
            }
            return configs;
        }

        private static void EnsureTmpEssentials()
        {
            if (File.Exists(TmpSettingsPath)) return;

            // NOTE: AssetDatabase.ImportPackage is async even in batchmode — the
            // essentials were extracted directly from the tarball instead (see
            // memory: ori-headless-tests). This path only triggers interactively.
            string physical = FileUtil.GetPhysicalPath(TmpEssentialsPackage);
            if (!File.Exists(physical))
            {
                throw new FileNotFoundException($"TMP essentials package not found at {physical}");
            }
            AssetDatabase.ImportPackage(physical, interactive: false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(TmpSettingsPath))
            {
                throw new FileNotFoundException("TMP Essential Resources not imported — extract the tarball manually (see BUILD_PLAN).");
            }
        }

        // Every non-ASCII character that ships in game copy (stage names, tier
        // names, Ìrékọjá, the proverb, Àṣẹ itself) — shared by the fallback
        // baker and the audit.
        private const string RequiredYorubaChars = "ÀàáÁèéìÌíòóọỌẹẸṣṢ̀́ńÒ";
        private const string NotoTtfPath = "Assets/Fonts/NotoSans-Regular.ttf";
        private const string NotoAssetPath = "Assets/Fonts/NotoSans-Regular SDF.asset";

        /// <summary>
        /// GAMEPLAY §7.9: the default TMP font lacks the subdot + tone-mark
        /// glyphs (audited MISSING 9), so build a Noto Sans TMP font asset and
        /// register it as a global TMP fallback. The required glyphs are
        /// pre-baked via TryAddCharacters — which simultaneously PROVES the
        /// source font contains them. Idempotent; non-fatal if the TTF is absent.
        /// </summary>
        private static TMP_FontAsset EnsureNotoFallback()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoAssetPath);
            if (fontAsset == null)
            {
                var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(NotoTtfPath);
                if (sourceFont == null)
                {
                    Debug.LogWarning($"FontFallback: {NotoTtfPath} not found — fallback not created.");
                    return null;
                }

                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 6,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512,
                    AtlasPopulationMode.Dynamic);
                fontAsset.name = "NotoSans-Regular SDF";
                AssetDatabase.CreateAsset(fontAsset, NotoAssetPath);
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            // Pre-bake the Yoruba set (also verifies the source font has them).
            if (!fontAsset.TryAddCharacters(RequiredYorubaChars, out string missing) &&
                !string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"FontFallback: Noto Sans itself lacks: {missing}");
            }
            EditorUtility.SetDirty(fontAsset);

            // Register as a global TMP fallback (SerializedObject — robust across
            // TMP versions; skip if already present).
            var settings = TMP_Settings.instance;
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_fallbackFontAssets");
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset) present = true;
            }
            if (!present)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = fontAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }
            return fontAsset;
        }

        /// <summary>
        /// Orthography audit: every required glyph must be covered by the default
        /// font OR the registered fallback. Warns with exact code points otherwise.
        /// </summary>
        private static void AuditYorubaGlyphCoverage(TMP_FontAsset fallback)
        {
            const string fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (defaultFont == null)
            {
                Debug.LogWarning("FontAudit: default TMP font not found — skipping audit.");
                return;
            }

            int viaFallback = 0;
            var missing = new System.Collections.Generic.List<string>();
            foreach (char c in RequiredYorubaChars)
            {
                if (defaultFont.HasCharacter(c)) continue;
                if (fallback != null && fallback.HasCharacter(c)) { viaFallback++; continue; }
                missing.Add($"U+{(int)c:X4} '{c}'");
            }

            if (missing.Count == 0)
            {
                Debug.Log($"FontAudit: PASS — all Yoruba glyphs covered ({viaFallback} via Noto fallback).");
            }
            else
            {
                Debug.LogWarning("FontAudit: MISSING " + missing.Count + " glyph(s): " +
                                 string.Join(", ", missing) + " — NOT covered by default or fallback.");
            }
        }

        // ================= scene =================

        private static void BuildMainScene(GameplayConfig gameplay, TribulationConfig tribulation,
            CouncilConfig councilConfig, OriConfig oriConfig, CrossroadsConfig crossroadsConfig,
            RemembranceConfig remembranceConfig, CrossroadsDeckConfig crossroadsDeck,
            ContestConfig contestConfig,
            CultivationStageConfig[] stages, PathConfig[] paths)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();
            BuildSystems(gameplay, tribulation, councilConfig, oriConfig, crossroadsConfig,
                remembranceConfig, crossroadsDeck, contestConfig, stages, paths);
            BuildUi(gameplay, tribulation, contestConfig);

            Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"Failed to save scene at {ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.GetUniversalAdditionalCameraData();
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>(); // new Input System only (activeInputHandler: 1)
        }

        private static void BuildSystems(GameplayConfig gameplay, TribulationConfig tribulation,
            CouncilConfig councilConfig, OriConfig oriConfig, CrossroadsConfig crossroadsConfig,
            RemembranceConfig remembranceConfig, CrossroadsDeckConfig crossroadsDeck,
            ContestConfig contestConfig,
            CultivationStageConfig[] stages, PathConfig[] paths)
        {
            var go = new GameObject("Systems");
            AssignConfig(go.AddComponent<SaveManager>(), gameplay);
            AssignConfig(go.AddComponent<AseGenerationSystem>(), gameplay);

            var cultivation = go.AddComponent<CultivationSystem>();
            AssignArray(cultivation, "_stages", stages);
            AssignArray(cultivation, "_paths", paths);
            Assign(cultivation, "_tribulationConfig", tribulation);

            var council = go.AddComponent<AncestralCouncilSystem>();
            Assign(council, "_config", councilConfig);

            var tribulationSystem = go.AddComponent<TribulationSystem>();
            Assign(tribulationSystem, "_config", tribulation);
            Assign(tribulationSystem, "_gameplayConfig", gameplay);
            Assign(tribulationSystem, "_remembranceConfig", remembranceConfig);
            Assign(tribulationSystem, "_crossroadsDeck", crossroadsDeck);

            var oriSystem = go.AddComponent<OriSystem>();
            Assign(oriSystem, "_config", oriConfig);

            var crossroadsSystem = go.AddComponent<CrossroadsSystem>();
            Assign(crossroadsSystem, "_config", crossroadsConfig);

            var marketplaceSystem = go.AddComponent<MarketplaceSystem>();
            Assign(marketplaceSystem, "_config", contestConfig);
            Assign(marketplaceSystem, "_remembranceConfig", remembranceConfig);

            go.AddComponent<OriAscendant.Save.CloudSaveManager>();
            go.AddComponent<OriAscendant.Audio.AudioManager>();
            // ProceduralAmbience also self-bootstraps via RuntimeInitializeOnLoadMethod
            // (matching MainScreenSkin); the component here ensures it is serialised into
            // the scene for builds that do not rely on the bootstrap path.
            go.AddComponent<OriAscendant.Audio.ProceduralAmbience>();

            AssignConfig(go.AddComponent<GameManager>(), gameplay);
        }

        private static void BuildUi(GameplayConfig config, TribulationConfig tribulation,
            ContestConfig contestConfig)
        {
            // ---- root canvas ----
            var canvasGo = new GameObject("MainCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvasGo.GetComponent<RectTransform>();

            // Storm vignette — first sibling, renders behind every zone. Alpha is
            // driven by MainScreenController per the ambient fractions.
            var vignette = MakeImage(root, "StormVignette", new Color(0.06f, 0.09f, 0.16f, 0f));
            Stretch(vignette.rectTransform);

            // Zone 1 — header.
            var header = Zone(root, "HeaderZone", 0.055f, 0.105f);
            var genText = MakeText(header, "GenerationText", "Gen 1", 16f, TextAlignmentOptions.MidlineLeft, TextDim);
            Inset(genText.rectTransform, left: 20f);
            var settingsBtn = MakeButton(header, "SettingsButton", "⚙", Panel, 14f);
            var settingsRt = (RectTransform)settingsBtn.transform;
            settingsRt.anchorMin = new Vector2(1f, 0f);
            settingsRt.anchorMax = new Vector2(1f, 1f);
            settingsRt.pivot = new Vector2(1f, 0.5f);
            settingsRt.sizeDelta = new Vector2(44f, 0f);
            settingsRt.anchoredPosition = new Vector2(-16f, 0f); // ACTIVE from Phase D (opens Settings)
            var ojaNavBtn = MakeButton(header, "OjaNavButton", "Ọjà", Panel, 13f);
            var ojaNavBtnRt = (RectTransform)ojaNavBtn.transform;
            ojaNavBtnRt.anchorMin = new Vector2(1f, 0f);
            ojaNavBtnRt.anchorMax = new Vector2(1f, 1f);
            ojaNavBtnRt.pivot = new Vector2(1f, 0.5f);
            ojaNavBtnRt.sizeDelta = new Vector2(52f, 0f);
            ojaNavBtnRt.anchoredPosition = new Vector2(-68f, 0f);

            // Zone 2 — counter on its own nested canvas (1Hz rebuild isolation).
            // ponytail: clean non-overlapping stack using MainScreenLayout constants.
            var counterZone = Zone(root, "CounterZone", 0.10f, 0.20f);
            var counterCanvasGo = new GameObject("CounterCanvas", typeof(RectTransform));
            var counterCanvasRt = (RectTransform)counterCanvasGo.transform;
            counterCanvasRt.SetParent(counterZone, false);
            Stretch(counterCanvasRt);
            counterCanvasGo.AddComponent<Canvas>();

            // Number + rate only. The "ÀṢẸ" eyebrow and hairline rule are drawn by
            // MainScreenSkin at runtime (single source — a static copy here would
            // double them). The number band reserves the eyebrow space above.
            var aseCounter = MakeText(counterCanvasRt, "AseCounterText", "0",
                TypographicScale.Display, TextAlignmentOptions.Center, Text);
            SetBand(aseCounter.rectTransform,
                MainScreenLayout.CounterNumberBottom, MainScreenLayout.CounterNumberTop);

            var rateText = MakeText(counterCanvasRt, "RateText", "+0 Àṣẹ/s",
                TypographicScale.BodySm, TextAlignmentOptions.Center, Gold);
            SetBand(rateText.rectTransform,
                MainScreenLayout.CounterRateBottom, MainScreenLayout.CounterRateTop);

            // Zone 3 — identity line + path badge + Ori badge + steadfastness.
            // ponytail: three non-overlapping horizontal lanes; SteadfastnessText on a
            // distinct centre-bottom sub-lane so it never shares inset+align with OriBadge.
            var identity = Zone(root, "IdentityZone", 0.20f, 0.24f);

            // StageText — centre lane (30–70 % width), H2, so edge badges never clip it.
            var stageText = MakeText(identity, "StageText", "Stage 1",
                TypographicScale.H2, TextAlignmentOptions.Center, Text);
            stageText.rectTransform.anchorMin = new Vector2(MainScreenLayout.IdentityStageCentreXMin, 0f);
            stageText.rectTransform.anchorMax = new Vector2(MainScreenLayout.IdentityStageCentreXMax, 1f);
            stageText.rectTransform.offsetMin = Vector2.zero;
            stageText.rectTransform.offsetMax = Vector2.zero;

            // PathBadge — right lane (IdentityPathBadgeXMin → 1).
            var pathBadge = MakeText(identity, "PathBadge", "",
                TypographicScale.Label, TextAlignmentOptions.MidlineRight, Gold);
            pathBadge.rectTransform.anchorMin = new Vector2(MainScreenLayout.IdentityPathBadgeXMin, 0f);
            pathBadge.rectTransform.anchorMax = new Vector2(1f, 1f);
            pathBadge.rectTransform.offsetMin = Vector2.zero;
            pathBadge.rectTransform.offsetMax = new Vector2(-SpacingScale.Md, 0f);
            pathBadge.gameObject.SetActive(false); // shown once a path is chosen

            // OriBadge — left lane (0 → IdentityOriBadgeXMax).
            var oriBadge = MakeText(identity, "OriBadge", "",
                TypographicScale.Label, TextAlignmentOptions.MidlineLeft, Gold);
            oriBadge.rectTransform.anchorMin = new Vector2(0f, 0f);
            oriBadge.rectTransform.anchorMax = new Vector2(MainScreenLayout.IdentityOriBadgeXMax, 1f);
            oriBadge.rectTransform.offsetMin = new Vector2(SpacingScale.Md, 0f);
            oriBadge.rectTransform.offsetMax = Vector2.zero;
            oriBadge.gameObject.SetActive(false); // shown once an Ori is vowed

            // SteadfastnessText — centre-bottom sub-lane; Caption, distinct from OriBadge lane.
            // Hidden until the first crossroads has been resolved (oriTrials > 0).
            var steadfastnessText = MakeText(identity, "SteadfastnessText", "",
                TypographicScale.Caption, TextAlignmentOptions.Center, TextDim);
            steadfastnessText.rectTransform.anchorMin =
                new Vector2(MainScreenLayout.IdentityStageCentreXMin, 0f);
            steadfastnessText.rectTransform.anchorMax =
                new Vector2(MainScreenLayout.IdentityStageCentreXMax,
                            MainScreenLayout.IdentitySteadfastnessTop);
            steadfastnessText.rectTransform.offsetMin = Vector2.zero;
            steadfastnessText.rectTransform.offsetMax = Vector2.zero;
            steadfastnessText.gameObject.SetActive(false);

            // Zone 4 — portrait = the channel-tap target (GAMEPLAY §5.3).
            // The Image is fully transparent — it is a raycast-only hit area.
            // All visuals (aura, silhouette, motes) come from child Images that
            // MainScreenSkin builds at runtime; no sprite is ever assigned here.
            var portraitZone = Zone(root, "PortraitZone", 0.24f, 0.58f);
            var portrait = MakeImage(portraitZone, "PortraitImage", Color.clear);
            var portraitRt = portrait.rectTransform;
            portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRt.sizeDelta = new Vector2(240f, 240f);
            portrait.raycastTarget = true;
            var portraitButton = portrait.gameObject.AddComponent<Button>();
            portraitButton.targetGraphic = portrait;
            portraitButton.transition = Selectable.Transition.None;

            // One-time channel hint (seenFlags bit 0).
            var hint = MakeText(portraitZone, "ChannelHint", "Touch your cultivator to channel àṣẹ", 14f,
                TextAlignmentOptions.Center, Gold);
            SetBand(hint.rectTransform, 0.00f, 0.12f);
            hint.gameObject.SetActive(false);

            // Zone 6 — primary CTA (ACTIVE from Phase B; tribulation flow Phase C).
            // Zone 5 (progress bar) removed — vessel is the gauge (issue #28).
            var ctaZone = Zone(root, "CtaZone", 0.66f, 0.76f);
            var advance = MakeButton(ctaZone, "AdvanceButton", "Advance", Gold, 22f);
            var advanceRt = (RectTransform)advance.transform;
            Stretch(advanceRt);
            advanceRt.offsetMin = new Vector2(24f, 8f);
            advanceRt.offsetMax = new Vector2(-24f, -8f);
            advance.interactable = false;
            var advanceLabel = advance.GetComponentInChildren<TMP_Text>();

            // Zone 7 — Ancestral Council strip (ACTIVE from Phase C).
            var councilZone = Zone(root, "CouncilZone", 0.78f, 0.88f);
            var stripTapTarget = MakeImage(councilZone, "StripTapTarget", new Color(1f, 1f, 1f, 0.01f));
            Stretch(stripTapTarget.rectTransform);
            stripTapTarget.raycastTarget = true;
            var stripButton = stripTapTarget.gameObject.AddComponent<Button>();
            stripButton.targetGraphic = stripTapTarget;
            stripButton.transition = Selectable.Transition.None;

            var strip = new GameObject("CouncilStrip", typeof(RectTransform));
            var stripRt = (RectTransform)strip.transform;
            stripRt.SetParent(councilZone, false);
            Stretch(stripRt);
            var layout = strip.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var councilSlots = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                councilSlots[i] = MakeImage(stripRt, $"CouncilSlot{i + 1}", PanelLine);
                councilSlots[i].rectTransform.sizeDelta = new Vector2(56f, 56f);
            }

            // ---- Path choice modal (controller active, root hidden) ----
            var pathController = new GameObject("PathScreen", typeof(RectTransform));
            var pathControllerRt = (RectTransform)pathController.transform;
            pathControllerRt.SetParent(root, false);
            Stretch(pathControllerRt);

            var pathRoot = new GameObject("PathRoot", typeof(RectTransform));
            var pathRootRt = (RectTransform)pathRoot.transform;
            pathRootRt.SetParent(pathControllerRt, false);
            Stretch(pathRootRt);
            var pathDim = MakeImage(pathRootRt, "Dim",
                new Color(0f, 0f, 0f, OpacitySpec.Scrim)); // ponytail: OpacitySpec.Scrim
            Stretch(pathDim.rectTransform);
            pathDim.raycastTarget = true;

            var pathPanel = MakeImage(pathRootRt, "Panel", Panel);
            var pathPanelRt = pathPanel.rectTransform;
            pathPanelRt.anchorMin = new Vector2(0.05f, 0.10f);
            pathPanelRt.anchorMax = new Vector2(0.95f, 0.90f);
            pathPanelRt.offsetMin = Vector2.zero;
            pathPanelRt.offsetMax = Vector2.zero;

            // ponytail: modal anatomy per ADR-0007 — H1 title, Body prompt gap,
            // SpacingScale gaps, confirm pill band from MainScreenLayout.
            var pathTitle = MakeText(pathPanelRt, "Title", "The initiate must choose a Path",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(pathTitle.rectTransform, MainScreenLayout.ModalTitleBottom, MainScreenLayout.ModalTitleTop);

            var cards = new PathCardView[3];
            float[][] cardBands =
            {
                new[] { MainScreenLayout.ModalCard0Bottom, MainScreenLayout.ModalCard0Top },
                new[] { MainScreenLayout.ModalCard1Bottom, MainScreenLayout.ModalCard1Top },
                new[] { MainScreenLayout.ModalCard2Bottom, MainScreenLayout.ModalCard2Top },
            };
            for (int i = 0; i < 3; i++)
            {
                cards[i] = BuildPathCard(pathPanelRt, $"PathCard{i}", cardBands[i][0], cardBands[i][1]);
            }

            var confirm = MakeButton(pathPanelRt, "ConfirmButton", "Choose a Path", Gold, TypographicScale.H2);
            var confirmRt = (RectTransform)confirm.transform;
            confirmRt.anchorMin = new Vector2(0.10f, MainScreenLayout.ModalConfirmBottom);
            confirmRt.anchorMax = new Vector2(0.90f, MainScreenLayout.ModalConfirmTop);
            confirmRt.offsetMin = Vector2.zero;
            confirmRt.offsetMax = Vector2.zero;
            confirm.interactable = false;
            var confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            pathRoot.SetActive(false);

            var pathScreen = pathController.AddComponent<PathScreenView>();
            Assign(pathScreen, "_root", pathRoot);
            AssignArray(pathScreen, "_cards", cards);
            Assign(pathScreen, "_confirmButton", confirm);
            Assign(pathScreen, "_confirmLabel", confirmLabel);

            // ---- Ori choice modal (Dynasty PRD Phase 1, slice 1) ----
            OriScreenView oriScreen = BuildOriScreenUi(root);

            // ---- Crossroads modal (Dynasty PRD Phase 1, slice 2a) ----
            CrossroadsScreenView crossroadsScreen = BuildCrossroadsScreenUi(root);

            // ---- Phase C screens ----
            TribulationScreen tribulationScreen = BuildTribulationScreenUi(root, tribulation);
            ChronicleScreenView chronicleScreen = BuildChronicleScreenUi(root);
            CouncilScreenView councilScreen = BuildCouncilScreenUi(root, chronicleScreen);
            SettingsScreenView settingsScreen = BuildSettingsUi(root); // Phase D
            OjaScreenView ojaScreen = BuildOjaScreenUi(root); // issue #41
            ContestScreenView contestScreen = BuildContestScreenUi(root, contestConfig); // issue #43

            var stripView = canvasGo.AddComponent<CouncilStripView>();
            AssignArray(stripView, "_slots", councilSlots);
            Assign(stripView, "_stripButton", stripButton);
            Assign(stripView, "_councilScreen", councilScreen);

            // ---- Welcome Back modal ----
            var modalController = new GameObject("WelcomeBackModal", typeof(RectTransform));
            var modalControllerRt = (RectTransform)modalController.transform;
            modalControllerRt.SetParent(root, false);
            Stretch(modalControllerRt);

            var modalRoot = new GameObject("ModalRoot", typeof(RectTransform));
            var modalRootRt = (RectTransform)modalRoot.transform;
            modalRootRt.SetParent(modalControllerRt, false);
            Stretch(modalRootRt);
            // ponytail: consistent OpacitySpec.Scrim; token text sizes.
            var dim = MakeImage(modalRootRt, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var card = MakeImage(modalRootRt, "Card", Panel);
            var cardRt = card.rectTransform;
            // Extended downward from 0.30 to make room for the rewarded "watch to
            // double" button (top edge unchanged at 0.70). Every band below is
            // reweighted against the taller card so each element keeps its exact
            // prior absolute on-screen position — zero visual change to what
            // already shipped; the freed strip at the bottom is new.
            cardRt.anchorMin = new Vector2(0.08f, 0.22f);
            cardRt.anchorMax = new Vector2(0.92f, 0.70f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            var wbHeader = MakeText(cardRt, "HeaderText", "Your Orí kept watch",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(wbHeader.rectTransform, 0.833f, 0.975f);
            var timeAway = MakeText(cardRt, "TimeAwayText", "Away —",
                TypographicScale.Body, TextAlignmentOptions.Center, TextDim);
            SetBand(timeAway.rectTransform, 0.717f, 0.833f);
            var earned = MakeText(cardRt, "EarnedText", "+0 Àṣẹ",
                TypographicScale.H1, TextAlignmentOptions.Center, Text);
            SetBand(earned.rectTransform, 0.533f, 0.717f);
            var bonusLine = MakeText(cardRt, "BonusLineText", "",
                TypographicScale.BodySm, TextAlignmentOptions.Center, Gold);
            SetBand(bonusLine.rectTransform, 0.442f, 0.533f);
            bonusLine.gameObject.SetActive(false);
            var rateContext = MakeText(cardRt, "RateContextText", "at 0 Àṣẹ/s",
                TypographicScale.BodySm, TextAlignmentOptions.Center, TextDim);
            SetBand(rateContext.rectTransform, 0.350f, 0.442f);
            var collect = MakeButton(cardRt, "CollectButton", "Collect", Gold, TypographicScale.H2);
            var collectRt = (RectTransform)collect.transform;
            collectRt.anchorMin = new Vector2(0.10f, 0.20f);
            collectRt.anchorMax = new Vector2(0.90f, 0.325f);
            collectRt.offsetMin = Vector2.zero;
            collectRt.offsetMax = Vector2.zero;

            // Rewarded-ad opt-in — hidden by RefreshDoubleButton until a rewarded
            // ad is actually loaded (ads inert / unconfigured today → never shown).
            var watchDouble = MakeButton(cardRt, "DoubleButton", "Watch to double", Gold, TypographicScale.BodySm);
            var watchDoubleRt = (RectTransform)watchDouble.transform;
            watchDoubleRt.anchorMin = new Vector2(0.10f, 0.02f);
            watchDoubleRt.anchorMax = new Vector2(0.90f, 0.17f);
            watchDoubleRt.offsetMin = Vector2.zero;
            watchDoubleRt.offsetMax = Vector2.zero;
            var watchDoubleLabel = watchDouble.GetComponentInChildren<TMP_Text>();
            watchDouble.gameObject.SetActive(false);
            modalRoot.SetActive(false);

            // ---- Title screen (topmost) ----
            var titleController = new GameObject("TitleScreen", typeof(RectTransform));
            var titleControllerRt = (RectTransform)titleController.transform;
            titleControllerRt.SetParent(root, false);
            Stretch(titleControllerRt);

            var titleRoot = new GameObject("TitleRoot", typeof(RectTransform));
            var titleRootRt = (RectTransform)titleRoot.transform;
            titleRootRt.SetParent(titleControllerRt, false);
            Stretch(titleRootRt);
            var titleBg = MakeImage(titleRootRt, "TitleBackground", Bg);
            Stretch(titleBg.rectTransform);
            titleBg.raycastTarget = true;

            var titleText = MakeText(titleRootRt, "TitleText", "Ori Ascendant", 44f, TextAlignmentOptions.Center, Gold);
            SetBand(titleText.rectTransform, 0.58f, 0.74f);
            var proverb = MakeText(titleRootRt, "ProverbText",
                "Ayé l'ọjà, ọ̀run nilé\n<size=70%>The world is a marketplace; ọ̀run is home.</size>",
                16f, TextAlignmentOptions.Center, TextDim);
            SetBand(proverb.rectTransform, 0.44f, 0.56f);
            var touchText = MakeText(titleRootRt, "TouchToBeginText", "Touch to begin", 18f, TextAlignmentOptions.Center, Text);
            SetBand(touchText.rectTransform, 0.16f, 0.24f);

            var beginButton = titleBg.gameObject.AddComponent<Button>();
            beginButton.targetGraphic = titleBg;
            beginButton.transition = Selectable.Transition.None;

            // ---- components + serialized wiring ----
            var view = canvasGo.AddComponent<MainScreenView>();
            Assign(view, "_aseCounterText", aseCounter);
            Assign(view, "_rateText", rateText);
            Assign(view, "_stageText", stageText);
            Assign(view, "_generationText", genText);
            Assign(view, "_pathBadge", pathBadge);
            Assign(view, "_oriBadge", oriBadge);
            Assign(view, "_steadfastnessText", steadfastnessText);

            var controller = canvasGo.AddComponent<MainScreenController>();
            Assign(controller, "_config", config);
            Assign(controller, "_tribulationConfig", tribulation);
            Assign(controller, "_stormVignette", vignette);
            Assign(controller, "_tribulationScreen", tribulationScreen);
            Assign(controller, "_ctaRoot", ctaZone.gameObject);
            Assign(controller, "_advanceButton", advance);
            Assign(controller, "_advanceLabel", advanceLabel);
            Assign(controller, "_portraitButton", portraitButton);
            Assign(controller, "_floatingTextAnchor", portraitZone);
            Assign(controller, "_hintRoot", hint.gameObject);
            Assign(controller, "_pathScreen", pathScreen);
            Assign(controller, "_oriScreen", oriScreen);
            Assign(controller, "_crossroadsScreen", crossroadsScreen);
            Assign(controller, "_settingsButton", settingsBtn);
            Assign(controller, "_settingsScreen", settingsScreen);
            Assign(controller, "_ojaButton", ojaNavBtn);
            Assign(controller, "_ojaScreen", ojaScreen);
            Assign(controller, "_contestScreen", contestScreen);

            var modal = modalController.AddComponent<WelcomeBackModal>();
            Assign(modal, "_config", config);
            Assign(modal, "_modalRoot", modalRoot);
            Assign(modal, "_timeAwayText", timeAway);
            Assign(modal, "_earnedText", earned);
            Assign(modal, "_rateContextText", rateContext);
            Assign(modal, "_bonusLineText", bonusLine);
            Assign(modal, "_collectButton", collect);
            Assign(modal, "_doubleButton", watchDouble);
            Assign(modal, "_doubleButtonLabel", watchDoubleLabel);

            var title = titleController.AddComponent<TitleScreen>();
            Assign(title, "_root", titleRoot);
            Assign(title, "_beginButton", beginButton);
        }

        private static PathCardView BuildPathCard(RectTransform parent, string name, float bottomFrac, float topFrac)
        {
            var cardBg = MakeImage(parent, name, Panel);
            SetBand(cardBg.rectTransform, bottomFrac, topFrac);
            var inner = cardBg.rectTransform;
            inner.offsetMin = new Vector2(12f, 0f);
            inner.offsetMax = new Vector2(-12f, 0f);
            cardBg.raycastTarget = true;
            cardBg.color = Panel;

            var button = cardBg.gameObject.AddComponent<Button>();
            button.targetGraphic = cardBg;

            // ponytail: token sizes — Body for name, Caption for tradition, Label for identity.
            var nameText = MakeText(inner, "Name", "", TypographicScale.Body, TextAlignmentOptions.TopLeft, Text);
            SetBand(nameText.rectTransform, 0.62f, 0.95f);
            InsetX(nameText.rectTransform, SpacingScale.Sm);
            var tradition = MakeText(inner, "Tradition", "", TypographicScale.Caption, TextAlignmentOptions.TopLeft, TextDim);
            SetBand(tradition.rectTransform, 0.44f, 0.62f);
            InsetX(tradition.rectTransform, SpacingScale.Sm);
            var identityText = MakeText(inner, "Identity", "", TypographicScale.Label, TextAlignmentOptions.TopLeft, Gold);
            SetBand(identityText.rectTransform, 0.06f, 0.44f);
            InsetX(identityText.rectTransform, SpacingScale.Sm);
            identityText.enableWordWrapping = true;

            var view = cardBg.gameObject.AddComponent<PathCardView>();
            Assign(view, "_button", button);
            Assign(view, "_background", cardBg);
            Assign(view, "_nameText", nameText);
            Assign(view, "_traditionText", tradition);
            Assign(view, "_identityText", identityText);
            return view;
        }

        private static OriScreenView BuildOriScreenUi(RectTransform root)
        {
            var controller = new GameObject("OriScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var oriRoot = new GameObject("OriRoot", typeof(RectTransform));
            var oriRootRt = (RectTransform)oriRoot.transform;
            oriRootRt.SetParent(controllerRt, false);
            Stretch(oriRootRt);
            // ponytail: OpacitySpec.Scrim + modal anatomy (ADR-0007).
            var dim = MakeImage(oriRootRt, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = MakeImage(oriRootRt, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.05f, 0.10f);
            panelRt.anchorMax = new Vector2(0.95f, 0.90f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "Àkùnlẹ̀yàn — vow your Ori",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, MainScreenLayout.ModalTitleBottom, MainScreenLayout.ModalTitleTop);

            var cards = new OriCardView[3];
            float[][] cardBands =
            {
                new[] { MainScreenLayout.ModalCard0Bottom, MainScreenLayout.ModalCard0Top },
                new[] { MainScreenLayout.ModalCard1Bottom, MainScreenLayout.ModalCard1Top },
                new[] { MainScreenLayout.ModalCard2Bottom, MainScreenLayout.ModalCard2Top },
            };
            for (int i = 0; i < 3; i++)
            {
                cards[i] = BuildOriCard(panelRt, $"OriCard{i}", cardBands[i][0], cardBands[i][1]);
            }

            var confirm = MakeButton(panelRt, "ConfirmButton", "Choose an Ori", Gold, TypographicScale.H2);
            var confirmRt = (RectTransform)confirm.transform;
            confirmRt.anchorMin = new Vector2(0.10f, MainScreenLayout.ModalConfirmBottom);
            confirmRt.anchorMax = new Vector2(0.90f, MainScreenLayout.ModalConfirmTop);
            confirmRt.offsetMin = Vector2.zero;
            confirmRt.offsetMax = Vector2.zero;
            confirm.interactable = false;
            var confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            oriRoot.SetActive(false);

            var screen = controller.AddComponent<OriScreenView>();
            Assign(screen, "_root", oriRoot);
            AssignArray(screen, "_cards", cards);
            Assign(screen, "_confirmButton", confirm);
            Assign(screen, "_confirmLabel", confirmLabel);
            return screen;
        }

        private static OriCardView BuildOriCard(RectTransform parent, string name, float bottomFrac, float topFrac)
        {
            var cardBg = MakeImage(parent, name, Panel);
            SetBand(cardBg.rectTransform, bottomFrac, topFrac);
            var inner = cardBg.rectTransform;
            inner.offsetMin = new Vector2(12f, 0f);
            inner.offsetMax = new Vector2(-12f, 0f);
            cardBg.raycastTarget = true;
            cardBg.color = Panel;

            var button = cardBg.gameObject.AddComponent<Button>();
            button.targetGraphic = cardBg;

            // ponytail: token sizes matching PathCard voice.
            var nameText = MakeText(inner, "Name", "", TypographicScale.Body, TextAlignmentOptions.TopLeft, Text);
            SetBand(nameText.rectTransform, 0.62f, 0.95f);
            InsetX(nameText.rectTransform, SpacingScale.Sm);
            var vowText = MakeText(inner, "Vow", "", TypographicScale.Label, TextAlignmentOptions.TopLeft, Gold);
            SetBand(vowText.rectTransform, 0.06f, 0.60f);
            InsetX(vowText.rectTransform, SpacingScale.Sm);
            vowText.enableWordWrapping = true;

            var view = cardBg.gameObject.AddComponent<OriCardView>();
            Assign(view, "_button", button);
            Assign(view, "_background", cardBg);
            Assign(view, "_nameText", nameText);
            Assign(view, "_vowText", vowText);
            return view;
        }

        // ---- Crossroads modal (Dynasty PRD Phase 1, slice 2a) ----
        // Mirrors the OriScreenView pattern: 4 option slots (most seed cards use 2–3),
        // hidden root, confirm button. The options bind dynamically when Show() is called.
        private static CrossroadsScreenView BuildCrossroadsScreenUi(RectTransform root)
        {
            var controller = new GameObject("CrossroadsScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var crossroadsRoot = new GameObject("CrossroadsRoot", typeof(RectTransform));
            var crossroadsRootRt = (RectTransform)crossroadsRoot.transform;
            crossroadsRootRt.SetParent(controllerRt, false);
            Stretch(crossroadsRootRt);
            // ponytail: OpacitySpec.Scrim + modal anatomy bands (ADR-0007).
            var dim = MakeImage(crossroadsRootRt, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = MakeImage(crossroadsRootRt, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.05f, 0.06f);
            panelRt.anchorMax = new Vector2(0.95f, 0.94f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "A crossroads",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, MainScreenLayout.ModalTitleBottom, MainScreenLayout.ModalTitleTop);

            var prompt = MakeText(panelRt, "Prompt", "", TypographicScale.Body, TextAlignmentOptions.Center, Text);
            SetBand(prompt.rectTransform,
                MainScreenLayout.CrossroadsPromptBottom, MainScreenLayout.CrossroadsPromptTop);
            InsetX(prompt.rectTransform, SpacingScale.Sm);
            prompt.enableWordWrapping = true;

            // 3 option slots (seed deck uses up to 3 options per card).
            const int maxOptions = 3;
            var optionViews = new CrossroadsOptionView[maxOptions];
            float[][] optionBands =
            {
                new[] { MainScreenLayout.CrossroadsOption0Bottom, MainScreenLayout.CrossroadsOption0Top },
                new[] { MainScreenLayout.CrossroadsOption1Bottom, MainScreenLayout.CrossroadsOption1Top },
                new[] { MainScreenLayout.CrossroadsOption2Bottom, MainScreenLayout.CrossroadsOption2Top },
            };
            for (int i = 0; i < maxOptions; i++)
            {
                optionViews[i] = BuildCrossroadsOptionView(panelRt, $"OptionCard{i}",
                    optionBands[i][0], optionBands[i][1]);
            }

            var confirm = MakeButton(panelRt, "ConfirmButton", "Choose your path",
                Gold, TypographicScale.Body);
            var confirmRt = (RectTransform)confirm.transform;
            confirmRt.anchorMin = new Vector2(0.10f, MainScreenLayout.CrossroadsConfirmBottom);
            confirmRt.anchorMax = new Vector2(0.90f, MainScreenLayout.CrossroadsConfirmTop);
            confirmRt.offsetMin = Vector2.zero;
            confirmRt.offsetMax = Vector2.zero;
            confirm.interactable = false;
            var confirmLabel = confirm.GetComponentInChildren<TMP_Text>();
            crossroadsRoot.SetActive(false);

            var screen = controller.AddComponent<CrossroadsScreenView>();
            Assign(screen, "_root", crossroadsRoot);
            Assign(screen, "_promptText", prompt);
            AssignArray(screen, "_optionViews", optionViews);
            Assign(screen, "_confirmButton", confirm);
            Assign(screen, "_confirmLabel", confirmLabel);
            return screen;
        }

        private static CrossroadsOptionView BuildCrossroadsOptionView(
            RectTransform parent, string name, float bottomFrac, float topFrac)
        {
            var cardBg = MakeImage(parent, name, Panel);
            SetBand(cardBg.rectTransform, bottomFrac, topFrac);
            var inner = cardBg.rectTransform;
            inner.offsetMin = new Vector2(12f, 0f);
            inner.offsetMax = new Vector2(-12f, 0f);
            cardBg.raycastTarget = true;

            var button = cardBg.gameObject.AddComponent<Button>();
            button.targetGraphic = cardBg;

            // ponytail: BodySm for option text — readable without competing with prompt.
            var optionText = MakeText(inner, "OptionText", "", TypographicScale.BodySm, TextAlignmentOptions.MidlineLeft, Text);
            SetBand(optionText.rectTransform, 0.10f, 0.90f);
            InsetX(optionText.rectTransform, SpacingScale.Sm);
            optionText.enableWordWrapping = true;

            var view = cardBg.gameObject.AddComponent<CrossroadsOptionView>();
            Assign(view, "_button", button);
            Assign(view, "_background", cardBg);
            Assign(view, "_optionText", optionText);
            return view;
        }

        private static TribulationScreen BuildTribulationScreenUi(RectTransform root, TribulationConfig tribulation)
        {
            var controller = new GameObject("TribulationScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            // -- confirm sheet --
            var confirmRoot = NewStretched(controllerRt, "ConfirmRoot");
            var confirmDim = MakeImage(confirmRoot, "Dim", new Color(0f, 0f, 0f, 0.8f));
            Stretch(confirmDim.rectTransform);
            confirmDim.raycastTarget = true;
            var sheet = MakeImage(confirmRoot, "Sheet", Panel);
            var sheetRt = sheet.rectTransform;
            sheetRt.anchorMin = new Vector2(0.07f, 0.16f);
            sheetRt.anchorMax = new Vector2(0.93f, 0.84f);
            sheetRt.offsetMin = Vector2.zero;
            sheetRt.offsetMax = Vector2.zero;

            var sheetTitle = MakeText(sheetRt, "Title", "Ìrékọjá — The Crossing", 22f, TextAlignmentOptions.Center, Gold);
            SetBand(sheetTitle.rectTransform, 0.88f, 0.98f);
            // ADR-0004 "always shown": the derived chance, foregrounded under the title.
            var chanceText = MakeText(sheetRt, "ChanceToAscendText", "Chance to ascend: —", 17f,
                TextAlignmentOptions.Center, Gold);
            SetBand(chanceText.rectTransform, 0.78f, 0.88f);
            var ascendLine = MakeText(sheetRt, "AscendLine", "", 15f, TextAlignmentOptions.Center, Text);
            SetBand(ascendLine.rectTransform, 0.66f, 0.78f);
            InsetX(ascendLine.rectTransform, 14f);
            var fallLine = MakeText(sheetRt, "FallLine", "", 15f, TextAlignmentOptions.Center, Text);
            SetBand(fallLine.rectTransform, 0.54f, 0.66f);
            InsetX(fallLine.rectTransform, 14f);
            var eitherWay = MakeText(sheetRt, "EitherWay", "Either way, your lineage grows stronger.", 14f,
                TextAlignmentOptions.Center, Gold);
            SetBand(eitherWay.rectTransform, 0.54f, 0.62f);
            var mythic = MakeText(sheetRt, "MythicLine",
                "<i>Most who are ready ascend; those who fall are honored among the ancestors.</i>",
                13f, TextAlignmentOptions.Center, TextDim);
            SetBand(mythic.rectTransform, 0.44f, 0.54f);
            InsetX(mythic.rectTransform, 14f);

            var oddsToggle = MakeButton(sheetRt, "OddsToggle", "?", PanelLine, 14f);
            var oddsToggleRt = (RectTransform)oddsToggle.transform;
            oddsToggleRt.anchorMin = new Vector2(1f, 1f);
            oddsToggleRt.anchorMax = new Vector2(1f, 1f);
            oddsToggleRt.pivot = new Vector2(1f, 1f);
            oddsToggleRt.sizeDelta = new Vector2(34f, 34f);
            oddsToggleRt.anchoredPosition = new Vector2(-8f, -8f);

            var oddsPanel = MakeImage(sheetRt, "OddsPanel", PanelLine);
            SetBand(oddsPanel.rectTransform, 0.28f, 0.44f);
            InsetX(oddsPanel.rectTransform, 12f);
            var oddsText = MakeText(oddsPanel.rectTransform, "OddsText",
                "Your ascend chance rises with your steadfastness — held faithful to your Ori at each Crossroads.\nEven the steadfast can fall; even the wavering can ascend. Both outcomes grant an Ancestor.",
                12f, TextAlignmentOptions.Center, Text);
            Stretch(oddsText.rectTransform);
            oddsPanel.gameObject.SetActive(false);

            var holdButton = MakeButton(sheetRt, "HoldButton", "", Gold, 16f);
            var holdRt = (RectTransform)holdButton.transform;
            holdRt.anchorMin = new Vector2(0.10f, 0.10f);
            holdRt.anchorMax = new Vector2(0.90f, 0.24f);
            holdRt.offsetMin = Vector2.zero;
            holdRt.offsetMax = Vector2.zero;
            var holdComponent = holdButton.gameObject.AddComponent<HoldButton>();
            var holdBack = holdButton.GetComponent<Image>();
            holdBack.color = PanelLine;
            var holdFill = MakeImage(holdRt, "HoldFill", Gold);
            Stretch(holdFill.rectTransform);
            holdFill.type = Image.Type.Filled;
            holdFill.fillMethod = Image.FillMethod.Horizontal;
            holdFill.fillAmount = 0f;
            holdFill.sprite = WhiteSprite();
            var holdLabel = MakeText(holdRt, "HoldLabel", "Hold to face the Crossing", 16f,
                TextAlignmentOptions.Center, Text);
            Stretch(holdLabel.rectTransform);

            var notYet = MakeButton(sheetRt, "NotYetButton", "Not yet", Panel, 13f);
            var notYetRt = (RectTransform)notYet.transform;
            notYetRt.anchorMin = new Vector2(0.32f, 0.01f);
            notYetRt.anchorMax = new Vector2(0.68f, 0.08f);
            notYetRt.offsetMin = Vector2.zero;
            notYetRt.offsetMax = Vector2.zero;
            confirmRoot.gameObject.SetActive(false);

            // -- ceremony overlay --
            var ceremonyRoot = NewStretched(controllerRt, "CeremonyRoot");
            var ceremonyBg = MakeImage(ceremonyRoot, "CeremonyBg", new Color(0.04f, 0.05f, 0.08f, 1f));
            Stretch(ceremonyBg.rectTransform);
            ceremonyBg.raycastTarget = true;
            var tapCatcher = ceremonyBg.gameObject.AddComponent<Button>();
            tapCatcher.targetGraphic = ceremonyBg;
            tapCatcher.transition = Selectable.Transition.None;
            var flash = MakeImage(ceremonyRoot, "Flash", new Color(1f, 1f, 1f, 0f));
            Stretch(flash.rectTransform);
            var whiteout = MakeImage(ceremonyRoot, "Whiteout", new Color(1f, 1f, 1f, 0f));
            Stretch(whiteout.rectTransform);
            var revealTitle = MakeText(ceremonyRoot, "RevealTitle", "", 40f, TextAlignmentOptions.Center, Gold);
            SetBand(revealTitle.rectTransform, 0.52f, 0.66f);
            var revealSubtitle = MakeText(ceremonyRoot, "RevealSubtitle", "", 16f, TextAlignmentOptions.Center, Text);
            SetBand(revealSubtitle.rectTransform, 0.42f, 0.52f);
            InsetX(revealSubtitle.rectTransform, 24f);
            var deltaLine = MakeText(ceremonyRoot, "DeltaLine", "", 18f, TextAlignmentOptions.Center, Gold);
            SetBand(deltaLine.rectTransform, 0.32f, 0.40f);

            // Victor portrait: receives the Stage 6 sprite at ascension reveal.
            // Placeholder until Phase D art — seam is clear, swap the sprite asset.
            var victoryPortrait = MakeImage(ceremonyRoot, "VictoryPortrait", Panel);
            SetBand(victoryPortrait.rectTransform, 0.67f, 0.88f);
            InsetX(victoryPortrait.rectTransform, 80f);
            victoryPortrait.gameObject.SetActive(false);

            // Gold-radiance FX overlay: ascension beat committed fallback (swap for
            // bespoke crowned reveal Phase D). Alpha pulsed by TribulationScreen.
            var ascensionFx = MakeImage(victoryPortrait.rectTransform, "AscensionFxOverlay",
                new Color(Gold.r, Gold.g, Gold.b, 0f));
            ascensionFx.gameObject.SetActive(false);

            ceremonyRoot.gameObject.SetActive(false);

            // -- ancestor card --
            var cardRoot = NewStretched(controllerRt, "CardRoot");
            var cardDim = MakeImage(cardRoot, "Dim", new Color(0f, 0f, 0f, 0.75f));
            Stretch(cardDim.rectTransform);
            cardDim.raycastTarget = true;
            var cardFrame = MakeImage(cardRoot, "CardFrame", Gold);
            var cardFrameRt = cardFrame.rectTransform;
            cardFrameRt.anchorMin = new Vector2(0.14f, 0.28f);
            cardFrameRt.anchorMax = new Vector2(0.86f, 0.72f);
            cardFrameRt.offsetMin = Vector2.zero;
            cardFrameRt.offsetMax = Vector2.zero;
            var cardInner = MakeImage(cardFrameRt, "CardInner", Panel);
            Stretch(cardInner.rectTransform);
            cardInner.rectTransform.offsetMin = new Vector2(5f, 5f);
            cardInner.rectTransform.offsetMax = new Vector2(-5f, -5f);
            var cardMotif = MakeImage(cardInner.rectTransform, "CardMotif", PanelLine);
            SetBand(cardMotif.rectTransform, 0.52f, 0.92f);
            InsetX(cardMotif.rectTransform, 30f);
            var cardTitle = MakeText(cardInner.rectTransform, "CardTitle", "", 17f, TextAlignmentOptions.Center, Text);
            SetBand(cardTitle.rectTransform, 0.36f, 0.50f);
            var cardBonus = MakeText(cardInner.rectTransform, "CardBonus", "", 16f, TextAlignmentOptions.Center, Gold);
            SetBand(cardBonus.rectTransform, 0.24f, 0.36f);
            var cardRetire = MakeText(cardInner.rectTransform, "CardRetireLine", "", 12f, TextAlignmentOptions.Center, TextDim);
            SetBand(cardRetire.rectTransform, 0.06f, 0.22f);
            InsetX(cardRetire.rectTransform, 12f);
            cardRoot.gameObject.SetActive(false);

            // -- generation summary --
            var summaryRoot = NewStretched(controllerRt, "SummaryRoot");
            var summaryDim = MakeImage(summaryRoot, "Dim", new Color(0f, 0f, 0f, 0.85f));
            Stretch(summaryDim.rectTransform);
            summaryDim.raycastTarget = true;
            var summaryPanel = MakeImage(summaryRoot, "Panel", Panel);
            var summaryPanelRt = summaryPanel.rectTransform;
            summaryPanelRt.anchorMin = new Vector2(0.08f, 0.22f);
            summaryPanelRt.anchorMax = new Vector2(0.92f, 0.78f);
            summaryPanelRt.offsetMin = Vector2.zero;
            summaryPanelRt.offsetMax = Vector2.zero;
            var summaryTitle = MakeText(summaryPanelRt, "SummaryTitle", "", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(summaryTitle.rectTransform, 0.82f, 0.96f);
            var summaryStats = MakeText(summaryPanelRt, "SummaryStats", "", 15f, TextAlignmentOptions.Center, Text);
            SetBand(summaryStats.rectTransform, 0.48f, 0.80f);
            InsetX(summaryStats.rectTransform, 16f);
            var ratePreview = MakeText(summaryPanelRt, "RatePreview", "", 18f, TextAlignmentOptions.Center, Gold);
            SetBand(ratePreview.rectTransform, 0.30f, 0.44f);
            var continueButton = MakeButton(summaryPanelRt, "ContinueButton", "Continue", Gold, 16f);
            var continueRt = (RectTransform)continueButton.transform;
            continueRt.anchorMin = new Vector2(0.16f, 0.06f);
            continueRt.anchorMax = new Vector2(0.84f, 0.20f);
            continueRt.offsetMin = Vector2.zero;
            continueRt.offsetMax = Vector2.zero;
            summaryRoot.gameObject.SetActive(false);

            // -- final beat --
            var finalRoot = NewStretched(controllerRt, "FinalRoot");
            var finalBg = MakeImage(finalRoot, "FinalBg", new Color(0.04f, 0.05f, 0.08f, 1f));
            Stretch(finalBg.rectTransform);
            finalBg.raycastTarget = true;
            var finalText = MakeText(finalRoot, "FinalText", "", 18f, TextAlignmentOptions.Center, Text);
            SetBand(finalText.rectTransform, 0.44f, 0.56f);
            finalRoot.gameObject.SetActive(false);

            // -- component + wiring --
            var screen = controller.AddComponent<TribulationScreen>();
            Assign(screen, "_config", tribulation);
            Assign(screen, "_confirmRoot", confirmRoot.gameObject);
            Assign(screen, "_chanceToAscendText", chanceText);
            Assign(screen, "_ascendLine", ascendLine);
            Assign(screen, "_fallLine", fallLine);
            Assign(screen, "_oddsPanel", oddsPanel.gameObject);
            Assign(screen, "_oddsToggle", oddsToggle);
            Assign(screen, "_notYetButton", notYet);
            Assign(screen, "_holdButton", holdComponent);
            Assign(screen, "_holdFill", holdFill);
            Assign(screen, "_ceremonyRoot", ceremonyRoot.gameObject);
            Assign(screen, "_flash", flash);
            Assign(screen, "_whiteout", whiteout);
            Assign(screen, "_revealTitle", revealTitle);
            Assign(screen, "_revealSubtitle", revealSubtitle);
            Assign(screen, "_deltaLine", deltaLine);
            Assign(screen, "_ceremonyTapCatcher", tapCatcher);
            Assign(screen, "_victoryPortrait", victoryPortrait);
            Assign(screen, "_ascensionFxOverlay", ascensionFx);
            Assign(screen, "_cardRoot", cardRoot.gameObject);
            Assign(screen, "_cardFrame", cardFrame);
            Assign(screen, "_cardMotif", cardMotif);
            Assign(screen, "_cardTitle", cardTitle);
            Assign(screen, "_cardBonus", cardBonus);
            Assign(screen, "_cardRetireLine", cardRetire);
            Assign(screen, "_summaryRoot", summaryRoot.gameObject);
            Assign(screen, "_summaryTitle", summaryTitle);
            Assign(screen, "_summaryStats", summaryStats);
            Assign(screen, "_ratePreview", ratePreview);
            Assign(screen, "_continueButton", continueButton);
            Assign(screen, "_finalRoot", finalRoot.gameObject);
            Assign(screen, "_finalText", finalText);
            return screen;
        }

        private static ChronicleScreenView BuildChronicleScreenUi(RectTransform root)
        {
            var controller = new GameObject("ChronicleScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var chronicleRoot = NewStretched(controllerRt, "ChronicleRoot");
            var dim = MakeImage(chronicleRoot, "Dim", new Color(0f, 0f, 0f, 0.85f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(chronicleRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.05f, 0.08f);
            panelRt.anchorMax = new Vector2(0.95f, 0.92f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "The Chronicle", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.92f, 0.99f);

            var close = MakeButton(panelRt, "CloseButton", "✕", PanelLine, 14f);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(34f, 34f);
            closeRt.anchoredPosition = new Vector2(-6f, -6f);

            // Scrollable body — viewport clips the content; rows added at runtime.
            var viewport = MakeImage(panelRt, "Viewport", new Color(0f, 0f, 0f, 0.01f));
            var viewportRt = viewport.rectTransform;
            viewportRt.anchorMin = new Vector2(0.03f, 0.04f);
            viewportRt.anchorMax = new Vector2(0.97f, 0.90f);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform));
            var contentRt = (RectTransform)content.transform;
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            chronicleRoot.gameObject.SetActive(false);

            var screen = controller.AddComponent<ChronicleScreenView>();
            Assign(screen, "_root", chronicleRoot.gameObject);
            Assign(screen, "_contentRoot", contentRt);
            Assign(screen, "_closeButton", close);
            return screen;
        }

        private static CouncilScreenView BuildCouncilScreenUi(RectTransform root,
            ChronicleScreenView chronicleScreen)
        {
            var controller = new GameObject("CouncilScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var shrineRoot = NewStretched(controllerRt, "ShrineRoot");
            var dim = MakeImage(shrineRoot, "Dim", new Color(0f, 0f, 0f, 0.8f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(shrineRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.05f, 0.10f);
            panelRt.anchorMax = new Vector2(0.95f, 0.90f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "The Lineage Shrine", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.92f, 0.99f);

            var close = MakeButton(panelRt, "CloseButton", "✕", PanelLine, 14f);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.sizeDelta = new Vector2(34f, 34f);
            closeRt.anchoredPosition = new Vector2(-6f, -6f);

            // Chronicle access button — top-left corner, mirroring the close button top-right.
            var chronicleBtn = MakeButton(panelRt, "ChronicleButton", "Chronicle ›", PanelLine, 12f);
            var chronicleBtnRt = (RectTransform)chronicleBtn.transform;
            chronicleBtnRt.anchorMin = new Vector2(0f, 1f);
            chronicleBtnRt.anchorMax = new Vector2(0f, 1f);
            chronicleBtnRt.pivot = new Vector2(0f, 1f);
            chronicleBtnRt.sizeDelta = new Vector2(100f, 30f);
            chronicleBtnRt.anchoredPosition = new Vector2(6f, -6f);

            var screen = controller.AddComponent<CouncilScreenView>();
            Assign(screen, "_root", shrineRoot.gameObject);
            Assign(screen, "_closeButton", close);
            Assign(screen, "_chronicleButton", chronicleBtn);
            Assign(screen, "_chronicleScreen", chronicleScreen);

            // Five card rows, top to bottom.
            for (int i = 0; i < 5; i++)
            {
                float top = 0.90f - i * 0.135f;
                float bottom = top - 0.115f;
                var row = MakeImage(panelRt, $"Row{i + 1}", PanelLine);
                SetBand(row.rectTransform, bottom, top);
                InsetX(row.rectTransform, 10f);
                var motif = MakeImage(row.rectTransform, "Motif", PathMotifNeutral);
                var motifRt = motif.rectTransform;
                motifRt.anchorMin = new Vector2(0f, 0.5f);
                motifRt.anchorMax = new Vector2(0f, 0.5f);
                motifRt.pivot = new Vector2(0f, 0.5f);
                motifRt.sizeDelta = new Vector2(40f, 40f);
                motifRt.anchoredPosition = new Vector2(10f, 0f);
                var rowTitle = MakeText(row.rectTransform, "Title", "", 14f, TextAlignmentOptions.MidlineLeft, Text);
                Stretch(rowTitle.rectTransform);
                rowTitle.rectTransform.offsetMin = new Vector2(62f, 0f);
                rowTitle.rectTransform.offsetMax = new Vector2(-70f, 0f);
                var remembrance = MakeText(row.rectTransform, "Remembrance", "", 15f, TextAlignmentOptions.MidlineRight, Gold);
                Inset(remembrance.rectTransform, right: 12f);

                AssignPath(screen, $"_rows.Array.data[{i}].root", row.gameObject, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].motif", motif, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].title", rowTitle, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].remembrance", remembrance, ensureRowsSize: 5);
            }

            var foundation = MakeText(panelRt, "FoundationLine", "", 13f, TextAlignmentOptions.Center, TextDim);
            SetBand(foundation.rectTransform, 0.115f, 0.18f);
            var total = MakeText(panelRt, "TotalLine", "", 17f, TextAlignmentOptions.Center, Gold);
            SetBand(total.rectTransform, 0.03f, 0.105f);
            Assign(screen, "_foundationLine", foundation);
            Assign(screen, "_totalLine", total);

            shrineRoot.gameObject.SetActive(false);
            return screen;
        }

        private static SettingsScreenView BuildSettingsUi(RectTransform root)
        {
            var controller = new GameObject("SettingsScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var settingsRoot = NewStretched(controllerRt, "SettingsRoot");
            // ponytail: OpacitySpec.Scrim; token text sizes for Settings panel.
            var dim = MakeImage(settingsRoot, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(settingsRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.08f, 0.16f);
            panelRt.anchorMax = new Vector2(0.92f, 0.86f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "Settings",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.90f, 0.99f);
            var bgmToggle = MakeToggle(panelRt, "BgmToggle", "Music", 0.795f, 0.875f);
            var sfxToggle = MakeToggle(panelRt, "SfxToggle", "Sound effects", 0.705f, 0.785f);
            var hapticsToggle = MakeToggle(panelRt, "HapticsToggle", "Haptics", 0.615f, 0.695f);
            var reduceMotionToggle = MakeToggle(panelRt, "ReduceMotionToggle", "Reduce motion", 0.525f, 0.605f);
            // ponytail: fits the existing gap between Version and Close rather than
            // reflowing every row below Reduce Motion for one more toggle.
            var notificationsToggle = MakeToggle(panelRt, "NotificationsToggle", "Notifications", 0.18f, 0.26f);
            var cloudStatus = MakeText(panelRt, "CloudStatus", "Local save only",
                TypographicScale.Label, TextAlignmentOptions.Center, TextDim);
            SetBand(cloudStatus.rectTransform, 0.45f, 0.51f);
            var about = MakeButton(panelRt, "AboutButton", "About & Glossary", PanelLine, TypographicScale.Body);
            var aboutRt = (RectTransform)about.transform;
            aboutRt.anchorMin = new Vector2(0.12f, 0.34f);
            aboutRt.anchorMax = new Vector2(0.88f, 0.43f);
            aboutRt.offsetMin = Vector2.zero;
            aboutRt.offsetMax = Vector2.zero;
            var version = MakeText(panelRt, "Version", "v0.1.0",
                TypographicScale.Caption, TextAlignmentOptions.Center, TextDim);
            SetBand(version.rectTransform, 0.27f, 0.33f);
            var close = MakeButton(panelRt, "CloseButton", "Close", Gold, TypographicScale.Body);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(0.20f, 0.05f);
            closeRt.anchorMax = new Vector2(0.80f, 0.15f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            settingsRoot.gameObject.SetActive(false);

            AboutScreenView about_ = BuildAboutUi(controllerRt);

            var screen = controller.AddComponent<SettingsScreenView>();
            Assign(screen, "_root", settingsRoot.gameObject);
            Assign(screen, "_bgmToggle", bgmToggle);
            Assign(screen, "_sfxToggle", sfxToggle);
            Assign(screen, "_hapticsToggle", hapticsToggle);
            Assign(screen, "_reduceMotionToggle", reduceMotionToggle);
            Assign(screen, "_notificationsToggle", notificationsToggle);
            Assign(screen, "_cloudStatus", cloudStatus);
            Assign(screen, "_version", version);
            Assign(screen, "_aboutButton", about);
            Assign(screen, "_closeButton", close);
            Assign(screen, "_about", about_);
            return screen;
        }

        private static AboutScreenView BuildAboutUi(RectTransform parent)
        {
            var controller = new GameObject("AboutScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(parent, false);
            Stretch(controllerRt);

            var aboutRoot = NewStretched(controllerRt, "AboutRoot");
            var dim = MakeImage(aboutRoot, "Dim", new Color(0f, 0f, 0f, 0.9f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(aboutRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.06f, 0.10f);
            panelRt.anchorMax = new Vector2(0.94f, 0.90f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var close = MakeButton(panelRt, "CloseButton", "Close", Gold, 15f);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(0.30f, 0.02f);
            closeRt.anchorMax = new Vector2(0.70f, 0.09f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;

            // Scrollable body (glossary is long): ScrollRect → viewport (mask) →
            // content (auto-sized TMP).
            var viewport = MakeImage(panelRt, "Viewport", new Color(0f, 0f, 0f, 0.01f));
            var viewportRt = viewport.rectTransform;
            viewportRt.anchorMin = new Vector2(0.04f, 0.11f);
            viewportRt.anchorMax = new Vector2(0.96f, 0.97f);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform));
            var contentRt = (RectTransform)content.transform;
            contentRt.SetParent(viewportRt, false);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var body = content.AddComponent<TextMeshProUGUI>();
            body.fontSize = 13f;
            body.color = Text;
            body.alignment = TextAlignmentOptions.TopLeft;
            body.text = "About"; // populated at runtime from HeritageContent

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            aboutRoot.gameObject.SetActive(false);

            var screen = controller.AddComponent<AboutScreenView>();
            Assign(screen, "_root", aboutRoot.gameObject);
            Assign(screen, "_body", body);
            Assign(screen, "_closeButton", close);
            return screen;
        }

        private static ContestScreenView BuildContestScreenUi(RectTransform root, ContestConfig contestConfig)
        {
            var controller = new GameObject("ContestScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            // ---- challenge sheet ----
            var challengeRoot = NewStretched(controllerRt, "ChallengeRoot");
            // ponytail: OpacitySpec.Scrim; token text sizes.
            var challengeDim = MakeImage(challengeRoot, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(challengeDim.rectTransform);
            challengeDim.raycastTarget = true;

            var panel = MakeImage(challengeRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.06f, 0.08f);
            panelRt.anchorMax = new Vector2(0.94f, 0.92f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "A rival House approaches",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.90f, 0.99f);

            var houseName = MakeText(panelRt, "HouseNameText", "",
                TypographicScale.H1, TextAlignmentOptions.Center, Text);
            SetBand(houseName.rectTransform, 0.79f, 0.90f);

            var housePath = MakeText(panelRt, "HousePathText", "",
                TypographicScale.BodySm, TextAlignmentOptions.Center, Gold);
            SetBand(housePath.rectTransform, 0.71f, 0.79f);

            var housePower = MakeText(panelRt, "HousePowerText", "",
                TypographicScale.Label, TextAlignmentOptions.Center, TextDim);
            SetBand(housePower.rectTransform, 0.63f, 0.71f);

            // Stance buttons — three bands with their odds label.
            var strikeBtn = MakeButton(panelRt, "StrikeButton", "Strike", PanelLine, 15f);
            var strikeBtnRt = (RectTransform)strikeBtn.transform;
            strikeBtnRt.anchorMin = new Vector2(0.05f, 0.52f);
            strikeBtnRt.anchorMax = new Vector2(0.95f, 0.61f);
            strikeBtnRt.offsetMin = Vector2.zero;
            strikeBtnRt.offsetMax = Vector2.zero;
            var strikeOdds = MakeText(panelRt, "StrikeOddsText", "", 12f, TextAlignmentOptions.MidlineRight, TextDim);
            SetBand(strikeOdds.rectTransform, 0.52f, 0.61f);
            Inset(strikeOdds.rectTransform, right: 12f);

            var endureBtn = MakeButton(panelRt, "EndureButton", "Endure", PanelLine, 15f);
            var endureBtnRt = (RectTransform)endureBtn.transform;
            endureBtnRt.anchorMin = new Vector2(0.05f, 0.41f);
            endureBtnRt.anchorMax = new Vector2(0.95f, 0.50f);
            endureBtnRt.offsetMin = Vector2.zero;
            endureBtnRt.offsetMax = Vector2.zero;
            var endureOdds = MakeText(panelRt, "EndureOddsText", "", 12f, TextAlignmentOptions.MidlineRight, TextDim);
            SetBand(endureOdds.rectTransform, 0.41f, 0.50f);
            Inset(endureOdds.rectTransform, right: 12f);

            var flowBtn = MakeButton(panelRt, "FlowButton", "Flow", PanelLine, 15f);
            var flowBtnRt = (RectTransform)flowBtn.transform;
            flowBtnRt.anchorMin = new Vector2(0.05f, 0.30f);
            flowBtnRt.anchorMax = new Vector2(0.95f, 0.39f);
            flowBtnRt.offsetMin = Vector2.zero;
            flowBtnRt.offsetMax = Vector2.zero;
            var flowOdds = MakeText(panelRt, "FlowOddsText", "", 12f, TextAlignmentOptions.MidlineRight, TextDim);
            SetBand(flowOdds.rectTransform, 0.30f, 0.39f);
            Inset(flowOdds.rectTransform, right: 12f);

            // Hold-to-confirm button.
            var holdBtn = MakeButton(panelRt, "HoldButton", "", Gold, 16f);
            var holdBtnRt = (RectTransform)holdBtn.transform;
            holdBtnRt.anchorMin = new Vector2(0.08f, 0.16f);
            holdBtnRt.anchorMax = new Vector2(0.92f, 0.26f);
            holdBtnRt.offsetMin = Vector2.zero;
            holdBtnRt.offsetMax = Vector2.zero;
            var holdComponent = holdBtn.gameObject.AddComponent<HoldButton>();
            var holdBack = holdBtn.GetComponent<Image>();
            holdBack.color = PanelLine;
            var holdFill = MakeImage(holdBtnRt, "HoldFill", Gold);
            Stretch(holdFill.rectTransform);
            holdFill.type = Image.Type.Filled;
            holdFill.fillMethod = Image.FillMethod.Horizontal;
            holdFill.fillAmount = 0f;
            holdFill.sprite = WhiteSprite();
            var holdLabel = MakeText(holdBtnRt, "HoldLabel", "Hold to enter the clash", 14f,
                TextAlignmentOptions.Center, Text);
            Stretch(holdLabel.rectTransform);

            // Decline button — "Not now".
            var declineBtn = MakeButton(panelRt, "DeclineButton", "Not now", Panel, 13f);
            var declineBtnRt = (RectTransform)declineBtn.transform;
            declineBtnRt.anchorMin = new Vector2(0.30f, 0.04f);
            declineBtnRt.anchorMax = new Vector2(0.70f, 0.11f);
            declineBtnRt.offsetMin = Vector2.zero;
            declineBtnRt.offsetMax = Vector2.zero;

            challengeRoot.gameObject.SetActive(false);

            // ---- reveal beat ----
            var revealRoot = NewStretched(controllerRt, "RevealRoot");
            var revealBg = MakeImage(revealRoot, "RevealBg", new Color(0.04f, 0.05f, 0.08f, 1f));
            Stretch(revealBg.rectTransform);
            revealBg.raycastTarget = true;

            var revealTitle = MakeText(revealRoot, "RevealTitle", "", 42f, TextAlignmentOptions.Center, Gold);
            SetBand(revealTitle.rectTransform, 0.52f, 0.66f);

            var renownDelta = MakeText(revealRoot, "RenownDeltaText", "", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(renownDelta.rectTransform, 0.40f, 0.50f);
            revealRoot.gameObject.SetActive(false);

            // ---- summary ----
            var summaryRoot = NewStretched(controllerRt, "SummaryRoot");
            var summaryDim = MakeImage(summaryRoot, "Dim", new Color(0f, 0f, 0f, 0.85f));
            Stretch(summaryDim.rectTransform);
            summaryDim.raycastTarget = true;
            var summaryPanel = MakeImage(summaryRoot, "SummaryPanel", Panel);
            var summaryPanelRt = summaryPanel.rectTransform;
            summaryPanelRt.anchorMin = new Vector2(0.08f, 0.26f);
            summaryPanelRt.anchorMax = new Vector2(0.92f, 0.74f);
            summaryPanelRt.offsetMin = Vector2.zero;
            summaryPanelRt.offsetMax = Vector2.zero;

            var standing = MakeText(summaryPanelRt, "StandingText", "", 16f, TextAlignmentOptions.Center, Text);
            SetBand(standing.rectTransform, 0.50f, 0.88f);
            InsetX(standing.rectTransform, 16f);
            standing.enableWordWrapping = true;

            var continueBtn = MakeButton(summaryPanelRt, "ContinueButton", "Continue", Gold, 16f);
            var continueBtnRt = (RectTransform)continueBtn.transform;
            continueBtnRt.anchorMin = new Vector2(0.16f, 0.08f);
            continueBtnRt.anchorMax = new Vector2(0.84f, 0.22f);
            continueBtnRt.offsetMin = Vector2.zero;
            continueBtnRt.offsetMax = Vector2.zero;
            summaryRoot.gameObject.SetActive(false);

            // ---- component + wiring ----
            var screen = controller.AddComponent<ContestScreenView>();
            Assign(screen, "_config", contestConfig);
            Assign(screen, "_challengeRoot", challengeRoot.gameObject);
            Assign(screen, "_houseNameText", houseName);
            Assign(screen, "_housePathText", housePath);
            Assign(screen, "_housePowerText", housePower);
            Assign(screen, "_strikeOddsText", strikeOdds);
            Assign(screen, "_endureOddsText", endureOdds);
            Assign(screen, "_flowOddsText", flowOdds);
            Assign(screen, "_strikeButton", strikeBtn);
            Assign(screen, "_endureButton", endureBtn);
            Assign(screen, "_flowButton", flowBtn);
            Assign(screen, "_holdButton", holdComponent);
            Assign(screen, "_holdFill", holdFill);
            Assign(screen, "_declineButton", declineBtn);
            Assign(screen, "_revealRoot", revealRoot.gameObject);
            Assign(screen, "_revealTitle", revealTitle);
            Assign(screen, "_renownDeltaText", renownDelta);
            Assign(screen, "_summaryRoot", summaryRoot.gameObject);
            Assign(screen, "_standingText", standing);
            Assign(screen, "_continueButton", continueBtn);
            return screen;
        }

        private static OjaScreenView BuildOjaScreenUi(RectTransform root)
        {
            var controller = new GameObject("OjaScreen", typeof(RectTransform));
            var controllerRt = (RectTransform)controller.transform;
            controllerRt.SetParent(root, false);
            Stretch(controllerRt);

            var ojaRoot = NewStretched(controllerRt, "OjaRoot");
            // ponytail: OpacitySpec.Scrim; token text sizes.
            var dim = MakeImage(ojaRoot, "Dim", new Color(0f, 0f, 0f, OpacitySpec.Scrim));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(ojaRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.08f, 0.16f);
            panelRt.anchorMax = new Vector2(0.92f, 0.86f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "Ọjà — The Marketplace",
                TypographicScale.H1, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.90f, 0.99f);

            var rankNumber = MakeText(panelRt, "RankNumberText", "—",
                TypographicScale.Hero, TextAlignmentOptions.Center, Text);
            SetBand(rankNumber.rectTransform, 0.68f, 0.88f);

            var rankOrdinal = MakeText(panelRt, "RankOrdinalText", "",
                TypographicScale.Body, TextAlignmentOptions.Center, Text);
            SetBand(rankOrdinal.rectTransform, 0.57f, 0.67f);

            var renownValue = MakeText(panelRt, "RenownValueText", "",
                TypographicScale.Body, TextAlignmentOptions.Center, TextDim);
            SetBand(renownValue.rectTransform, 0.47f, 0.57f);

            var progressBg = MakeImage(panelRt, "ProgressBg", PanelLine);
            SetBand(progressBg.rectTransform, 0.38f, 0.44f);
            InsetX(progressBg.rectTransform, 24f);

            var progressFill = MakeImage(progressBg.rectTransform, "ProgressFill", Gold);
            Stretch(progressFill.rectTransform);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;
            progressFill.sprite = WhiteSprite();

            var nextRankLine = MakeText(panelRt, "NextRankLine", "",
                TypographicScale.Label, TextAlignmentOptions.Center, TextDim);
            SetBand(nextRankLine.rectTransform, 0.28f, 0.37f);

            var close = MakeButton(panelRt, "CloseButton", "Close", Gold, TypographicScale.Body);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(0.20f, 0.05f);
            closeRt.anchorMax = new Vector2(0.80f, 0.15f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            ojaRoot.gameObject.SetActive(false);

            var screen = controller.AddComponent<OjaScreenView>();
            Assign(screen, "_root", ojaRoot.gameObject);
            Assign(screen, "_rankNumberText", rankNumber);
            Assign(screen, "_rankOrdinalText", rankOrdinal);
            Assign(screen, "_renownValueText", renownValue);
            Assign(screen, "_nextRankLine", nextRankLine);
            Assign(screen, "_rankProgressBar", progressFill);
            Assign(screen, "_closeButton", close);
            return screen;
        }

        private static Toggle MakeToggle(RectTransform parent, string name, string label,
            float bottomFrac, float topFrac)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            SetBand(rt, bottomFrac, topFrac);

            var box = MakeImage(rt, "Box", PanelLine);
            var boxRt = box.rectTransform;
            boxRt.anchorMin = new Vector2(0f, 0.5f);
            boxRt.anchorMax = new Vector2(0f, 0.5f);
            boxRt.pivot = new Vector2(0f, 0.5f);
            boxRt.sizeDelta = new Vector2(34f, 34f);
            boxRt.anchoredPosition = new Vector2(16f, 0f);
            box.raycastTarget = true;
            var check = MakeImage(boxRt, "Check", Gold);
            Stretch(check.rectTransform);
            check.rectTransform.offsetMin = new Vector2(6f, 6f);
            check.rectTransform.offsetMax = new Vector2(-6f, -6f);

            // ponytail: Body size for toggle labels.
            var lbl = MakeText(rt, "Label", label, TypographicScale.Body, TextAlignmentOptions.MidlineLeft, Text);
            Stretch(lbl.rectTransform);
            lbl.rectTransform.offsetMin = new Vector2(60f, 0f);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            return toggle;
        }

        private static readonly Color PathMotifNeutral = new Color(0.165f, 0.192f, 0.251f);

        private static RectTransform NewStretched(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        // ---- wiring helpers ----

        private static void AssignConfig(Component component, GameplayConfig config) =>
            Assign(component, "_config", config);

        private static void Assign(Component component, string field, Object value)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                throw new System.InvalidOperationException(
                    $"{component.GetType().Name} has no serialized field '{field}'");
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns into nested serialized paths (struct arrays like
        /// CouncilScreenView.CardRow), growing the array first if needed.</summary>
        private static void AssignPath(Component component, string propertyPath, Object value, int ensureRowsSize)
        {
            var so = new SerializedObject(component);
            var rows = so.FindProperty("_rows");
            if (rows != null && rows.arraySize < ensureRowsSize) rows.arraySize = ensureRowsSize;
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                throw new System.InvalidOperationException(
                    $"{component.GetType().Name} has no serialized path '{propertyPath}'");
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray(Component component, string field, Object[] values)
        {
            var so = new SerializedObject(component);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                throw new System.InvalidOperationException(
                    $"{component.GetType().Name} has no serialized array '{field}'");
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- layout helpers ----

        private static RectTransform Zone(RectTransform parent, string name, float topFrac, float bottomFrac)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f - bottomFrac);
            rt.anchorMax = new Vector2(1f, 1f - topFrac);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void SetBand(RectTransform rt, float bottomFrac, float topFrac)
        {
            rt.anchorMin = new Vector2(0f, bottomFrac);
            rt.anchorMax = new Vector2(1f, topFrac);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Inset(RectTransform rt, float left = 0f, float right = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, 0f);
            rt.offsetMax = new Vector2(-right, 0f);
        }

        private static void InsetX(RectTransform rt, float inset)
        {
            rt.offsetMin = new Vector2(inset, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-inset, rt.offsetMax.y);
        }

        private static TMP_Text MakeText(RectTransform parent, string name, string text,
            float size, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image MakeImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button MakeButton(RectTransform parent, string name, string label,
            Color background, float labelSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = background;
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var text = MakeText(rt, "Label", label, labelSize, TextAlignmentOptions.Center, Bg);
            text.raycastTarget = false;
            return button;
        }

        private static Sprite WhiteSprite()
        {
            // Image.Type.Filled needs a sprite; Unity's built-in UI sprite suffices.
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        // ponytail: Hex() no longer used for palette consts (replaced by Palette.* aliases above).
        // Kept for any future one-off colour needs; removing it would be churn with no gain.
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
