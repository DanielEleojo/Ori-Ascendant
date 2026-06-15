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
        private const string CrossroadsDeckPath = ConfigFolder + "/CrossroadsDeck.asset";
        private const string StageFolder = "Assets/Resources/StageConfigs";
        private const string PathFolder = "Assets/Resources/PathConfigs";
        private const string OriFolder = "Assets/Resources/OriConfigs";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpEssentialsPackage =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // Palette (placeholder art direction: night-sky neutrals + àṣẹ gold).
        private static readonly Color Bg = Hex("#0E1116");
        private static readonly Color Panel = Hex("#1A1F29");
        private static readonly Color PanelLine = Hex("#2A3140");
        private static readonly Color Gold = Hex("#D9A441");
        private static readonly Color Text = Hex("#ECE6D8");
        private static readonly Color TextDim = Hex("#9AA3B2");

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

        // ---- Ori (virtue-vow) seed set — PLACEHOLDER content, pre-§7.10 review (slice #10).
        //      Universal virtues only (no culture-specific claims) until the real deck is authored. ----
        private struct OriRow { public string File, Name, Description; }

        private static readonly OriRow[] Oris =
        {
            new OriRow { File = "Mercy",    Name = "The Path of Mercy",    Description = "To spare, to forgive, to lift the fallen — held even when the world counsels otherwise." },
            new OriRow { File = "Resolve",  Name = "The Path of Resolve",  Description = "To finish what is begun; to bend without breaking; to outlast the storm." },
            new OriRow { File = "Cunning",  Name = "The Path of Cunning",  Description = "To win by wit and angle where strength alone would fail." },
            new OriRow { File = "Devotion", Name = "The Path of Devotion", Description = "To give oneself wholly — to kin, to vow, to the line that comes after." },
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
            CultivationStageConfig[] stages = BuildStageConfigs();
            PathConfig[] paths = BuildPathConfigs();
            BuildMainScene(gameplay, tribulation, councilConfig, stages, paths);
            BuildConfigurator.Apply();
            Debug.Log("SceneBuilder: scene + 12 config assets built successfully.");
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
            config.welcomeBackMinSeconds = 60;
            config.autosaveIntervalSeconds = 30;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static TribulationConfig BuildTribulationConfig()
        {
            var config = EnsureAsset<TribulationConfig>(TribulationConfigPath);
            config.baseAscendChance = 0.60;
            config.aseThresholdMantissa = 25.0;
            config.aseThresholdExponent = 6;
            config.ambientFractions = new[] { 0.5f, 0.8f, 1.0f };
            config.holdToConfirmSeconds = 0.8;
            // Ceremony beats — GAMEPLAY §3.5 timing table.
            config.transitionSeconds = 2.0f;
            config.stormWaveCount = 3;
            config.stormWaveIntervalSeconds = 1.0f;
            config.silenceHoldSeconds = 1.5f;
            config.revealSeconds = 2.5f;
            config.ancestorCardSeconds = 2.5f;
            config.finalBeatSeconds = 2.0f;
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

        private static OriConfig[] BuildOriConfigs()
        {
            var configs = new OriConfig[Oris.Length];
            for (int i = 0; i < Oris.Length; i++)
            {
                OriRow row = Oris[i];
                var config = EnsureAsset<OriConfig>($"{OriFolder}/{row.File}.asset");
                config.oriName = row.Name;
                config.oriDescription = row.Description;
                EditorUtility.SetDirty(config);
                configs[i] = config;
            }
            return configs;
        }

        // ---- Crossroads seed deck — PLACEHOLDER content, pre-§7.10 review (slice #10).
        //      Universal moral dilemmas only (no culture-specific claims) until the real
        //      deck is authored. Every beat offers one option per Ori so the life's vow
        //      is always on the table — temptation, not a trap. ----
        private static CrossroadsBeat Beat(string id, string prompt,
            string mercy, string resolve, string cunning, string devotion) =>
            new CrossroadsBeat
            {
                id = id,
                prompt = prompt,
                options = new[]
                {
                    new CrossroadsOption { oriIndex = 0, text = mercy },    // 0 = Mercy
                    new CrossroadsOption { oriIndex = 1, text = resolve },  // 1 = Resolve
                    new CrossroadsOption { oriIndex = 2, text = cunning },  // 2 = Cunning
                    new CrossroadsOption { oriIndex = 3, text = devotion }, // 3 = Devotion
                },
            };

        private static CrossroadsDeckConfig BuildCrossroadsDeck()
        {
            var deck = EnsureAsset<CrossroadsDeckConfig>(CrossroadsDeckPath);
            deck.beats = new[]
            {
                Beat("ford",
                    "A swollen river bars the road, and a stranger clings to a branch midstream, calling out.",
                    "Wade in and pull them free, whatever the current costs you.",
                    "Brace on the bank and hold a line out to them until the water tires.",
                    "Read the eddies and talk them to a footing only you can see.",
                    "Call your kin to make a chain — no one crosses this water alone."),
                Beat("market",
                    "In the crowded market a hungry child is caught taking bread, and the seller raises a hand.",
                    "Pay for the loaf and let the child go.",
                    "Step between the blow and the child, and do not move.",
                    "Turn the seller's anger into a bargain that feeds them both.",
                    "Take the child home and answer for them as your own."),
                Beat("rival",
                    "A rival who once wronged you stumbles on the climb and asks you, quietly, for help.",
                    "Lift them up; the old wound need not be theirs to carry.",
                    "Help them stand, then hold them to finishing what they began.",
                    "Trade your help for what only they can teach you in return.",
                    "Help for the sake of the line you both serve, not for them."),
                Beat("inheritance",
                    "An elder dies and leaves to you alone a gift that was meant for the whole village.",
                    "Give it to those who need it more than you do.",
                    "Keep it, and prove by your climb that the trust was earned.",
                    "Place it where it quietly grows into far more for everyone.",
                    "Lay it before your ancestors, in the name of the line."),
                Beat("oath",
                    "An oath you swore at dawn would, by dusk, cost an innocent dearly to keep.",
                    "Break the oath; mercy outweighs your pride.",
                    "Keep the oath — your word, once given, is iron.",
                    "Find the third path that honours the oath and spares the innocent.",
                    "Keep faith with those it was sworn to, and bear the cost yourself."),
            };
            EditorUtility.SetDirty(deck);
            return deck;
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
            CouncilConfig councilConfig, CultivationStageConfig[] stages, PathConfig[] paths)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();
            BuildSystems(gameplay, tribulation, councilConfig, stages, paths);
            BuildUi(gameplay, tribulation);

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
            CouncilConfig councilConfig, CultivationStageConfig[] stages, PathConfig[] paths)
        {
            var go = new GameObject("Systems");
            AssignConfig(go.AddComponent<SaveManager>(), gameplay);
            AssignConfig(go.AddComponent<AseGenerationSystem>(), gameplay);

            var cultivation = go.AddComponent<CultivationSystem>();
            AssignArray(cultivation, "_stages", stages);
            AssignArray(cultivation, "_paths", paths);
            AssignArray(cultivation, "_oris", BuildOriConfigs()); // virtue-vow set (Àkùnlẹ̀yàn); placeholder content pre-§7.10
            Assign(cultivation, "_tribulationConfig", tribulation);

            var crossroads = go.AddComponent<CrossroadsSystem>();
            Assign(crossroads, "_deck", BuildCrossroadsDeck()); // climb-tied dilemmas; placeholder deck pre-§7.10

            var council = go.AddComponent<AncestralCouncilSystem>();
            Assign(council, "_config", councilConfig);

            var tribulationSystem = go.AddComponent<TribulationSystem>();
            Assign(tribulationSystem, "_config", tribulation);
            Assign(tribulationSystem, "_gameplayConfig", gameplay);

            go.AddComponent<OriAscendant.Save.CloudSaveManager>();
            go.AddComponent<OriAscendant.Audio.AudioManager>();

            AssignConfig(go.AddComponent<GameManager>(), gameplay);
        }

        private static void BuildUi(GameplayConfig config, TribulationConfig tribulation)
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

            // Zone 2 — counter on its own nested canvas (1Hz rebuild isolation).
            var counterZone = Zone(root, "CounterZone", 0.10f, 0.20f);
            var counterCanvasGo = new GameObject("CounterCanvas", typeof(RectTransform));
            var counterCanvasRt = (RectTransform)counterCanvasGo.transform;
            counterCanvasRt.SetParent(counterZone, false);
            Stretch(counterCanvasRt);
            counterCanvasGo.AddComponent<Canvas>();
            var aseCounter = MakeText(counterCanvasRt, "AseCounterText", "0", 52f, TextAlignmentOptions.Bottom, Text);
            SetBand(aseCounter.rectTransform, 0.30f, 1.00f);
            var rateText = MakeText(counterCanvasRt, "RateText", "+0 Àṣẹ/s", 17f, TextAlignmentOptions.Top, Gold);
            SetBand(rateText.rectTransform, 0.00f, 0.30f);

            // Zone 3 — identity line + path badge.
            var identity = Zone(root, "IdentityZone", 0.20f, 0.24f);
            var stageText = MakeText(identity, "StageText", "Stage 1", 20f, TextAlignmentOptions.Center, Text);
            Stretch(stageText.rectTransform);
            var pathBadge = MakeText(identity, "PathBadge", "", 14f, TextAlignmentOptions.MidlineRight, Gold);
            Inset(pathBadge.rectTransform, right: 20f);
            pathBadge.gameObject.SetActive(false); // shown once a path is chosen
            var oriBadge = MakeText(identity, "OriBadge", "", 14f, TextAlignmentOptions.MidlineLeft, Gold);
            Inset(oriBadge.rectTransform, left: 20f);
            oriBadge.gameObject.SetActive(false); // shown once an Ori is vowed

            // Zone 4 — portrait = the channel-tap target (GAMEPLAY §5.3).
            var portraitZone = Zone(root, "PortraitZone", 0.24f, 0.58f);
            var portrait = MakeImage(portraitZone, "PortraitImage", Panel);
            var portraitRt = portrait.rectTransform;
            portraitRt.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRt.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRt.sizeDelta = new Vector2(240f, 240f);
            portrait.raycastTarget = true;
            var portraitButton = portrait.gameObject.AddComponent<Button>();
            portraitButton.targetGraphic = portrait;
            portraitButton.transition = Selectable.Transition.ColorTint;

            // One-time channel hint (seenFlags bit 0).
            var hint = MakeText(portraitZone, "ChannelHint", "Touch your cultivator to channel àṣẹ", 14f,
                TextAlignmentOptions.Center, Gold);
            SetBand(hint.rectTransform, 0.00f, 0.12f);
            hint.gameObject.SetActive(false);

            // Steadfastness readout — "Held N of M" crossroads kept true to the vow.
            // Top of the portrait zone; hidden until the first crossroads (Trials > 0).
            var steadfastness = MakeText(portraitZone, "SteadfastnessLine", "", 13f,
                TextAlignmentOptions.Center, Gold);
            SetBand(steadfastness.rectTransform, 0.88f, 1.00f);
            steadfastness.gameObject.SetActive(false);

            // Zone 5 — progress bar (ACTIVE from Phase B).
            var progressZone = Zone(root, "ProgressZone", 0.58f, 0.64f);
            var barBg = MakeImage(progressZone, "BarBackground", PanelLine);
            var barBgRt = barBg.rectTransform;
            Stretch(barBgRt);
            barBgRt.offsetMin = new Vector2(24f, 12f);
            barBgRt.offsetMax = new Vector2(-24f, -12f);
            var barFill = MakeImage(barBgRt, "BarFill", Gold);
            Stretch(barFill.rectTransform);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillAmount = 0f;
            barFill.sprite = WhiteSprite(); // Filled type requires a sprite
            var progressLabel = MakeText(barBgRt, "ProgressLabel", "Next: —", 13f, TextAlignmentOptions.Center, Text);
            Stretch(progressLabel.rectTransform);

            // Zone 6 — primary CTA (ACTIVE from Phase B; tribulation flow Phase C).
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
            var pathDim = MakeImage(pathRootRt, "Dim", new Color(0f, 0f, 0f, 0.75f));
            Stretch(pathDim.rectTransform);
            pathDim.raycastTarget = true;

            var pathPanel = MakeImage(pathRootRt, "Panel", Panel);
            var pathPanelRt = pathPanel.rectTransform;
            pathPanelRt.anchorMin = new Vector2(0.05f, 0.10f);
            pathPanelRt.anchorMax = new Vector2(0.95f, 0.90f);
            pathPanelRt.offsetMin = Vector2.zero;
            pathPanelRt.offsetMax = Vector2.zero;

            var pathTitle = MakeText(pathPanelRt, "Title", "The initiate must choose a Path", 20f,
                TextAlignmentOptions.Center, Gold);
            SetBand(pathTitle.rectTransform, 0.91f, 0.99f);

            var cards = new PathCardView[3];
            float[][] cardBands = { new[] { 0.66f, 0.89f }, new[] { 0.41f, 0.64f }, new[] { 0.16f, 0.39f } };
            for (int i = 0; i < 3; i++)
            {
                cards[i] = BuildPathCard(pathPanelRt, $"PathCard{i}", cardBands[i][0], cardBands[i][1]);
            }

            var confirm = MakeButton(pathPanelRt, "ConfirmButton", "Choose a Path", Gold, 18f);
            var confirmRt = (RectTransform)confirm.transform;
            confirmRt.anchorMin = new Vector2(0.10f, 0.03f);
            confirmRt.anchorMax = new Vector2(0.90f, 0.13f);
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

            // ---- Ori vow gate (Àkùnlẹ̀yàn, the birth of a life) ----
            var oriController = new GameObject("OriScreen", typeof(RectTransform));
            var oriControllerRt = (RectTransform)oriController.transform;
            oriControllerRt.SetParent(root, false);
            Stretch(oriControllerRt);

            var oriRoot = new GameObject("OriRoot", typeof(RectTransform));
            var oriRootRt = (RectTransform)oriRoot.transform;
            oriRootRt.SetParent(oriControllerRt, false);
            Stretch(oriRootRt);
            var oriDim = MakeImage(oriRootRt, "Dim", new Color(0f, 0f, 0f, 0.75f));
            Stretch(oriDim.rectTransform);
            oriDim.raycastTarget = true;

            var oriPanel = MakeImage(oriRootRt, "Panel", Panel);
            var oriPanelRt = oriPanel.rectTransform;
            oriPanelRt.anchorMin = new Vector2(0.05f, 0.10f);
            oriPanelRt.anchorMax = new Vector2(0.95f, 0.90f);
            oriPanelRt.offsetMin = Vector2.zero;
            oriPanelRt.offsetMax = Vector2.zero;

            var oriTitle = MakeText(oriPanelRt, "Title", "Kneel, and choose your Ori", 20f,
                TextAlignmentOptions.Center, Gold);
            SetBand(oriTitle.rectTransform, 0.91f, 0.99f);

            var oriCards = new OriCardView[4];
            float[][] oriBands =
            {
                new[] { 0.72f, 0.90f }, new[] { 0.52f, 0.70f },
                new[] { 0.32f, 0.50f }, new[] { 0.12f, 0.30f },
            };
            for (int i = 0; i < 4; i++)
            {
                oriCards[i] = BuildOriCard(oriPanelRt, $"OriCard{i}", oriBands[i][0], oriBands[i][1]);
            }

            var oriConfirm = MakeButton(oriPanelRt, "ConfirmButton", "Choose your Ori", Gold, 18f);
            var oriConfirmRt = (RectTransform)oriConfirm.transform;
            oriConfirmRt.anchorMin = new Vector2(0.10f, 0.02f);
            oriConfirmRt.anchorMax = new Vector2(0.90f, 0.10f);
            oriConfirmRt.offsetMin = Vector2.zero;
            oriConfirmRt.offsetMax = Vector2.zero;
            oriConfirm.interactable = false;
            var oriConfirmLabel = oriConfirm.GetComponentInChildren<TMP_Text>();
            oriRoot.SetActive(false);

            var oriScreen = oriController.AddComponent<OriScreenView>();
            Assign(oriScreen, "_root", oriRoot);
            AssignArray(oriScreen, "_cards", oriCards);
            Assign(oriScreen, "_confirmButton", oriConfirm);
            Assign(oriScreen, "_confirmLabel", oriConfirmLabel);

            // ---- Crossroads modal (DYNASTY_REDESIGN slice 2a: blocking, one-tap dilemma) ----
            var crossController = new GameObject("CrossroadsScreen", typeof(RectTransform));
            var crossControllerRt = (RectTransform)crossController.transform;
            crossControllerRt.SetParent(root, false);
            Stretch(crossControllerRt);

            var crossRoot = new GameObject("CrossroadsRoot", typeof(RectTransform));
            var crossRootRt = (RectTransform)crossRoot.transform;
            crossRootRt.SetParent(crossControllerRt, false);
            Stretch(crossRootRt);
            var crossDim = MakeImage(crossRootRt, "Dim", new Color(0f, 0f, 0f, 0.75f));
            Stretch(crossDim.rectTransform);
            crossDim.raycastTarget = true;

            var crossPanel = MakeImage(crossRootRt, "Panel", Panel);
            var crossPanelRt = crossPanel.rectTransform;
            crossPanelRt.anchorMin = new Vector2(0.05f, 0.10f);
            crossPanelRt.anchorMax = new Vector2(0.95f, 0.90f);
            crossPanelRt.offsetMin = Vector2.zero;
            crossPanelRt.offsetMax = Vector2.zero;

            var crossPrompt = MakeText(crossPanelRt, "Prompt", "", 18f, TextAlignmentOptions.Center, Text);
            SetBand(crossPrompt.rectTransform, 0.74f, 0.98f);
            InsetX(crossPrompt.rectTransform, 18f);
            crossPrompt.enableWordWrapping = true;

            var crossOptions = new Button[4];
            var crossLabels = new TMP_Text[4];
            float[][] crossBands =
            {
                new[] { 0.57f, 0.70f }, new[] { 0.42f, 0.55f },
                new[] { 0.27f, 0.40f }, new[] { 0.12f, 0.25f },
            };
            for (int i = 0; i < 4; i++)
            {
                crossOptions[i] = BuildCrossroadsOption(crossPanelRt, $"Option{i}", crossBands[i][0], crossBands[i][1]);
                crossLabels[i] = crossOptions[i].GetComponentInChildren<TMP_Text>();
            }
            crossRoot.SetActive(false);

            var crossroadsModal = crossController.AddComponent<CrossroadsModalView>();
            Assign(crossroadsModal, "_root", crossRoot);
            Assign(crossroadsModal, "_promptText", crossPrompt);
            AssignArray(crossroadsModal, "_optionButtons", crossOptions);
            AssignArray(crossroadsModal, "_optionLabels", crossLabels);

            // ---- Phase C screens ----
            TribulationScreen tribulationScreen = BuildTribulationScreenUi(root, tribulation);
            CouncilScreenView councilScreen = BuildCouncilScreenUi(root);
            SettingsScreenView settingsScreen = BuildSettingsUi(root); // Phase D

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
            var dim = MakeImage(modalRootRt, "Dim", new Color(0f, 0f, 0f, 0.65f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var card = MakeImage(modalRootRt, "Card", Panel);
            var cardRt = card.rectTransform;
            cardRt.anchorMin = new Vector2(0.08f, 0.30f);
            cardRt.anchorMax = new Vector2(0.92f, 0.70f);
            cardRt.offsetMin = Vector2.zero;
            cardRt.offsetMax = Vector2.zero;

            var wbHeader = MakeText(cardRt, "HeaderText", "Your Orí kept watch", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(wbHeader.rectTransform, 0.80f, 0.97f);
            var timeAway = MakeText(cardRt, "TimeAwayText", "Away —", 15f, TextAlignmentOptions.Center, TextDim);
            SetBand(timeAway.rectTransform, 0.66f, 0.80f);
            var earned = MakeText(cardRt, "EarnedText", "+0 Àṣẹ", 32f, TextAlignmentOptions.Center, Text);
            SetBand(earned.rectTransform, 0.44f, 0.66f);
            var bonusLine = MakeText(cardRt, "BonusLineText", "", 14f, TextAlignmentOptions.Center, Gold);
            SetBand(bonusLine.rectTransform, 0.33f, 0.44f);
            bonusLine.gameObject.SetActive(false);
            var rateContext = MakeText(cardRt, "RateContextText", "at 0 Àṣẹ/s", 14f, TextAlignmentOptions.Center, TextDim);
            SetBand(rateContext.rectTransform, 0.22f, 0.33f);
            var collect = MakeButton(cardRt, "CollectButton", "Collect", Gold, 18f);
            var collectRt = (RectTransform)collect.transform;
            collectRt.anchorMin = new Vector2(0.10f, 0.04f);
            collectRt.anchorMax = new Vector2(0.90f, 0.19f);
            collectRt.offsetMin = Vector2.zero;
            collectRt.offsetMax = Vector2.zero;
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
            Assign(view, "_steadfastnessText", steadfastness);

            var controller = canvasGo.AddComponent<MainScreenController>();
            Assign(controller, "_config", config);
            Assign(controller, "_tribulationConfig", tribulation);
            Assign(controller, "_stormVignette", vignette);
            Assign(controller, "_tribulationScreen", tribulationScreen);
            Assign(controller, "_progressRoot", progressZone.gameObject);
            Assign(controller, "_barFill", barFill);
            Assign(controller, "_progressLabel", progressLabel);
            Assign(controller, "_ctaRoot", ctaZone.gameObject);
            Assign(controller, "_advanceButton", advance);
            Assign(controller, "_advanceLabel", advanceLabel);
            Assign(controller, "_portraitButton", portraitButton);
            Assign(controller, "_floatingTextAnchor", portraitZone);
            Assign(controller, "_hintRoot", hint.gameObject);
            Assign(controller, "_pathScreen", pathScreen);
            Assign(controller, "_oriScreen", oriScreen);
            Assign(controller, "_crossroadsModal", crossroadsModal);
            Assign(controller, "_settingsButton", settingsBtn);
            Assign(controller, "_settingsScreen", settingsScreen);

            var modal = modalController.AddComponent<WelcomeBackModal>();
            Assign(modal, "_config", config);
            Assign(modal, "_modalRoot", modalRoot);
            Assign(modal, "_timeAwayText", timeAway);
            Assign(modal, "_earnedText", earned);
            Assign(modal, "_rateContextText", rateContext);
            Assign(modal, "_bonusLineText", bonusLine);
            Assign(modal, "_collectButton", collect);

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

            var nameText = MakeText(inner, "Name", "", 17f, TextAlignmentOptions.TopLeft, Text);
            SetBand(nameText.rectTransform, 0.62f, 0.95f);
            InsetX(nameText.rectTransform, 14f);
            var tradition = MakeText(inner, "Tradition", "", 11f, TextAlignmentOptions.TopLeft, TextDim);
            SetBand(tradition.rectTransform, 0.44f, 0.62f);
            InsetX(tradition.rectTransform, 14f);
            var identityText = MakeText(inner, "Identity", "", 13f, TextAlignmentOptions.TopLeft, Gold);
            SetBand(identityText.rectTransform, 0.06f, 0.44f);
            InsetX(identityText.rectTransform, 14f);
            identityText.enableWordWrapping = true;

            var view = cardBg.gameObject.AddComponent<PathCardView>();
            Assign(view, "_button", button);
            Assign(view, "_background", cardBg);
            Assign(view, "_nameText", nameText);
            Assign(view, "_traditionText", tradition);
            Assign(view, "_identityText", identityText);
            return view;
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

            var nameText = MakeText(inner, "Name", "", 17f, TextAlignmentOptions.TopLeft, Text);
            SetBand(nameText.rectTransform, 0.55f, 0.95f);
            InsetX(nameText.rectTransform, 14f);
            var descText = MakeText(inner, "Description", "", 12f, TextAlignmentOptions.TopLeft, TextDim);
            SetBand(descText.rectTransform, 0.06f, 0.55f);
            InsetX(descText.rectTransform, 14f);
            descText.enableWordWrapping = true;

            var view = cardBg.gameObject.AddComponent<OriCardView>();
            Assign(view, "_button", button);
            Assign(view, "_background", cardBg);
            Assign(view, "_nameText", nameText);
            Assign(view, "_descriptionText", descText);
            return view;
        }

        /// <summary>One Crossroads option — a full-width card-button with a wrapping
        /// label (one-tap commit, no selection state). The modal binds text + click
        /// per beat; the caller reads the label via GetComponentInChildren.</summary>
        private static Button BuildCrossroadsOption(RectTransform parent, string name, float bottomFrac, float topFrac)
        {
            var cardBg = MakeImage(parent, name, Panel);
            SetBand(cardBg.rectTransform, bottomFrac, topFrac);
            var inner = cardBg.rectTransform;
            inner.offsetMin = new Vector2(12f, 0f);
            inner.offsetMax = new Vector2(-12f, 0f);
            cardBg.raycastTarget = true;

            var button = cardBg.gameObject.AddComponent<Button>();
            button.targetGraphic = cardBg;

            var label = MakeText(inner, "Label", "", 14f, TextAlignmentOptions.Center, Text);
            InsetX(label.rectTransform, 14f);
            label.enableWordWrapping = true;
            return button;
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
            var ascendLine = MakeText(sheetRt, "AscendLine", "", 15f, TextAlignmentOptions.Center, Text);
            SetBand(ascendLine.rectTransform, 0.74f, 0.86f);
            InsetX(ascendLine.rectTransform, 14f);
            var fallLine = MakeText(sheetRt, "FallLine", "", 15f, TextAlignmentOptions.Center, Text);
            SetBand(fallLine.rectTransform, 0.62f, 0.74f);
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
                "Ascend: 60%. Fall: 40%.\nEvery Tribulation, every generation, same odds.\nBoth outcomes grant an Ancestor.",
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

        private static CouncilScreenView BuildCouncilScreenUi(RectTransform root)
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

            var screen = controller.AddComponent<CouncilScreenView>();
            Assign(screen, "_root", shrineRoot.gameObject);
            Assign(screen, "_closeButton", close);

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
                var contribution = MakeText(row.rectTransform, "Contribution", "", 15f, TextAlignmentOptions.MidlineRight, Gold);
                Inset(contribution.rectTransform, right: 12f);

                AssignPath(screen, $"_rows.Array.data[{i}].root", row.gameObject, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].motif", motif, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].title", rowTitle, ensureRowsSize: 5);
                AssignPath(screen, $"_rows.Array.data[{i}].contribution", contribution, ensureRowsSize: 5);
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
            var dim = MakeImage(settingsRoot, "Dim", new Color(0f, 0f, 0f, 0.8f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            var panel = MakeImage(settingsRoot, "Panel", Panel);
            var panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.08f, 0.26f);
            panelRt.anchorMax = new Vector2(0.92f, 0.74f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var title = MakeText(panelRt, "Title", "Settings", 20f, TextAlignmentOptions.Center, Gold);
            SetBand(title.rectTransform, 0.86f, 0.97f);
            var bgmToggle = MakeToggle(panelRt, "BgmToggle", "Music", 0.70f, 0.82f);
            var sfxToggle = MakeToggle(panelRt, "SfxToggle", "Sound effects", 0.56f, 0.68f);
            var cloudStatus = MakeText(panelRt, "CloudStatus", "Local save only", 13f, TextAlignmentOptions.Center, TextDim);
            SetBand(cloudStatus.rectTransform, 0.44f, 0.52f);
            var about = MakeButton(panelRt, "AboutButton", "About & Glossary", PanelLine, 15f);
            var aboutRt = (RectTransform)about.transform;
            aboutRt.anchorMin = new Vector2(0.12f, 0.30f);
            aboutRt.anchorMax = new Vector2(0.88f, 0.42f);
            aboutRt.offsetMin = Vector2.zero;
            aboutRt.offsetMax = Vector2.zero;
            var version = MakeText(panelRt, "Version", "v0.1.0", 12f, TextAlignmentOptions.Center, TextDim);
            SetBand(version.rectTransform, 0.20f, 0.27f);
            var close = MakeButton(panelRt, "CloseButton", "Close", Gold, 16f);
            var closeRt = (RectTransform)close.transform;
            closeRt.anchorMin = new Vector2(0.20f, 0.04f);
            closeRt.anchorMax = new Vector2(0.80f, 0.16f);
            closeRt.offsetMin = Vector2.zero;
            closeRt.offsetMax = Vector2.zero;
            settingsRoot.gameObject.SetActive(false);

            AboutScreenView about_ = BuildAboutUi(controllerRt);

            var screen = controller.AddComponent<SettingsScreenView>();
            Assign(screen, "_root", settingsRoot.gameObject);
            Assign(screen, "_bgmToggle", bgmToggle);
            Assign(screen, "_sfxToggle", sfxToggle);
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

            var lbl = MakeText(rt, "Label", label, 15f, TextAlignmentOptions.MidlineLeft, Text);
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

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
