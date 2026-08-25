#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// v1.9.2 permanent viewport/source cleanup.
    ///
    /// Runtime repairs the current generated scene.
    /// This menu command makes future Build Complete Game runs use RectMask2D
    /// and removes remaining M/Operator player-facing builder text.
    /// </summary>
    public static class BalloonRushV192OperatorRectMaskInstaller
    {
        private const string BuilderPath =
            "Assets/BalloonRush/Editor/BalloonRushProjectBuilder.cs";

        private const string MainVisualPath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushMainGameVisualRebuild.cs";

        private const string RuntimePath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushOperatorRectMaskFixV192.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.9.2 OPERATOR RECT MASK FIX",
            priority = -36)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.9.2: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(RuntimePath))
            {
                Debug.LogError(
                    "Balloon Rush v1.9.2 runtime file is missing. " +
                    "Merge the patch Assets folder first.");
                return;
            }

            string backup =
                CreateBackupFolder();

            int changed = 0;

            if (File.Exists(BuilderPath))
            {
                Backup(
                    BuilderPath,
                    backup);

                string text =
                    Normalize(
                        File.ReadAllText(
                            BuilderPath));

                string original = text;

                // Permanent viewport fix for future generated Operator scenes.
                text = text.Replace(
                    "Mask mask = viewportObject.AddComponent<Mask>();\n            mask.showMaskGraphic = true;",
                    "RectMask2D mask = viewportObject.AddComponent<RectMask2D>();");

                // Remove remaining player-facing Operator instructions.
                text = text.Replace(
                    "LEFT/RIGHT SELECT   UP/SPACE POPS   M OPERATOR",
                    "LEFT/RIGHT SELECT   UP/SPACE POPS");

                text = text.Replace(
                    "M = OPERATOR MENU     ESC = DEBUG / SERVICE PANEL",
                    "ESC = SERVICE / DEBUG");

                text = text.Replace(
                    "ESC CLOSES PANEL   |   M OPENS OPERATOR SETTINGS",
                    "ESC CLOSES PANEL");

                text = text.Replace(
                    "C = CREDIT     M = OPERATOR MENU",
                    "C = CREDIT");

                text = text.Replace(
                    "M OR ESC = RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE",
                    "ESC = RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE");

                // Current TMP API while we are touching the builder.
                text = text.Replace(
                    ".enableWordWrapping = false;",
                    ".textWrappingMode = TextWrappingModes.NoWrap;");

                text = text.Replace(
                    ".enableWordWrapping = true;",
                    ".textWrappingMode = TextWrappingModes.Normal;");

                if (text != original)
                {
                    WriteText(
                        BuilderPath,
                        text);

                    changed++;
                }
            }

            if (File.Exists(MainVisualPath))
            {
                Backup(
                    MainVisualPath,
                    backup);

                string text =
                    Normalize(
                        File.ReadAllText(
                            MainVisualPath));

                string original = text;

                text = text.Replace(
                    "\"M = OPERATOR     ESC = SERVICE / DEBUG\"",
                    "string.Empty");

                if (text != original)
                {
                    WriteText(
                        MainVisualPath,
                        text);

                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.9.2 APPLIED\n\n" +
                "Key fix: Operator viewport now uses a RECTANGULAR mask " +
                "instead of clipping settings through the rounded viewport sprite.\n\n" +
                "Source files changed: " +
                changed +
                "\nBackup:\n" +
                backup +
                "\n\n" +
                "No scene rebuild is required.");
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.9.2 OPERATOR RECT MASK FIX",
            priority = -35)]
        public static void Verify()
        {
            bool runtime =
                File.Exists(RuntimePath);

            bool builderMask = true;
            bool hintsRemoved = true;

            if (File.Exists(BuilderPath))
            {
                string text =
                    File.ReadAllText(
                        BuilderPath);

                builderMask =
                    text.Contains(
                        "RectMask2D mask = viewportObject.AddComponent<RectMask2D>()") &&
                    !text.Contains(
                        "viewportObject.AddComponent<Mask>()");

                hintsRemoved =
                    !text.Contains("M OPERATOR") &&
                    !text.Contains("M = OPERATOR MENU") &&
                    !text.Contains("M OPENS OPERATOR SETTINGS");
            }

            if (runtime &&
                builderMask &&
                hintsRemoved)
            {
                Debug.Log(
                    "Balloon Rush v1.9.2 VERIFY PASS:\n" +
                    "- rectangular Operator scroll mask installed\n" +
                    "- rounded-mask text clipping removed\n" +
                    "- remaining public M/Operator instructions removed\n" +
                    "- runtime current-scene repair installed");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.9.2 VERIFY\n" +
                    "Runtime: " + runtime +
                    "\nFuture builder RectMask2D: " + builderMask +
                    "\nOperator hints removed: " + hintsRemoved);
            }
        }

        private static void Backup(
            string path,
            string backupFolder)
        {
            if (!File.Exists(path))
                return;

            string safe =
                path.Replace("/", "_")
                    .Replace("\\", "_");

            File.Copy(
                path,
                Path.Combine(
                    backupFolder,
                    safe),
                true);
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
                    "BalloonRush_v1.9.2_MASK_FIX_" +
                    DateTime.Now.ToString(
                        "yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(
                folder);

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
            string normalized)
        {
            File.WriteAllText(
                path,
                normalized.Replace(
                    "\n",
                    Environment.NewLine),
                new UTF8Encoding(false));
        }
    }
}
#endif
