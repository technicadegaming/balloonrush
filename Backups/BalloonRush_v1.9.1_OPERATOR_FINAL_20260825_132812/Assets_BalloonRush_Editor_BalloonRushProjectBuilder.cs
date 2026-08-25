#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.Effects;
using BalloonRush.Gameplay;
using BalloonRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace BalloonRush.Editor
{
    public static class BalloonRushProjectBuilder
    {
        private const string Root = "Assets/BalloonRush";
        private const string ScenesPath = Root + "/Scenes";
        private const string PrefabsPath = Root + "/Prefabs";
        private const string ResourcesPath = Root + "/Resources";
        private const string DefinitionsPath = ResourcesPath + "/BalloonDefinitions";
        private const string GeneratedPath = Root + "/Generated";

        private static readonly Color Navy = new Color(0.012f, 0.025f, 0.09f, 1f);
        private static readonly Color Panel = new Color(0.025f, 0.055f, 0.16f, 0.94f);
        private static readonly Color Cyan = new Color(0.08f, 0.82f, 1f, 1f);
        private static readonly Color Pink = new Color(1f, 0.08f, 0.65f, 1f);
        private static readonly Color Gold = new Color(1f, 0.72f, 0.04f, 1f);
        private static readonly Color Green = new Color(0.12f, 0.92f, 0.28f, 1f);
        private static readonly Color Purple = new Color(0.65f, 0.18f, 1f, 1f);
        private static readonly Color Red = new Color(1f, 0.08f, 0.12f, 1f);

        private static TMP_FontAsset fontAsset;
        private static Sprite builtinSprite;
        private static Sprite knobSprite;
        private static Material spriteMaterial;

        [MenuItem("Tools/Balloon Rush/Build Complete Game", priority = 1)]
        public static void BuildCompleteGame()
        {
            BuildCompleteGameInternal(true);
        }

        public static void BuildCompleteGameSilent()
        {
            BuildCompleteGameInternal(false);
        }

        [MenuItem("Tools/Balloon Rush/Build Windows Cabinet", priority = 2)]
        public static void BuildWindowsCabinet()
        {
            try
            {
                BuildCompleteGameInternal(false);
                string buildFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows"));
                Directory.CreateDirectory(buildFolder);
                string executablePath = Path.Combine(buildFolder, "BalloonRush.exe");
                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenePaths(),
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
                }

                CopyCabinetLauncher(buildFolder);

                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(executablePath);
                    EditorUtility.DisplayDialog("Balloon Rush Windows Build", $"Cabinet build created at:\n{executablePath}", "Open Folder");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Balloon Rush Build Failed", exception.Message, "Close");
                    return;
                }
                throw;
            }
        }

        public static void BuildWindowsCabinetCommandLine()
        {
            BuildWindowsCabinet();
        }

        private static void BuildCompleteGameInternal(bool showDialog)
        {
            try
            {
                EnsureFolders();
                LoadOrCreateSharedAssets();
                GameConfig config = CreateConfigurationAssets();
                BalloonDefinition[] definitions = CreateBalloonDefinitions();
                Balloon balloonPrefab = CreateBalloonPrefab();
                TextMeshPro floatingTextPrefab = CreateFloatingTextPrefab();

                string boot = BuildBootScene();
                string attract = BuildAttractScene();
                string game = BuildMainGameScene(config, definitions, balloonPrefab, floatingTextPrefab);
                string results = BuildResultsScene();
                string operatorMenu = BuildOperatorScene();

                ConfigureBuildSettings(boot, attract, game, results, operatorMenu);
                ConfigurePlayerSettings(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                BalloonRushPreflightValidator.ValidateOrThrow();
                EditorSceneManager.OpenScene(boot);
                if (showDialog && !Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Balloon Rush Built",
                        "Balloon Rush v1.3.0 has been generated. Press Play from the Boot scene.\n\nDevelopment controls:\nC = add credit\nEnter or P = start\nLeft Arrow or A = lane left\nRight Arrow or D = lane right\nUp Arrow or Space = pop\nM = operator menu\nEscape = open/close debug panel\nF2-F6 = debug actions while the panel is open",
                        "Play");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (showDialog && !Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Balloon Rush Build Failed", exception.Message, "Close");
                }
                throw;
            }
        }

        [MenuItem("Tools/Balloon Rush/Open Reference Art", priority = 20)]
        private static void OpenReferenceArt()
        {
            UnityEngine.Object reference = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Root + "/ReferenceArt/GameplayMockup.png");
            Selection.activeObject = reference;
            EditorGUIUtility.PingObject(reference);
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ScenesPath,
                PrefabsPath,
                ResourcesPath,
                DefinitionsPath,
                GeneratedPath,
                Root + "/Animations",
                Root + "/Audio",
                Root + "/Materials",
                Root + "/Particles",
                Root + "/ScriptableObjects",
                Root + "/Sprites",
                Root + "/Fonts"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                EnsureFolder(folders[i]);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void LoadOrCreateSharedAssets()
        {
            builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (builtinSprite == null)
            {
                builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            }

            knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (knobSprite == null)
            {
                knobSprite = builtinSprite;
            }

            string materialPath = GeneratedPath + "/SpriteLine.mat";
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (spriteMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                spriteMaterial = new Material(shader != null ? shader : Shader.Find("UI/Default"));
                AssetDatabase.CreateAsset(spriteMaterial, materialPath);
            }

            string fontPath = Root + "/Fonts/BalloonRushFont.asset";
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (fontAsset == null)
            {
                Font sourceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (sourceFont == null)
                {
                    sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                if (sourceFont != null)
                {
                    fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
                    if (fontAsset != null)
                    {
                        AssetDatabase.CreateAsset(fontAsset, fontPath);
                        if (fontAsset.atlasTexture != null && !AssetDatabase.Contains(fontAsset.atlasTexture))
                        {
                            fontAsset.atlasTexture.name = "BalloonRushFont Atlas";
                            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                        }
                        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
                        {
                            fontAsset.material.name = "BalloonRushFont Material";
                            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                        }
                        EditorUtility.SetDirty(fontAsset);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            if (fontAsset == null)
            {
                fontAsset = TMP_Settings.defaultFontAsset;
            }

            if (fontAsset != null)
            {
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                fontAsset.isMultiAtlasTexturesEnabled = true;
                EditorUtility.SetDirty(fontAsset);
            }
        }

        private static GameConfig CreateConfigurationAssets()
        {
            PayoutConfig payout = CreateOrLoadAsset<PayoutConfig>(ResourcesPath + "/PayoutConfig.asset");
            payout.balanceVersion = 2;
            payout.visibleTiers = new[] { 500, 250, 100, 50, 25, 10, 5, 1 };
            payout.minimumTicketsPerGame = 5;
            payout.regularTicketsCap = 125;
            payout.greenTickets = 1;
            payout.blueTickets = 5;
            payout.goldenTriggerTickets = 1;
            payout.mysteryMinimum = 1;
            payout.mysteryMaximum = 5;
            payout.mysteryGoldenChance = 0.01f;
            payout.jackpotTickets = 500;
            payout.maximumTicketsPerGame = 625;
            payout.goodTicketMultiplier = 1f;
            payout.greatTicketMultiplier = 1f;
            payout.perfectTicketMultiplier = 1.10f;
            payout.goldenGreatReward = 25;
            payout.goldenGoodReward = 10;
            payout.goldenMissReward = 3;
            payout.combo5Multiplier = 1f;
            payout.combo10Multiplier = 1f;
            payout.combo15Multiplier = 1.05f;
            payout.combo20Multiplier = 1.10f;
            payout.combo30Multiplier = 1.15f;
            EditorUtility.SetDirty(payout);

            DifficultyConfig difficulty = CreateOrLoadAsset<DifficultyConfig>(ResourcesPath + "/DifficultyConfig.asset");
            difficulty.speedMultiplier = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.85f);
            difficulty.spawnIntervalMultiplier = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f);
            difficulty.dangerMultiplier = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.6f);
            difficulty.timingWindowScale = AnimationCurve.EaseInOut(0f, 1.15f, 1f, 0.85f);
            EditorUtility.SetDirty(difficulty);

            AudioConfig audio = CreateOrLoadAsset<AudioConfig>(ResourcesPath + "/AudioConfig.asset");
            EditorUtility.SetDirty(audio);

            GameConfig config = CreateOrLoadAsset<GameConfig>(ResourcesPath + "/BalloonRushConfig.asset");
            config.targetWidth = 1080;
            config.targetHeight = 1920;
            config.targetFrameRate = 60;
            config.buildVersion = "1.4.0";
            config.enforcePortraitResolutionInPlayer = true;
            config.playerFullScreenMode = FullScreenMode.FullScreenWindow;
            config.hideCursorInPlayer = true;
            config.runInBackground = true;
            config.runtimeLogMaxKilobytes = 2048;
            config.laneSpacing = 2.4f;
            config.spawnY = -6.8f;
            config.despawnY = 6.8f;
            config.hitZoneY = 3.15f;
            config.hitZoneHalfHeight = 0.82f;
            config.balloonPoolSize = 48;
            config.floatingTextPoolSize = 24;
            config.resultsTimeout = 12f;
            config.allowDebugShortcutsInRelease = false;
            config.payoutConfig = payout;
            config.difficultyConfig = difficulty;
            config.audioConfig = audio;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static BalloonDefinition[] CreateBalloonDefinitions()
        {
            List<BalloonDefinition> definitions = new List<BalloonDefinition>
            {
                CreateDefinition("Green", "green", "GREEN +1", BalloonKind.Green, new Color(0.16f, 0.95f, 0.25f), 100, 1, 1f, false, BalloonSpecialBehavior.None),
                CreateDefinition("Blue", "blue", "BLUE +5", BalloonKind.Blue, new Color(0.08f, 0.55f, 1f), 350, 5, 0.08f, false, BalloonSpecialBehavior.None),
                CreateDefinition("Multiplier", "x2", "PAYOUT x2", BalloonKind.Multiplier, Purple, 250, 0, 0.025f, false, BalloonSpecialBehavior.DoublePayout),
                CreateDefinition("Mystery", "mystery", "MYSTERY", BalloonKind.Mystery, Gold, 250, 0, 0.03f, false, BalloonSpecialBehavior.MysteryReward),
                CreateDefinition("Bomb", "bomb", "BOMB", BalloonKind.Bomb, Red, 0, 0, 0.10f, true, BalloonSpecialBehavior.Dangerous),
                CreateDefinition("SuperBomb", "superbomb", "SUPER BOMB", BalloonKind.SuperBomb, new Color(0.05f, 0.05f, 0.07f), 0, 0, 0.01f, true, BalloonSpecialBehavior.Dangerous),
                CreateDefinition("GoldenTrigger", "golden", "GOLDEN BALLOON", BalloonKind.GoldenTrigger, Gold, 600, 1, 0.0004f, false, BalloonSpecialBehavior.StartGoldenRound),
                CreateDefinition("GoldenJackpot", "jackpot", "GOLDEN JACKPOT", BalloonKind.GoldenJackpot, new Color(1f, 0.86f, 0.05f), 1000, 0, 0f, false, BalloonSpecialBehavior.ResolveJackpot)
            };
            return definitions.ToArray();
        }

        private static BalloonDefinition CreateDefinition(
            string fileName,
            string id,
            string displayName,
            BalloonKind kind,
            Color color,
            int points,
            int tickets,
            float weight,
            bool dangerous,
            BalloonSpecialBehavior behavior)
        {
            string path = DefinitionsPath + "/" + fileName + ".asset";
            BalloonDefinition definition = CreateOrLoadAsset<BalloonDefinition>(path);
            definition.Configure(id, displayName, kind, null, color, points, tickets, weight, dangerous, behavior);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static Balloon CreateBalloonPrefab()
        {
            string path = PrefabsPath + "/Balloon.prefab";
            GameObject root = new GameObject("Balloon");
            Balloon balloon = root.AddComponent<Balloon>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            SpriteRenderer glow = CreateWorldSprite("Glow", visual.transform, Vector3.zero, new Vector2(2.4f, 2.4f), new Color(0.2f, 0.8f, 1f, 0.2f), 4);
            SpriteRenderer body = CreateWorldSprite("Body", visual.transform, Vector3.zero, new Vector2(1.65f, 2.0f), Color.white, 5);

            TextMeshPro icon = CreateWorldText("Icon", visual.transform, "+1", 5.5f, Color.white, 6);
            icon.rectTransform.sizeDelta = new Vector2(2f, 1f);
            icon.transform.localPosition = new Vector3(0f, 0.12f, -0.05f);
            icon.enableAutoSizing = true;
            icon.fontSizeMin = 2f;
            icon.fontSizeMax = 5.5f;
            icon.textWrappingMode = TextWrappingModes.NoWrap;

            GameObject stringObject = new GameObject("String");
            stringObject.transform.SetParent(visual.transform, false);
            LineRenderer line = stringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(0f, -0.75f, 0f));
            line.SetPosition(1, new Vector3(0.08f, -1.25f, 0f));
            line.widthMultiplier = 0.025f;
            line.material = spriteMaterial;
            line.sortingOrder = 3;

            balloon.ConfigureVisuals(visual.transform, body, glow, icon, line);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<Balloon>();
        }

        private static TextMeshPro CreateFloatingTextPrefab()
        {
            string path = PrefabsPath + "/FloatingText.prefab";
            GameObject root = new GameObject("FloatingText");
            TextMeshPro text = root.AddComponent<TextMeshPro>();
            text.font = fontAsset;
            text.text = "PERFECT +5";
            text.fontSize = 4.5f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.yellow;
            text.sortingOrder = 50;
            text.rectTransform.sizeDelta = new Vector2(3.5f, 1.2f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<TextMeshPro>();
        }

        private static string BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("BalloonRushBootstrap");
            bootstrap.AddComponent<GameBootstrap>();
            string path = ScenesPath + "/Boot.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildAttractScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSceneCamera(Navy, false);
            CreateEventSystem();

            GameObject sceneRoot = new GameObject("AttractModeRoot");
            sceneRoot.AddComponent<SceneBootstrapGuard>();
            AttractModeManager manager = sceneRoot.AddComponent<AttractModeManager>();
            Canvas canvas = CreateCanvas("Attract Canvas");
            RectTransform root = (RectTransform)canvas.transform;

            CreatePanel(root, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Navy, false);
            CreatePanel(root, "Center Glow", new Vector2(0.06f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.04f, 0.13f, 0.72f), false);
            CreateNeonSideBars(root);

            RectTransform creditsPanel = CreatePanel(root, "Credits Panel", new Vector2(0.035f, 0.902f), new Vector2(0.235f, 0.982f), Vector2.zero, Vector2.zero, Panel, true);
            TMP_Text credits = CreateText(creditsPanel, "CREDITS\n0", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(credits.rectTransform, 6f, 6f, 3f, 3f);
            EnableAutoSize(credits, 20f, 31f);

            RectTransform highScorePanel = CreatePanel(root, "High Score Panel", new Vector2(0.275f, 0.902f), new Vector2(0.725f, 0.982f), Vector2.zero, Vector2.zero, new Color(0.025f, 0.08f, 0.20f, 0.96f), true);
            TMP_Text highScore = CreateText(highScorePanel, "HIGH SCORE\n0", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            Stretch(highScore.rectTransform, 6f, 6f, 3f, 3f);
            EnableAutoSize(highScore, 20f, 31f);

            RectTransform jackpotPanel = CreatePanel(root, "Jackpot Panel", new Vector2(0.765f, 0.902f), new Vector2(0.965f, 0.982f), Vector2.zero, Vector2.zero, new Color(0.28f, 0.025f, 0.025f, 0.98f), true);
            SetOutlineColor(jackpotPanel, Gold);
            TMP_Text jackpot = CreateText(jackpotPanel, "JACKPOT\n500 TICKETS", 29f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            Stretch(jackpot.rectTransform, 4f, 4f, 3f, 3f);
            EnableAutoSize(jackpot, 18f, 29f);

            RectTransform popBadge = CreatePanel(root, "Pop Badge", new Vector2(0.385f, 0.842f), new Vector2(0.615f, 0.895f), Vector2.zero, Vector2.zero, Red, true);
            SetOutlineColor(popBadge, Gold);
            TMP_Text popBadgeText = CreateText(popBadge, "POP!", 44f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(popBadgeText.rectTransform, 4f, 4f, 1f, 1f);
            AddTextShadow(popBadgeText, new Color(0.45f, 0.01f, 0.02f, 0.9f), new Vector2(3f, -3f));

            TMP_Text logo = CreateText(root, "BALLOON\nRUSH", 92f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(logo.rectTransform, new Vector2(0.10f, 0.705f), new Vector2(0.90f, 0.842f), Vector2.zero, Vector2.zero);
            EnableAutoSize(logo, 58f, 92f);
            logo.lineSpacing = -9f;
            AddTextShadow(logo, Pink, new Vector2(6f, -6f));

            TMP_Text tagline = CreateText(root, "SELECT A LANE - POP IN THE HIT ZONE", 35f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(tagline.rectTransform, new Vector2(0.06f, 0.655f), new Vector2(0.94f, 0.705f), Vector2.zero, Vector2.zero);
            EnableAutoSize(tagline, 24f, 35f);

            TMP_Text instruction = CreateText(root, "LEFT/RIGHT SELECT   UP/SPACE POPS   M OPERATOR", 25f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(instruction.rectTransform, new Vector2(0.05f, 0.617f), new Vector2(0.95f, 0.655f), Vector2.zero, Vector2.zero);
            EnableAutoSize(instruction, 18f, 25f);

            RectTransform gameField = CreatePanel(root, "Demo Field", new Vector2(0.075f, 0.205f), new Vector2(0.925f, 0.612f), Vector2.zero, Vector2.zero, new Color(0.008f, 0.026f, 0.105f, 0.98f), true);
            gameField.gameObject.AddComponent<RectMask2D>();
            SetOutlineColor(gameField, Cyan);

            for (int i = 0; i < 3; i++)
            {
                float minX = 0.018f + i * 0.328f;
                RectTransform lane = CreatePanel(gameField, "Demo Lane " + (i + 1), new Vector2(minX, 0.018f), new Vector2(minX + 0.309f, 0.982f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.10f + i * 0.015f, 0.22f + i * 0.025f, 0.48f), true);
                SetOutlineColor(lane, i == 1 ? Gold : new Color(0.08f, 0.55f, 1f, 0.78f));
                TMP_Text laneText = CreateText(lane, "LANE " + (i + 1), 20f, FontStyles.Bold, TextAlignmentOptions.Top, new Color(1f, 1f, 1f, 0.42f));
                SetRect(laneText.rectTransform, new Vector2(0f, 0.92f), Vector2.one, Vector2.zero, Vector2.zero);
            }

            RectTransform hitZone = CreatePanel(gameField, "Hit Zone", new Vector2(0.02f, 0.66f), new Vector2(0.98f, 0.765f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.67f, 1f, 0.30f), true);
            SetOutlineColor(hitZone, new Color(0.20f, 0.95f, 1f, 1f));
            TMP_Text hitText = CreateText(hitZone, "HIT ZONE - POP NOW", 34f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(hitText.rectTransform, 8f, 8f, 2f, 2f);
            EnableAutoSize(hitText, 22f, 34f);

            RectTransform[] demoBalloons = new RectTransform[15];
            Color blue = new Color(0.08f, 0.55f, 1f);
            Color[] colors = { Green, blue, Purple, Gold, Red, Green, blue, Gold, Purple, Green, Red, Gold, blue, Green, Purple };
            string[] icons = { "+1", "+5", "x2", "?", "!", "+1", "+5", "GOLD", "x2", "+1", "!", "?", "+5", "+1", "x2" };
            for (int i = 0; i < demoBalloons.Length; i++)
            {
                float x = 0.17f + (i % 3) * 0.33f;
                float y = -0.24f + (i / 3) * 0.235f;
                demoBalloons[i] = CreateUiBalloon(gameField, "Demo Balloon " + i, new Vector2(x, y), colors[i], icons[i]);
            }

            TMP_Text message = CreateText(root, string.Empty, 40f, FontStyles.Bold, TextAlignmentOptions.Center, Red);
            SetRect(message.rectTransform, new Vector2(0.10f, 0.435f), new Vector2(0.90f, 0.515f), Vector2.zero, Vector2.zero);
            EnableAutoSize(message, 26f, 40f);

            RectTransform controls = CreatePanel(root, "Controls", new Vector2(0.055f, 0.065f), new Vector2(0.945f, 0.188f), Vector2.zero, Vector2.zero, Panel, true);
            CreateControlDisplay(controls, new Vector2(0.18f, 0.5f), "<", "LEFT", "LEFT / A", new Color(0.08f, 0.52f, 1f));
            CreateControlDisplay(controls, new Vector2(0.50f, 0.5f), "POP", "POP", "UP / SPACE", Red);
            CreateControlDisplay(controls, new Vector2(0.82f, 0.5f), ">", "RIGHT", "RIGHT / D", Green);

            TMP_Text startPrompt = CreateText(root, "PRESS C TO ADD CREDIT", 47f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(startPrompt.rectTransform, new Vector2(0.08f, 0.010f), new Vector2(0.92f, 0.060f), Vector2.zero, Vector2.zero);
            EnableAutoSize(startPrompt, 30f, 47f);

            manager.Configure(logo, tagline, credits, highScore, jackpot, startPrompt, message, demoBalloons);
            string path = ScenesPath + "/AttractMode.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildMainGameScene(GameConfig config, BalloonDefinition[] definitions, Balloon balloonPrefab, TextMeshPro floatingTextPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateSceneCamera(Navy, true);
            camera.orthographicSize = 9.6f;
            CreateEventSystem();

            GameObject sceneRoot = new GameObject("MainGameRoot");
            sceneRoot.AddComponent<SceneBootstrapGuard>();
            GameManager gameManager = sceneRoot.AddComponent<GameManager>();

            GameObject world = new GameObject("Gameplay Field");
            CreateWorldSprite("Outer Field Glow", world.transform, new Vector3(0f, 0f, 0.7f), new Vector2(8.25f, 14.35f), new Color(0.02f, 0.38f, 0.65f, 0.15f), -40);
            CreateWorldSprite("Field Backplate", world.transform, new Vector3(0f, 0f, 0.6f), new Vector2(7.75f, 13.95f), new Color(0.004f, 0.018f, 0.075f, 0.98f), -35);

            Transform[] laneAnchors = new Transform[3];
            SpriteRenderer[] laneHighlights = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * config.laneSpacing;
                GameObject lane = new GameObject("Lane " + (i + 1));
                lane.transform.SetParent(world.transform, false);
                lane.transform.position = new Vector3(x, 0f, 0f);
                laneAnchors[i] = lane.transform;
                laneHighlights[i] = CreateWorldSprite("Lane Glow", lane.transform, new Vector3(0f, 0f, 0.2f), new Vector2(2.05f, 13.2f), new Color(0.03f, 0.18f, 0.45f, 0.14f), -20);
                CreateWorldSprite("Lane Inner", lane.transform, new Vector3(0f, 0f, 0.35f), new Vector2(1.90f, 13.0f), new Color(0.015f, 0.07f + i * 0.015f, 0.15f + i * 0.02f, 0.42f), -19);
                CreateWorldSprite("Lane Border Left", lane.transform, new Vector3(-1.04f, 0f, 0f), new Vector2(0.035f, 13.2f), new Color(0.08f, 0.65f, 1f, 0.75f), -10);
                CreateWorldSprite("Lane Border Right", lane.transform, new Vector3(1.04f, 0f, 0f), new Vector2(0.035f, 13.2f), new Color(0.08f, 0.65f, 1f, 0.75f), -10);
            }

            LaneManager laneManager = world.AddComponent<LaneManager>();
            laneManager.Configure(laneAnchors, laneHighlights);

            GameObject hitZoneObject = new GameObject("Hit Zone");
            hitZoneObject.transform.SetParent(world.transform, false);
            hitZoneObject.transform.position = new Vector3(0f, config.hitZoneY, 0f);
            CreateWorldSprite("Hit Zone Fill", hitZoneObject.transform, Vector3.zero, new Vector2(6.56f, config.hitZoneHalfHeight * 2f), new Color(0.04f, 0.68f, 0.95f, 0.17f), 8);
            SpriteRenderer[] borders = new SpriteRenderer[4];
            borders[0] = CreateWorldSprite("Top", hitZoneObject.transform, new Vector3(0f, config.hitZoneHalfHeight, 0f), new Vector2(6.65f, 0.10f), Cyan, 10);
            borders[1] = CreateWorldSprite("Bottom", hitZoneObject.transform, new Vector3(0f, -config.hitZoneHalfHeight, 0f), new Vector2(6.65f, 0.10f), Cyan, 10);
            borders[2] = CreateWorldSprite("Left", hitZoneObject.transform, new Vector3(-3.28f, 0f, 0f), new Vector2(0.10f, config.hitZoneHalfHeight * 2f), Cyan, 10);
            borders[3] = CreateWorldSprite("Right", hitZoneObject.transform, new Vector3(3.28f, 0f, 0f), new Vector2(0.10f, config.hitZoneHalfHeight * 2f), Cyan, 10);
            TextMeshPro hitLabel = CreateWorldText("Hit Zone Label", hitZoneObject.transform, "HIT ZONE", 2.25f, Color.white, 12);
            hitLabel.transform.localPosition = new Vector3(0f, 0.44f, -0.05f);
            hitLabel.rectTransform.sizeDelta = new Vector2(5.4f, 0.72f);
            hitLabel.textWrappingMode = TextWrappingModes.NoWrap;
            HitZone hitZone = hitZoneObject.AddComponent<HitZone>();
            hitZone.Configure(config.hitZoneHalfHeight, borders);

            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(sceneRoot.transform, false);
            RoundManager roundManager = CreateSystem<RoundManager>(systems.transform, "Round Manager");
            ComboManager comboManager = CreateSystem<ComboManager>(systems.transform, "Combo Manager");
            ScoreManager scoreManager = CreateSystem<ScoreManager>(systems.transform, "Score Manager");
            DifficultyManager difficultyManager = CreateSystem<DifficultyManager>(systems.transform, "Difficulty Manager");
            JackpotManager jackpotManager = CreateSystem<JackpotManager>(systems.transform, "Jackpot Manager");
            GoldenRoundManager goldenRoundManager = CreateSystem<GoldenRoundManager>(systems.transform, "Golden Round Manager");
            BalloonManager balloonManager = CreateSystem<BalloonManager>(systems.transform, "Balloon Manager");
            BalloonSpawner balloonSpawner = CreateSystem<BalloonSpawner>(systems.transform, "Balloon Spawner");

            GameObject poolObject = new GameObject("Balloon Pool");
            poolObject.transform.SetParent(systems.transform, false);
            BalloonPool balloonPool = poolObject.AddComponent<BalloonPool>();
            balloonPool.Configure(balloonPrefab, config.balloonPoolSize);

            GameObject textPoolObject = new GameObject("Floating Text Pool");
            textPoolObject.transform.SetParent(systems.transform, false);
            FloatingTextPool floatingTextPool = textPoolObject.AddComponent<FloatingTextPool>();
            floatingTextPool.Configure(floatingTextPrefab, config.floatingTextPoolSize);

            GameObject effectsObject = new GameObject("Effects");
            effectsObject.transform.SetParent(systems.transform, false);
            ScreenShake screenShake = effectsObject.AddComponent<ScreenShake>();
            screenShake.Configure(camera.transform, null);
            EffectsManager effectsManager = effectsObject.AddComponent<EffectsManager>();
            effectsManager.Configure(screenShake, floatingTextPool, null, null, null, null, null);

            // World-space playfield treatment. The HUD remains independent from gameplay logic,
            // but the generated scene should already read like an arcade cabinet rather than a tutorial.
            CreateWorldSprite("Field Core", world.transform, new Vector3(0f, -0.15f, 0.45f), new Vector2(7.15f, 13.75f), new Color(0.005f, 0.02f, 0.085f, 0.98f), -60);
            CreateWorldSprite("Field Inner Glow", world.transform, new Vector3(0f, -0.15f, 0.40f), new Vector2(6.85f, 13.45f), new Color(0.015f, 0.16f, 0.28f, 0.24f), -55);
            CreateWorldSprite("Field Left Rail", world.transform, new Vector3(-3.48f, -0.15f, 0.35f), new Vector2(0.08f, 13.75f), Pink, -5);
            CreateWorldSprite("Field Right Rail", world.transform, new Vector3(3.48f, -0.15f, 0.35f), new Vector2(0.08f, 13.75f), Cyan, -5);

            Canvas canvas = CreateCanvas("Gameplay Canvas");
            RectTransform canvasRoot = (RectTransform)canvas.transform;
            CreatePanel(canvasRoot, "Top Color Wash", new Vector2(0f, 0.80f), Vector2.one, Vector2.zero, Vector2.zero, new Color(0.19f, 0.015f, 0.34f, 0.20f), false);
            CreatePanel(canvasRoot, "Bottom Color Wash", Vector2.zero, new Vector2(1f, 0.225f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.24f, 0.40f, 0.18f), false);
            CreateNeonSideBars(canvasRoot);

            RectTransform topBar = CreatePanel(canvasRoot, "Top Bar", new Vector2(0.018f, 0.885f), new Vector2(0.982f, 0.995f), Vector2.zero, Vector2.zero, new Color(0.012f, 0.03f, 0.105f, 0.98f), true);
            TMP_Text logo = CreateText(topBar, "<color=#FFD427>POP!</color>  <color=#FFFFFF>BALLOON</color>  <color=#FF4B32>RUSH</color>", 43f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(logo.rectTransform, new Vector2(0.22f, 0.46f), new Vector2(0.78f, 0.96f), Vector2.zero, Vector2.zero);
            AddTextShadow(logo, Pink, new Vector2(3f, -3f));

            RectTransform ticketsPanel = CreatePanel(topBar, "Tickets", new Vector2(0.008f, 0.10f), new Vector2(0.215f, 0.90f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.10f, 0.23f, 0.98f), true);
            TMP_Text ticketsText = CreateText(ticketsPanel, "TICKETS\n0", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            Stretch(ticketsText.rectTransform, 7f, 7f, 2f, 2f);

            RectTransform jackpotPanel = CreatePanel(topBar, "Jackpot", new Vector2(0.785f, 0.10f), new Vector2(0.992f, 0.90f), Vector2.zero, Vector2.zero, new Color(0.30f, 0.02f, 0.025f, 0.98f), true);
            TMP_Text jackpotText = CreateText(jackpotPanel, "JACKPOT\n500 TICKETS", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            Stretch(jackpotText.rectTransform, 7f, 7f, 2f, 2f);

            TMP_Text scoreText = CreateText(topBar, "SCORE  0", 23f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(scoreText.rectTransform, new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.47f), Vector2.zero, Vector2.zero);

            RectTransform timerPanel = CreatePanel(canvasRoot, "Timer", new Vector2(0.405f, 0.825f), new Vector2(0.595f, 0.885f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.09f, 0.22f, 0.98f), true);
            TMP_Text timerLabel = CreateText(timerPanel, "TIME", 16f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.45f, 0.88f, 1f));
            SetRect(timerLabel.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.96f), Vector2.zero, Vector2.zero);
            TMP_Text timerText = CreateText(timerPanel, "45.0", 39f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(timerText.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.70f), Vector2.zero, Vector2.zero);

            RectTransform comboPanel = CreatePanel(canvasRoot, "Combo Meter", new Vector2(0.022f, 0.235f), new Vector2(0.157f, 0.815f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.97f), true);
            TMP_Text comboText = CreateText(comboPanel, "COMBO\nx0", 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(comboText.rectTransform, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.985f), Vector2.zero, Vector2.zero);
            RectTransform comboTrack = CreatePanel(comboPanel, "Combo Track", new Vector2(0.33f, 0.15f), new Vector2(0.67f, 0.80f), Vector2.zero, Vector2.zero, new Color(0.006f, 0.018f, 0.065f, 1f), true);
            Image comboFill = CreateImage(comboTrack, "Combo Fill", Cyan);
            SetRect(comboFill.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));
            comboFill.type = Image.Type.Filled;
            comboFill.fillMethod = Image.FillMethod.Vertical;
            comboFill.fillOrigin = 0;
            comboFill.fillAmount = 0f;
            TMP_Text comboHint = CreateText(comboPanel, "KEEP IT\nGOING!", 19f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(comboHint.rectTransform, new Vector2(0.02f, 0.015f), new Vector2(0.98f, 0.14f), Vector2.zero, Vector2.zero);

            RectTransform payoutPanel = CreatePanel(canvasRoot, "Payout Ladder", new Vector2(0.843f, 0.235f), new Vector2(0.978f, 0.815f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.97f), true);
            TMP_Text payoutTitle = CreateText(payoutPanel, "PAYOUT", 25f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(payoutTitle.rectTransform, new Vector2(0f, 0.90f), new Vector2(1f, 0.985f), Vector2.zero, Vector2.zero);
            int[] tiers = { 500, 250, 100, 50, 25, 10, 5, 1 };
            for (int i = 0; i < tiers.Length; i++)
            {
                float top = 0.88f - i * 0.103f;
                Color tierColor = i == 0
                    ? new Color(0.50f, 0.055f, 0.04f, 1f)
                    : new Color(0.035f, 0.16f + i * 0.008f, 0.33f, 1f);
                RectTransform tier = CreatePanel(payoutPanel, "Tier " + tiers[i], new Vector2(0.085f, top - 0.078f), new Vector2(0.915f, top), Vector2.zero, Vector2.zero, tierColor, true);
                TMP_Text tierText = CreateText(tier, tiers[i].ToString(), 25f, FontStyles.Bold, TextAlignmentOptions.Center, i == 0 ? Gold : Color.white);
                Stretch(tierText.rectTransform, 0f, 0f, 0f, 0f);
            }

            Image[] laneIndicators = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float minX = 0.185f + i * 0.216f;
                Color laneColor = i == 1
                    ? new Color(0.08f, 0.45f, 0.63f, 0.96f)
                    : new Color(0.025f, 0.14f, 0.32f, 0.96f);
                RectTransform indicatorPanel = CreatePanel(canvasRoot, "Lane Indicator " + (i + 1), new Vector2(minX, 0.785f), new Vector2(minX + 0.196f, 0.817f), Vector2.zero, Vector2.zero, laneColor, true);
                laneIndicators[i] = indicatorPanel.GetComponent<Image>();
                TMP_Text laneLabel = CreateText(indicatorPanel, "LANE " + (i + 1), 19f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
                Stretch(laneLabel.rectTransform, 2f, 2f, 1f, 1f);
            }

            RectTransform controls = CreatePanel(canvasRoot, "Control Display", new Vector2(0.048f, 0.012f), new Vector2(0.952f, 0.215f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.98f), true);
            TMP_Text controlHeader = CreateText(controls, "CABINET CONTROLS", 16f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.48f, 0.84f, 1f));
            SetRect(controlHeader.rectTransform, new Vector2(0.35f, 0.87f), new Vector2(0.65f, 0.98f), Vector2.zero, Vector2.zero);
            CreateControlDisplay(controls, new Vector2(0.18f, 0.48f), "<", "LEFT", "LEFT ARROW / A", new Color(0.08f, 0.52f, 1f));
            CreateControlDisplay(controls, new Vector2(0.50f, 0.48f), "POP", "POP", "UP ARROW / SPACE", Red);
            CreateControlDisplay(controls, new Vector2(0.82f, 0.48f), ">", "RIGHT", "RIGHT ARROW / D", Green);
            TMP_Text serviceHint = CreateText(controls, "M = OPERATOR MENU     ESC = DEBUG / SERVICE PANEL", 15f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.55f, 0.82f, 1f));
            SetRect(serviceHint.rectTransform, new Vector2(0.18f, 0.00f), new Vector2(0.82f, 0.075f), Vector2.zero, Vector2.zero);

            TMP_Text ratingText = CreateText(canvasRoot, string.Empty, 68f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(ratingText.rectTransform, new Vector2(0.29f, 0.705f), new Vector2(0.82f, 0.775f), Vector2.zero, Vector2.zero);
            EnableAutoSize(ratingText, 38f, 68f);
            ratingText.gameObject.SetActive(false);

            TMP_Text countdownText = CreateText(canvasRoot, string.Empty, 170f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(countdownText.rectTransform, new Vector2(0.19f, 0.34f), new Vector2(0.81f, 0.64f), Vector2.zero, Vector2.zero);
            countdownText.gameObject.SetActive(false);

            TMP_Text messageText = CreateText(canvasRoot, string.Empty, 52f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(messageText.rectTransform, new Vector2(0.18f, 0.545f), new Vector2(0.82f, 0.615f), Vector2.zero, Vector2.zero);
            EnableAutoSize(messageText, 29f, 52f);
            messageText.gameObject.SetActive(false);

            TMP_Text multiplierText = CreateText(canvasRoot, string.Empty, 31f, FontStyles.Bold, TextAlignmentOptions.Center, Purple);
            SetRect(multiplierText.rectTransform, new Vector2(0.18f, 0.705f), new Vector2(0.38f, 0.775f), Vector2.zero, Vector2.zero);
            EnableAutoSize(multiplierText, 20f, 31f);
            multiplierText.gameObject.SetActive(false);

            RectTransform goldenBanner = CreatePanel(canvasRoot, "Golden Round Banner", new Vector2(0.255f, 0.825f), new Vector2(0.745f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.36f, 0.19f, 0.005f, 0.98f), true);
            TMP_Text goldenTimer = CreateText(goldenBanner, "GOLDEN ROUND  10.0", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            Stretch(goldenTimer.rectTransform, 6f, 6f, 2f, 2f);
            goldenBanner.gameObject.SetActive(false);

            Image flashOverlay = CreateImage(canvasRoot, "Flash Overlay", Color.clear);
            SetRect(flashOverlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            flashOverlay.raycastTarget = false;
            flashOverlay.gameObject.SetActive(false);

            RectTransform debugPanel = CreatePanel(canvasRoot, "Debug Panel", new Vector2(0.105f, 0.16f), new Vector2(0.895f, 0.84f), Vector2.zero, Vector2.zero, new Color(0.002f, 0.008f, 0.028f, 0.97f), true);
            TMP_Text debugHeader = CreateText(debugPanel, "DEBUG / SERVICE PANEL", 35f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(debugHeader.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero);
            TMP_Text debugSubheader = CreateText(debugPanel, "ESC CLOSES PANEL   |   M OPENS OPERATOR SETTINGS", 18f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(debugSubheader.rectTransform, new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
            TMP_Text debugText = CreateText(debugPanel, "DEBUG", 25f, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
            SetRect(debugText.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.80f), Vector2.zero, Vector2.zero);
            debugText.textWrappingMode = TextWrappingModes.Normal;
            debugText.overflowMode = TextOverflowModes.Overflow;
            debugPanel.gameObject.SetActive(false);

            UIManager uiManager = canvas.gameObject.AddComponent<UIManager>();
            uiManager.Configure(ticketsText, scoreText, timerText, comboText, multiplierText, jackpotText, comboFill, laneIndicators, ratingText, countdownText, messageText, flashOverlay, goldenBanner.gameObject, goldenTimer, debugPanel.gameObject, debugText);
            DebugPanelManager debugManager = canvas.gameObject.AddComponent<DebugPanelManager>();

            comboManager.Configure(2.75f);
            scoreManager.Configure(comboManager, config.payoutConfig);
            difficultyManager.Configure(config.difficultyConfig, null);
            jackpotManager.Configure(scoreManager, null);
            goldenRoundManager.Configure(balloonSpawner, jackpotManager, null);
            roundManager.Configure(balloonSpawner, difficultyManager, goldenRoundManager, null);
            balloonSpawner.Configure(balloonPool, balloonManager, laneManager, difficultyManager, definitions, null);
            balloonManager.Configure(laneManager, hitZone, scoreManager, comboManager, difficultyManager, goldenRoundManager, effectsManager, uiManager, null);
            gameManager.Configure(roundManager, balloonManager, balloonSpawner, balloonPool, laneManager, hitZone, comboManager, scoreManager, difficultyManager, goldenRoundManager, jackpotManager, uiManager, effectsManager, screenShake, floatingTextPool, debugManager);
            debugManager.Configure(gameManager, uiManager);

            string path = ScenesPath + "/MainGame.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildResultsScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSceneCamera(Navy, false);
            CreateEventSystem();

            GameObject sceneRoot = new GameObject("ResultsRoot");
            sceneRoot.AddComponent<SceneBootstrapGuard>();
            ResultsManager manager = sceneRoot.AddComponent<ResultsManager>();
            Canvas canvas = CreateCanvas("Results Canvas");
            RectTransform root = (RectTransform)canvas.transform;
            CreatePanel(root, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Navy, false);
            CreatePanel(root, "Top Color Wash", new Vector2(0f, 0.72f), Vector2.one, Vector2.zero, Vector2.zero, new Color(0.20f, 0.02f, 0.34f, 0.25f), false);
            CreatePanel(root, "Bottom Color Wash", Vector2.zero, new Vector2(1f, 0.24f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.24f, 0.40f, 0.20f), false);
            CreateNeonSideBars(root);

            TMP_Text brand = CreateText(root, "<color=#FFD427>POP!</color>  <color=#FFFFFF>BALLOON</color>  <color=#FF4B32>RUSH</color>", 46f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(brand.rectTransform, new Vector2(0.14f, 0.93f), new Vector2(0.86f, 0.985f), Vector2.zero, Vector2.zero);
            AddTextShadow(brand, Pink, new Vector2(4f, -4f));

            TMP_Text title = CreateText(root, "RESULTS", 67f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(title.rectTransform, new Vector2(0.12f, 0.845f), new Vector2(0.88f, 0.93f), Vector2.zero, Vector2.zero);
            AddTextShadow(title, Cyan, new Vector2(5f, -5f));

            RectTransform ticketPanel = CreatePanel(root, "Ticket Result", new Vector2(0.13f, 0.555f), new Vector2(0.87f, 0.835f), Vector2.zero, Vector2.zero, new Color(0.29f, 0.13f, 0.005f, 0.98f), true);
            TMP_Text ticketHeader = CreateText(ticketPanel, "TOTAL TICKETS WON", 29f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(ticketHeader.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);
            TMP_Text tickets = CreateText(ticketPanel, "0\nTICKETS", 112f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(tickets.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.82f), Vector2.zero, Vector2.zero);
            AddTextShadow(tickets, new Color(0.60f, 0.20f, 0f, 0.9f), new Vector2(6f, -6f));

            RectTransform statsPanel = CreatePanel(root, "Statistics", new Vector2(0.085f, 0.245f), new Vector2(0.915f, 0.535f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.98f), true);
            TMP_Text statsHeader = CreateText(statsPanel, "GAME SUMMARY", 27f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(statsHeader.rectTransform, new Vector2(0.08f, 0.85f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);
            TMP_Text score = CreateText(statsPanel, "FINAL SCORE", 31f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(score.rectTransform, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.84f), Vector2.zero, Vector2.zero);
            TMP_Text combo = CreateText(statsPanel, "HIGHEST COMBO", 29f, FontStyles.Bold, TextAlignmentOptions.Center, Pink);
            SetRect(combo.rectTransform, new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.65f), Vector2.zero, Vector2.zero);
            TMP_Text accuracy = CreateText(statsPanel, "PERFECT / GREAT / GOOD / MISS", 23f, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            SetRect(accuracy.rectTransform, new Vector2(0.02f, 0.27f), new Vector2(0.98f, 0.48f), Vector2.zero, Vector2.zero);
            TMP_Text golden = CreateText(statsPanel, "GOLDEN BALLOONS", 24f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(golden.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.30f), Vector2.zero, Vector2.zero);
            TMP_Text jackpot = CreateText(statsPanel, "JACKPOT WON!", 27f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(jackpot.rectTransform, new Vector2(0.04f, 0.00f), new Vector2(0.96f, 0.15f), Vector2.zero, Vector2.zero);

            TMP_Text message = CreateText(root, string.Empty, 34f, FontStyles.Bold, TextAlignmentOptions.Center, Red);
            SetRect(message.rectTransform, new Vector2(0.08f, 0.195f), new Vector2(0.92f, 0.245f), Vector2.zero, Vector2.zero);
            message.gameObject.SetActive(false);

            RectTransform replayPanel = CreatePanel(root, "Replay Prompt", new Vector2(0.075f, 0.075f), new Vector2(0.925f, 0.19f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.10f, 0.23f, 0.97f), true);
            TMP_Text replay = CreateText(replayPanel, "ENTER OR P TO PLAY AGAIN", 37f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(replay.rectTransform, new Vector2(0.03f, 0.35f), new Vector2(0.97f, 0.92f), Vector2.zero, Vector2.zero);
            TMP_Text replayHint = CreateText(replayPanel, "C = CREDIT     M = OPERATOR MENU", 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.50f, 0.84f, 1f));
            SetRect(replayHint.rectTransform, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.37f), Vector2.zero, Vector2.zero);

            TMP_Text countdown = CreateText(root, "RETURNING IN 12", 23f, FontStyles.Normal, TextAlignmentOptions.Center, Cyan);
            SetRect(countdown.rectTransform, new Vector2(0.22f, 0.025f), new Vector2(0.78f, 0.070f), Vector2.zero, Vector2.zero);

            manager.Configure(title, tickets, score, combo, accuracy, golden, jackpot, replay, countdown, message);
            string path = ScenesPath + "/Results.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string BuildOperatorScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateSceneCamera(Navy, false);
            CreateEventSystem();

            GameObject sceneRoot = new GameObject("OperatorMenuRoot");
            sceneRoot.AddComponent<SceneBootstrapGuard>();
            OperatorMenuManager manager = sceneRoot.AddComponent<OperatorMenuManager>();
            Canvas canvas = CreateCanvas("Operator Canvas");
            RectTransform root = (RectTransform)canvas.transform;
            CreatePanel(root, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Navy, false);
            CreatePanel(root, "Top Color Wash", new Vector2(0f, 0.80f), Vector2.one, Vector2.zero, Vector2.zero, new Color(0.20f, 0.02f, 0.34f, 0.20f), false);
            CreateNeonSideBars(root);

            TMP_Text brand = CreateText(root, "<color=#FFD427>POP!</color>  <color=#FFFFFF>BALLOON</color>  <color=#FF4B32>RUSH</color>", 37f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(brand.rectTransform, new Vector2(0.16f, 0.947f), new Vector2(0.84f, 0.99f), Vector2.zero, Vector2.zero);
            TMP_Text title = CreateText(root, "OPERATOR SETTINGS", 48f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(title.rectTransform, new Vector2(0.06f, 0.895f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);
            AddTextShadow(title, Cyan, new Vector2(4f, -4f));
            TMP_Text exitHint = CreateText(root, "M OR ESC = RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE", 17f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.55f, 0.84f, 1f));
            SetRect(exitHint.rectTransform, new Vector2(0.06f, 0.867f), new Vector2(0.94f, 0.898f), Vector2.zero, Vector2.zero);

            TMP_Text settingsTitle = CreateText(root, "MACHINE SETTINGS", 25f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(settingsTitle.rectTransform, new Vector2(0.035f, 0.825f), new Vector2(0.965f, 0.865f), Vector2.zero, Vector2.zero);
            RectTransform scrollPanel = CreatePanel(root, "Settings Panel", new Vector2(0.025f, 0.25f), new Vector2(0.975f, 0.825f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.98f), true);
            ScrollRect scrollRect = CreateScrollView(scrollPanel, out RectTransform content);
            scrollRect.scrollSensitivity = 55f;

            RectTransform statsPanel = CreatePanel(root, "Statistics Panel", new Vector2(0.025f, 0.15f), new Vector2(0.655f, 0.235f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.98f), true);
            TMP_Text statsHeading = CreateText(statsPanel, "LIFETIME STATISTICS", 17f, FontStyles.Bold, TextAlignmentOptions.Center, Gold);
            SetRect(statsHeading.rectTransform, new Vector2(0.02f, 0.68f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
            TMP_Text statistics = CreateText(statsPanel, "Statistics", 20f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(statistics.rectTransform, new Vector2(0.025f, 0.05f), new Vector2(0.975f, 0.70f), Vector2.zero, Vector2.zero);
            statistics.textWrappingMode = TextWrappingModes.Normal;
            statistics.overflowMode = TextOverflowModes.Overflow;
            EnableAutoSize(statistics, 14f, 20f);

            RectTransform statusPanel = CreatePanel(root, "Status Panel", new Vector2(0.675f, 0.15f), new Vector2(0.975f, 0.235f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.10f, 0.20f, 0.98f), true);
            TMP_Text statusLabel = CreateText(statusPanel, "STATUS / INPUT TEST", 15f, FontStyles.Bold, TextAlignmentOptions.Center, Cyan);
            SetRect(statusLabel.rectTransform, new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
            TMP_Text status = CreateText(statusPanel, "Operator settings loaded.", 19f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(status.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.70f), Vector2.zero, Vector2.zero);
            EnableAutoSize(status, 13f, 19f);

            RectTransform buttonArea = CreatePanel(root, "Buttons", new Vector2(0.02f, 0.015f), new Vector2(0.98f, 0.145f), Vector2.zero, Vector2.zero, new Color(0.018f, 0.052f, 0.145f, 0.98f), true);
            Button save = CreateButton(buttonArea, "SAVE", new Vector2(0.01f, 0.52f), new Vector2(0.19f, 0.95f), Green);
            Button reset = CreateButton(buttonArea, "RESET DEFAULTS", new Vector2(0.205f, 0.52f), new Vector2(0.39f, 0.95f), Gold);
            Button testInputs = CreateButton(buttonArea, "TEST INPUTS", new Vector2(0.405f, 0.52f), new Vector2(0.59f, 0.95f), Cyan);
            Button testTickets = CreateButton(buttonArea, "TEST TICKETS", new Vector2(0.605f, 0.52f), new Vector2(0.79f, 0.95f), Purple);
            Button back = CreateButton(buttonArea, "BACK", new Vector2(0.805f, 0.52f), new Vector2(0.99f, 0.95f), Red);
            Button resetStats = CreateButton(buttonArea, "RESET STATISTICS", new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.44f), new Color(0.55f, 0.08f, 0.08f, 1f));

            manager.Configure(content, status, statistics, save, reset, testInputs, testTickets, resetStats, back);
            string path = ScenesPath + "/OperatorMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void ConfigureBuildSettings(params string[] scenePaths)
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }
            EditorBuildSettings.scenes = scenes;
        }


        private static string[] GetEnabledScenePaths()
        {
            List<string> paths = new List<string>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] != null && scenes[i].enabled && !string.IsNullOrEmpty(scenes[i].path))
                {
                    paths.Add(scenes[i].path);
                }
            }
            return paths.ToArray();
        }

        private static void ConfigurePlayerSettings(GameConfig config)
        {
            int width = config != null ? Mathf.Max(480, config.targetWidth) : 1080;
            int height = config != null ? Mathf.Max(800, config.targetHeight) : 1920;
            FullScreenMode fullScreenMode = config != null ? config.playerFullScreenMode : FullScreenMode.FullScreenWindow;

            PlayerSettings.companyName = "nick's Workspace";
            PlayerSettings.productName = "Balloon Rush";
            PlayerSettings.defaultScreenWidth = width;
            PlayerSettings.defaultScreenHeight = height;
            PlayerSettings.fullScreenMode = fullScreenMode;
            PlayerSettings.runInBackground = config == null || config.runInBackground;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.bundleVersion = config != null && !string.IsNullOrWhiteSpace(config.buildVersion) ? config.buildVersion : "1.4.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.nicksworkspace.balloonrush");
        }

        private static void CopyCabinetLauncher(string buildFolder)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string source = Path.Combine(projectRoot, "Hardware", "Windows", "LaunchBalloonRush.bat");
            if (!File.Exists(source))
            {
                Debug.LogWarning("Balloon Rush cabinet launcher was not found at: " + source);
                return;
            }

            string destination = Path.Combine(buildFolder, "LaunchBalloonRush.bat");
            File.Copy(source, destination, true);
        }

        private static Camera CreateSceneCamera(Color background, bool orthographic)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = orthographic;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name, typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static RectTransform CreatePanel(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            bool outline)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)panelObject.transform;
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = panelObject.AddComponent<Image>();
            image.sprite = builtinSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            if (outline)
            {
                Outline effect = panelObject.AddComponent<Outline>();
                effect.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f);
                effect.effectDistance = new Vector2(3f, -3f);
                effect.useGraphicAlpha = false;
            }
            return rect;
        }

        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = builtinSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(RectTransform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = fontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static TextMeshPro CreateWorldText(string name, Transform parent, string value, float size, Color color, int sortingOrder)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.font = fontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.sortingOrder = sortingOrder;
            return text;
        }

        private static SpriteRenderer CreateWorldSprite(string name, Transform parent, Vector3 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = localPosition;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = builtinSprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static RectTransform CreateUiBalloon(RectTransform parent, string name, Vector2 normalizedPosition, Color color, string icon)
        {
            GameObject balloonObject = new GameObject(name, typeof(RectTransform));
            balloonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)balloonObject.transform;
            rect.anchorMin = normalizedPosition;
            rect.anchorMax = normalizedPosition;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(126f, 158f);

            Image image = balloonObject.AddComponent<Image>();
            image.sprite = knobSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;

            Shadow shadow = balloonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, 0.75f);
            shadow.effectDistance = new Vector2(7f, -7f);

            Image highlight = CreateImage(rect, "Highlight", new Color(1f, 1f, 1f, 0.32f));
            highlight.sprite = knobSprite;
            highlight.type = Image.Type.Simple;
            highlight.preserveAspect = true;
            highlight.raycastTarget = false;
            SetRect(highlight.rectTransform, new Vector2(0.19f, 0.59f), new Vector2(0.45f, 0.86f), Vector2.zero, Vector2.zero);

            Image knot = CreateImage(rect, "Knot", new Color(color.r * 0.78f, color.g * 0.78f, color.b * 0.78f, 1f));
            knot.sprite = builtinSprite;
            knot.type = Image.Type.Sliced;
            knot.raycastTarget = false;
            SetRect(knot.rectTransform, new Vector2(0.43f, -0.01f), new Vector2(0.57f, 0.10f), Vector2.zero, Vector2.zero);

            Image stringImage = CreateImage(rect, "String", new Color(1f, 1f, 1f, 0.62f));
            stringImage.sprite = builtinSprite;
            stringImage.type = Image.Type.Sliced;
            stringImage.raycastTarget = false;
            SetRect(stringImage.rectTransform, new Vector2(0.492f, -0.27f), new Vector2(0.508f, 0.01f), Vector2.zero, Vector2.zero);

            TMP_Text label = CreateText(rect, icon, 43f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(label.rectTransform, new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.82f), Vector2.zero, Vector2.zero);
            EnableAutoSize(label, 18f, 43f);
            AddTextShadow(label, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));
            return rect;
        }

        private static void CreateControlDisplay(RectTransform parent, Vector2 normalizedCenter, string symbol, string label, string keyHint, Color color)
        {
            GameObject buttonObject = new GameObject(label + " Control", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = normalizedCenter;
            rect.anchorMax = normalizedCenter;
            rect.sizeDelta = new Vector2(230f, 194f);

            Image glow = CreateImage(rect, "Glow", new Color(color.r, color.g, color.b, 0.25f));
            glow.sprite = builtinSprite;
            glow.type = Image.Type.Sliced;
            SetRect(glow.rectTransform, new Vector2(-0.035f, -0.035f), new Vector2(1.035f, 1.035f), Vector2.zero, Vector2.zero);

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = builtinSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, -3f);

            TMP_Text symbolText = CreateText(rect, symbol, symbol.Length > 2 ? 44f : 68f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(symbolText.rectTransform, new Vector2(0f, 0.38f), new Vector2(1f, 0.94f), Vector2.zero, Vector2.zero);
            EnableAutoSize(symbolText, 30f, symbol.Length > 2 ? 44f : 68f);

            TMP_Text labelText = CreateText(rect, label, 25f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.20f), new Vector2(1f, 0.43f), Vector2.zero, Vector2.zero);

            TMP_Text hintText = CreateText(rect, keyHint, 18f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.78f));
            SetRect(hintText.rectTransform, new Vector2(0f, 0.025f), new Vector2(1f, 0.22f), Vector2.zero, Vector2.zero);
            EnableAutoSize(hintText, 13f, 18f);
        }

        private static void CreateNeonSideBars(RectTransform root)
        {
            RectTransform left = CreatePanel(root, "Left Neon", new Vector2(0f, 0f), new Vector2(0.018f, 1f), Vector2.zero, Vector2.zero, Pink, false);
            RectTransform right = CreatePanel(root, "Right Neon", new Vector2(0.982f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Cyan, false);
            left.GetComponent<Image>().raycastTarget = false;
            right.GetComponent<Image>().raycastTarget = false;
        }

        private static Button CreateButton(RectTransform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreatePanel(parent, label + " Button", anchorMin, anchorMax, Vector2.zero, Vector2.zero, color, true);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText(rect, label, 24f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, 8f, 8f, 3f, 3f);
            EnableAutoSize(text, 15f, 24f);
            return button;
        }

        private static ScrollRect CreateScrollView(RectTransform parent, out RectTransform content)
        {
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(parent, false);
            RectTransform viewport = (RectTransform)viewportObject.transform;
            Stretch(viewport, 12f, 24f, 12f, 12f);
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.sprite = builtinSprite;
            viewportImage.color = new Color(0.01f, 0.025f, 0.08f, 0.95f);
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static T CreateSystem<T>(Transform parent, string name) where T : Component
        {
            GameObject systemObject = new GameObject(name);
            systemObject.transform.SetParent(parent, false);
            return systemObject.AddComponent<T>();
        }

        private static void AddTextShadow(TMP_Text text, Color color, Vector2 distance)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void EnableAutoSize(TMP_Text text, float minimum, float maximum)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
        }

        private static void SetOutlineColor(RectTransform rect, Color color)
        {
            if (rect == null)
            {
                return;
            }

            Outline outline = rect.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = color;
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
#endif
