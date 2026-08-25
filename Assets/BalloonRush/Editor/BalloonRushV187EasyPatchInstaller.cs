#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// v1.8.7 easy patch installer.
    ///
    /// New runtime visual scripts auto-install after the Assets folder is
    /// merged. This menu command cleans the TextMeshPro obsolete warnings in
    /// BalloonRushProjectBuilder.cs.
    /// </summary>
    public static class BalloonRushV187EasyPatchInstaller
    {
        private const string BuilderPath =
            "Assets/BalloonRush/Editor/BalloonRushProjectBuilder.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.8.7 Attract + Pop + Warning Cleanup",
            priority = -46)]
        public static void ApplyPatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.7: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(BuilderPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.8.7: BalloonRushProjectBuilder.cs " +
                    "was not found at the expected path.");
                return;
            }

            string backupFolder =
                CreateBackupFolder();

            File.Copy(
                BuilderPath,
                Path.Combine(
                    backupFolder,
                    "BalloonRushProjectBuilder.cs"),
                true);

            int warningFixes =
                FixWordWrappingWarnings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.8.7 PATCH APPLIED\n" +
                "TMP obsolete word-wrapping assignments fixed: " +
                warningFixes +
                "\nBackup: " +
                backupFolder +
                "\n\n" +
                "Attract screen and hit-pop visuals auto-install at runtime. " +
                "Open AttractMode or start from Boot to test.");
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.8.7 Attract + Pop",
            priority = -45)]
        public static void VerifyPatch()
        {
            bool attractExists =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushAttractVisualV187.cs");

            bool popExists =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushHitPopV187.cs");

            string builder =
                File.Exists(BuilderPath)
                    ? File.ReadAllText(BuilderPath)
                    : string.Empty;

            bool obsoleteWrappingGone =
                !builder.Contains(
                    "enableWordWrapping");

            if (attractExists &&
                popExists &&
                obsoleteWrappingGone)
            {
                Debug.Log(
                    "Balloon Rush v1.8.7 VERIFY PASS: " +
                    "new Attract presentation installed, hit-pop visual installed, " +
                    "and old TMP enableWordWrapping warnings removed.");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.8.7 VERIFY\n" +
                    "Attract visual: " +
                    attractExists +
                    "\nHit-pop visual: " +
                    popExists +
                    "\nOld enableWordWrapping references removed: " +
                    obsoleteWrappingGone);
            }
        }

        private static int FixWordWrappingWarnings()
        {
            string text =
                Normalize(
                    File.ReadAllText(
                        BuilderPath));

            int falseCount =
                Regex.Matches(
                    text,
                    @"\.enableWordWrapping\s*=\s*false\s*;")
                .Count;

            int trueCount =
                Regex.Matches(
                    text,
                    @"\.enableWordWrapping\s*=\s*true\s*;")
                .Count;

            text = Regex.Replace(
                text,
                @"\.enableWordWrapping\s*=\s*false\s*;",
                ".textWrappingMode = TextWrappingModes.NoWrap;");

            text = Regex.Replace(
                text,
                @"\.enableWordWrapping\s*=\s*true\s*;",
                ".textWrappingMode = TextWrappingModes.Normal;");

            WriteText(
                BuilderPath,
                text);

            return falseCount + trueCount;
        }

        private static string CreateBackupFolder()
        {
            string projectRoot =
                Directory.GetParent(
                    Application.dataPath)?.FullName ??
                Directory.GetCurrentDirectory();

            string folder =
                Path.Combine(
                    projectRoot,
                    "Backups",
                    "BalloonRush_v1.8.7_" +
                    DateTime.Now.ToString(
                        "yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string Normalize(
            string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }

        private static void WriteText(
            string path,
            string normalizedText)
        {
            File.WriteAllText(
                path,
                normalizedText.Replace(
                    "\n",
                    Environment.NewLine),
                new UTF8Encoding(false));
        }
    }
}
#endif
