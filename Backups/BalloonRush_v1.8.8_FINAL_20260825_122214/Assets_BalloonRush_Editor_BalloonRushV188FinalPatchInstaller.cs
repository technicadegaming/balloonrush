#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// Balloon Rush v1.8.8 final easy patch installer.
    ///
    /// The runtime final-polish script fixes the current scene immediately.
    /// This installer also updates OperatorMenuManager.cs so the improved
    /// layout remains correct after future scene rebuilds.
    /// </summary>
    public static class BalloonRushV188FinalPatchInstaller
    {
        private const string OperatorPath =
            "Assets/BalloonRush/Scripts/UI/OperatorMenuManager.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.8.8 FINAL Polish",
            priority = -44)]
        public static void ApplyPatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.8: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(OperatorPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.8.8: OperatorMenuManager.cs was not found.");
                return;
            }

            string backup =
                CreateBackupFolder();

            File.Copy(
                OperatorPath,
                Path.Combine(
                    backup,
                    "OperatorMenuManager.cs"),
                true);

            int operatorChanges =
                PatchOperatorManager();

            int warningChanges =
                CleanAllTmpWrappingWarnings(
                    backup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.8.8 FINAL POLISH APPLIED\n" +
                "Operator layout edits: " +
                operatorChanges +
                "\nTMP obsolete-property cleanups: " +
                warningChanges +
                "\nBackup folder: " +
                backup +
                "\n\n" +
                "Let Unity compile, then test OperatorMenu, AttractMode, " +
                "MainGame and Results.");
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.8.8 FINAL Polish",
            priority = -43)]
        public static void VerifyPatch()
        {
            bool finalRuntime =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushFinalPolishV188.cs");

            bool v186Consolidated =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushMissPopPolishV186.cs") &&
                !File.ReadAllText(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushMissPopPolishV186.cs")
                    .Contains(
                        "SpawnWorldPopBurst(");

            string operatorText =
                File.Exists(OperatorPath)
                    ? File.ReadAllText(
                        OperatorPath)
                    : string.Empty;

            bool readableOperator =
                operatorText.Contains(
                    "labelText.fontSizeMin = 12f;") &&
                operatorText.Contains(
                    "inputLayout.preferredWidth = 170f;") &&
                operatorText.Contains(
                    "labelText.textWrappingMode = TextWrappingModes.Normal;");

            int obsoleteCount =
                CountObsoleteWrappingReferences();

            if (finalRuntime &&
                v186Consolidated &&
                readableOperator &&
                obsoleteCount == 0)
            {
                Debug.Log(
                    "Balloon Rush v1.8.8 FINAL VERIFY PASS:\n" +
                    "- Operator labels wrap/read correctly\n" +
                    "- value controls use less horizontal space\n" +
                    "- duplicate older pop burst removed\n" +
                    "- no enableWordWrapping references remain\n" +
                    "- final presentation runtime is installed");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.8.8 FINAL VERIFY\n" +
                    "Final runtime: " +
                    finalRuntime +
                    "\nConsolidated pop feedback: " +
                    v186Consolidated +
                    "\nReadable Operator source: " +
                    readableOperator +
                    "\nRemaining enableWordWrapping references: " +
                    obsoleteCount);
            }
        }

        private static int PatchOperatorManager()
        {
            string text =
                Normalize(
                    File.ReadAllText(
                        OperatorPath));

            string original = text;

            // Row height: allow long labels to use two lines.
            text = text.Replace(
                "GameObject row = CreateRow(label, 62f);",
                "GameObject row = CreateRow(label, 76f);");

            // Input-row spacing.
            text = text.Replace(
                "layout.padding = new RectOffset(16, 16, 7, 7);",
                "layout.padding = new RectOffset(12, 12, 7, 7);");

            text = text.Replace(
                "layout.spacing = 14f;",
                "layout.spacing = 10f;");

            // Toggle-row spacing.
            text = text.Replace(
                "layout.padding = new RectOffset(16, 20, 7, 7);",
                "layout.padding = new RectOffset(12, 12, 7, 7);");

            // Stop controls from expanding over the label.
            string widthAnchor =
                "layout.childControlWidth = true;";

            string widthReplacement =
                "layout.childControlWidth = true;\n" +
                "            layout.childForceExpandWidth = false;\n" +
                "            layout.childForceExpandHeight = false;";

            if (!text.Contains(
                    "layout.childForceExpandWidth = false;"))
            {
                text = text.Replace(
                    widthAnchor,
                    widthReplacement);
            }

            // Both field types use this same label block.
            text = text.Replace(
                "labelText.fontSizeMin = 17f;",
                "labelText.fontSizeMin = 12f;");

            text = text.Replace(
                "labelText.fontSizeMax = 22f;",
                "labelText.fontSizeMax = 20f;\n" +
                "            labelText.textWrappingMode = TextWrappingModes.Normal;\n" +
                "            labelText.overflowMode = TextOverflowModes.Overflow;");

            text = text.Replace(
                "labelLayout.minWidth = 350f;",
                "labelLayout.minWidth = 0f;\n" +
                "            labelLayout.preferredWidth = 0f;");

            // Give values enough room without consuming a third of the row.
            text = text.Replace(
                "inputLayout.preferredWidth = 220f;",
                "inputLayout.preferredWidth = 170f;");

            text = text.Replace(
                "inputLayout.minWidth = 200f;",
                "inputLayout.minWidth = 145f;\n" +
                "            inputLayout.flexibleWidth = 0f;");

            text = text.Replace(
                "toggleLayout.preferredWidth = 80f;",
                "toggleLayout.preferredWidth = 72f;\n" +
                "            toggleLayout.minWidth = 68f;\n" +
                "            toggleLayout.flexibleWidth = 0f;");

            // Current TMP API.
            text = text.Replace(
                ".enableWordWrapping = false;",
                ".textWrappingMode = TextWrappingModes.NoWrap;");

            text = text.Replace(
                ".enableWordWrapping = true;",
                ".textWrappingMode = TextWrappingModes.Normal;");

            if (text == original)
            {
                Debug.Log(
                    "Balloon Rush v1.8.8: OperatorMenuManager already appears " +
                    "to contain the final layout values.");
                return 0;
            }

            WriteText(
                OperatorPath,
                text);

            return 1;
        }

        private static int CleanAllTmpWrappingWarnings(
            string backupRoot)
        {
            string scriptsRoot =
                Path.Combine(
                    Application.dataPath,
                    "BalloonRush");

            if (!Directory.Exists(
                    scriptsRoot))
            {
                return 0;
            }

            string[] files =
                Directory.GetFiles(
                    scriptsRoot,
                    "*.cs",
                    SearchOption.AllDirectories);

            int changedFiles = 0;

            foreach (string absolutePath in files)
            {
                string text =
                    File.ReadAllText(
                        absolutePath);

                if (!text.Contains(
                        "enableWordWrapping"))
                {
                    continue;
                }

                string relative =
                    "Assets" +
                    absolutePath.Substring(
                        Application.dataPath.Length)
                        .Replace(
                            '\\',
                            '/');

                string relativeBackup =
                    relative.Replace(
                        '/',
                        '_');

                File.Copy(
                    absolutePath,
                    Path.Combine(
                        backupRoot,
                        relativeBackup),
                    true);

                string updated =
                    text.Replace(
                        ".enableWordWrapping = false;",
                        ".textWrappingMode = TextWrappingModes.NoWrap;")
                    .Replace(
                        ".enableWordWrapping = true;",
                        ".textWrappingMode = TextWrappingModes.Normal;");

                if (updated != text)
                {
                    File.WriteAllText(
                        absolutePath,
                        updated,
                        new UTF8Encoding(false));

                    changedFiles++;
                }
            }

            return changedFiles;
        }

        private static int CountObsoleteWrappingReferences()
        {
            string scriptsRoot =
                Path.Combine(
                    Application.dataPath,
                    "BalloonRush");

            if (!Directory.Exists(
                    scriptsRoot))
            {
                return -1;
            }

            string[] files =
                Directory.GetFiles(
                    scriptsRoot,
                    "*.cs",
                    SearchOption.AllDirectories);

            int count = 0;

            foreach (string file in files)
            {
                string text =
                    File.ReadAllText(file);

                int index = 0;

                while (true)
                {
                    index =
                        text.IndexOf(
                            "enableWordWrapping",
                            index,
                            StringComparison.Ordinal);

                    if (index < 0)
                        break;

                    count++;
                    index += 4;
                }
            }

            return count;
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
                    "BalloonRush_v1.8.8_FINAL_" +
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
