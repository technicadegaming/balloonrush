#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// Validates the generated cabinet project before play-testing or building.
    /// Reaching this menu already proves that scripts compiled; these checks focus
    /// on generated assets, scene order, payout safety, and production settings.
    /// </summary>
    public static class BalloonRushPreflightValidator
    {
        private const string Root = "Assets/BalloonRush";
        private const string ResourcesRoot = Root + "/Resources";
        private const string ConfigPath = ResourcesRoot + "/BalloonRushConfig.asset";
        private const string PayoutPath = ResourcesRoot + "/PayoutConfig.asset";
        private const string DifficultyPath = ResourcesRoot + "/DifficultyConfig.asset";
        private const string AudioPath = ResourcesRoot + "/AudioConfig.asset";
        private const string BalloonPrefabPath = Root + "/Prefabs/Balloon.prefab";
        private const string FloatingTextPrefabPath = Root + "/Prefabs/FloatingText.prefab";

        private static readonly string[] RequiredScenes =
        {
            Root + "/Scenes/Boot.unity",
            Root + "/Scenes/AttractMode.unity",
            Root + "/Scenes/MainGame.unity",
            Root + "/Scenes/Results.unity",
            Root + "/Scenes/OperatorMenu.unity"
        };

        [MenuItem("Tools/Balloon Rush/Validate Generated Project", priority = 3)]
        private static void ValidateFromMenu()
        {
            ValidationReport report = Validate();
            LogReport(report);

            if (!Application.isBatchMode)
            {
                string title = report.IsValid ? "Balloon Rush Validation Passed" : "Balloon Rush Validation Failed";
                string message = report.IsValid
                    ? $"Preflight passed with {report.Warnings.Count} warning(s). Review the Console, then run Unity tests and the cabinet checklist."
                    : $"Preflight found {report.Errors.Count} error(s) and {report.Warnings.Count} warning(s). Review the Console before building.";
                EditorUtility.DisplayDialog(title, message, "Close");
            }
        }

        public static void ValidateOrThrow()
        {
            ValidationReport report = Validate();
            LogReport(report);
            if (!report.IsValid)
            {
                throw new BuildFailedException($"Balloon Rush preflight failed with {report.Errors.Count} error(s). See the Console for details.");
            }
        }

        public static ValidationReport Validate()
        {
            ValidationReport report = new ValidationReport();
            ValidateAssets(report);
            ValidateScenes(report);
            ValidatePayoutSafety(report);
            ValidateBalloonDefinitions(report);
            ValidatePlayerSettings(report);
            return report;
        }

        private static void ValidateAssets(ValidationReport report)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            PayoutConfig payout = AssetDatabase.LoadAssetAtPath<PayoutConfig>(PayoutPath);
            DifficultyConfig difficulty = AssetDatabase.LoadAssetAtPath<DifficultyConfig>(DifficultyPath);
            AudioConfig audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioPath);

            Require(config != null, ConfigPath, report);
            Require(payout != null, PayoutPath, report);
            Require(difficulty != null, DifficultyPath, report);
            Require(audio != null, AudioPath, report);
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(BalloonPrefabPath) != null, BalloonPrefabPath, report);
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(FloatingTextPrefabPath) != null, FloatingTextPrefabPath, report);

            if (config == null)
            {
                return;
            }

            if (config.payoutConfig == null) report.Errors.Add("BalloonRushConfig has no PayoutConfig reference.");
            if (config.difficultyConfig == null) report.Errors.Add("BalloonRushConfig has no DifficultyConfig reference.");
            if (config.audioConfig == null) report.Errors.Add("BalloonRushConfig has no AudioConfig reference.");
            if (config.targetWidth >= config.targetHeight) report.Errors.Add("Target resolution is not portrait.");
            if (config.targetWidth < 480 || config.targetHeight < 800) report.Warnings.Add("Target resolution is unusually small for the portrait cabinet UI.");
            if (config.targetFrameRate < 60) report.Warnings.Add("Target frame rate is below 60 FPS.");
            if (config.balloonPoolSize < 24) report.Warnings.Add("Balloon pool is small and may exhaust during Rush or Golden Round.");
            if (config.floatingTextPoolSize < 12) report.Warnings.Add("Floating-text pool is small and may recycle effects aggressively.");
            if (string.IsNullOrWhiteSpace(config.buildVersion)) report.Warnings.Add("Build version is blank; session audit rows will be harder to reconcile.");

            if (TMP_Settings.defaultFontAsset == null)
            {
                report.Warnings.Add("TextMesh Pro has no default font asset. Import TMP Essential Resources before production use.");
            }
        }

        private static void ValidateScenes(ValidationReport report)
        {
            for (int i = 0; i < RequiredScenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RequiredScenes[i]) == null)
                {
                    report.Errors.Add("Missing generated scene: " + RequiredScenes[i]);
                }
            }

            EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
            if (configured == null || configured.Length < RequiredScenes.Length)
            {
                report.Errors.Add("Build Settings does not contain all five Balloon Rush scenes.");
                return;
            }

            for (int i = 0; i < RequiredScenes.Length; i++)
            {
                if (i >= configured.Length || configured[i] == null)
                {
                    report.Errors.Add($"Build Settings scene slot {i} is missing.");
                    continue;
                }

                if (!configured[i].enabled)
                {
                    report.Errors.Add("Disabled required scene: " + configured[i].path);
                }

                if (!string.Equals(configured[i].path, RequiredScenes[i], StringComparison.Ordinal))
                {
                    report.Errors.Add($"Build Settings scene {i} should be '{RequiredScenes[i]}' but is '{configured[i].path}'.");
                }
            }
        }

        private static void ValidatePayoutSafety(ValidationReport report)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            PayoutConfig payout = AssetDatabase.LoadAssetAtPath<PayoutConfig>(PayoutPath);
            OperatorSettings defaults = config != null ? config.CreateDefaultSettings() : new OperatorSettings();
            defaults.Validate();

            if (defaults.pricePerPlayCents != 100) report.Errors.Add("Default card-swipe play price must be 100 cents ($1.00).");
            if (defaults.cardSwipeValue != 1 || defaults.creditsPerPlay != 1) report.Errors.Add("Default setup must grant exactly one playable credit per card swipe.");
            if (defaults.minimumTicketPayout < 1) report.Warnings.Add("Default minimum payout is zero; a paid player can leave without tickets.");
            if (defaults.regularTicketCap > 125) report.Warnings.Add("Default regular-ticket cap exceeds the tested $1 commercial profile of 125.");
            if (defaults.jackpotTickets > 500) report.Errors.Add("Default jackpot exceeds the 500-ticket cabinet limit.");
            if (defaults.maxTicketPayout > 1000) report.Errors.Add("Default total payout exceeds the 1,000-ticket cabinet limit.");
            if (defaults.maxTicketPayout < defaults.jackpotTickets + defaults.regularTicketCap) report.Errors.Add("Maximum payout cannot preserve both the configured jackpot and regular-ticket cap.");
            int targetAverageTickets = EconomyMath.CalculateTargetAverageTickets(defaults);
            if (targetAverageTickets < defaults.minimumTicketPayout) report.Errors.Add("Configured prize-cost target is lower than the guaranteed minimum payout.");
            if (defaults.estimatedPrizeCostPerTicketCents <= 0f) report.Errors.Add("Estimated prize cost per ticket must be greater than zero.");
            if (defaults.targetPrizeCostPercent > 35f) report.Warnings.Add("Default target prize-cost percentage is above 35%; verify venue labor and overhead before deployment.");
            if (defaults.maxTicketPayout < defaults.jackpotTickets) report.Errors.Add("Maximum payout is lower than the jackpot.");
            if (defaults.goldenGreatReward > defaults.jackpotTickets) report.Errors.Add("Golden GREAT reward exceeds the jackpot.");
            if (defaults.goldenGoodReward > defaults.goldenGreatReward) report.Errors.Add("Golden GOOD reward exceeds Golden GREAT.");
            if (defaults.goldenMissReward > defaults.goldenGoodReward) report.Errors.Add("Golden MISS reward exceeds Golden GOOD.");
            if (defaults.perfectWindow > defaults.greatWindow || defaults.greatWindow > defaults.goodWindow)
            {
                report.Errors.Add("Timing windows are not ordered Perfect <= Great <= Good.");
            }

            if (defaults.goodTicketMultiplier > defaults.greatTicketMultiplier || defaults.greatTicketMultiplier > defaults.perfectTicketMultiplier)
            {
                report.Errors.Add("Timing ticket multipliers are not ordered GOOD <= GREAT <= PERFECT.");
            }

            if (defaults.combo5Multiplier > defaults.combo10Multiplier ||
                defaults.combo10Multiplier > defaults.combo15Multiplier ||
                defaults.combo15Multiplier > defaults.combo20Multiplier ||
                defaults.combo20Multiplier > defaults.combo30Multiplier)
            {
                report.Errors.Add("Combo ticket multipliers must be non-decreasing from combo 5 through combo 30.");
            }

            if (defaults.greenSpawnWeight <= 0f)
            {
                report.Errors.Add("Green balloon spawn weight must be greater than zero.");
            }

            if (defaults.blueSpawnWeight < 0f || defaults.bombSpawnWeight < 0f || defaults.superBombSpawnWeight < 0f ||
                defaults.goldenSpawnWeight < 0f || defaults.mysterySpawnWeight < 0f || defaults.multiplierSpawnWeight < 0f)
            {
                report.Errors.Add("Balloon spawn weights cannot be negative.");
            }

            if (defaults.inputDebounceMilliseconds < 0 || defaults.inputDebounceMilliseconds > 250)
            {
                report.Errors.Add("Input debounce must remain between 0 and 250 milliseconds.");
            }

            if (payout == null || payout.visibleTiers == null || payout.visibleTiers.Length == 0)
            {
                report.Errors.Add("Payout ladder has no visible tiers.");
                return;
            }

            int previous = int.MaxValue;
            bool hasOne = false;
            bool hasJackpot = false;
            for (int i = 0; i < payout.visibleTiers.Length; i++)
            {
                int tier = payout.visibleTiers[i];
                if (tier <= 0) report.Errors.Add("Payout ladder contains a non-positive tier.");
                if (tier > previous) report.Warnings.Add("Payout ladder is not arranged from highest to lowest.");
                previous = tier;
                hasOne |= tier == 1;
                hasJackpot |= tier == defaults.jackpotTickets;
            }

            if (!hasOne) report.Warnings.Add("Payout ladder does not show the 1-ticket starting tier.");
            if (!hasJackpot) report.Warnings.Add("Payout ladder does not show the configured jackpot value.");
        }

        private static void ValidateBalloonDefinitions(ValidationReport report)
        {
            string[] guids = AssetDatabase.FindAssets("t:BalloonDefinition", new[] { ResourcesRoot + "/BalloonDefinitions" });
            Dictionary<BalloonKind, int> counts = new Dictionary<BalloonKind, int>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BalloonDefinition definition = AssetDatabase.LoadAssetAtPath<BalloonDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                counts.TryGetValue(definition.Kind, out int count);
                counts[definition.Kind] = count + 1;
                if (string.IsNullOrWhiteSpace(definition.Id)) report.Errors.Add("Balloon definition has a blank ID: " + path);
                if (string.IsNullOrWhiteSpace(definition.DisplayName)) report.Warnings.Add("Balloon definition has a blank display name: " + path);
                if (definition.SpawnWeight < 0f) report.Errors.Add("Balloon definition has a negative spawn weight: " + path);
            }

            BalloonKind[] required =
            {
                BalloonKind.Green,
                BalloonKind.Blue,
                BalloonKind.Multiplier,
                BalloonKind.Mystery,
                BalloonKind.Bomb,
                BalloonKind.SuperBomb,
                BalloonKind.GoldenTrigger,
                BalloonKind.GoldenJackpot
            };

            for (int i = 0; i < required.Length; i++)
            {
                counts.TryGetValue(required[i], out int count);
                if (count == 0) report.Errors.Add("Missing BalloonDefinition for " + required[i] + ".");
                if (count > 1) report.Warnings.Add($"Multiple BalloonDefinitions exist for {required[i]} ({count}).");
            }
        }

        private static void ValidatePlayerSettings(ValidationReport report)
        {
            if (PlayerSettings.defaultScreenWidth >= PlayerSettings.defaultScreenHeight)
            {
                report.Errors.Add("Player Settings default resolution is not portrait.");
            }

            if (!PlayerSettings.runInBackground)
            {
                report.Warnings.Add("Run In Background is disabled; cabinet I/O may pause if Windows changes focus.");
            }

            if (PlayerSettings.resizableWindow)
            {
                report.Warnings.Add("Resizable Window is enabled; cabinet users could alter the presentation.");
            }

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                report.Warnings.Add("Linear color space is recommended for the neon presentation.");
            }
        }

        private static void Require(bool condition, string path, ValidationReport report)
        {
            if (!condition)
            {
                report.Errors.Add("Missing required asset: " + path);
            }
        }

        private static void LogReport(ValidationReport report)
        {
            for (int i = 0; i < report.Errors.Count; i++)
            {
                Debug.LogError("[Balloon Rush Preflight] " + report.Errors[i]);
            }

            for (int i = 0; i < report.Warnings.Count; i++)
            {
                Debug.LogWarning("[Balloon Rush Preflight] " + report.Warnings[i]);
            }

            if (report.IsValid)
            {
                Debug.Log($"[Balloon Rush Preflight] PASSED with {report.Warnings.Count} warning(s).");
            }
        }

        public sealed class ValidationReport
        {
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public bool IsValid => Errors.Count == 0;
        }
    }

    /// <summary>
    /// Prevents cabinet builds made outside the custom menu from bypassing the
    /// same asset, scene-order, portrait, and payout-safety checks.
    /// </summary>
    public sealed class BalloonRushBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            BalloonRushPreflightValidator.ValidateOrThrow();
        }
    }
}
#endif
