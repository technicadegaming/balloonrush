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
    /// v1.9.1 permanent source patch.
    ///
    /// Runtime fixes the already-built scenes immediately.
    /// This editor command also updates the source builders so future scene
    /// regeneration preserves the same clean layout and hidden service hints.
    /// </summary>
    public static class BalloonRushV191OperatorFinalInstaller
    {
        private const string OperatorPath =
            "Assets/BalloonRush/Scripts/UI/OperatorMenuManager.cs";

        private const string MainVisualPath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushMainGameVisualRebuild.cs";

        private const string BuilderPath =
            "Assets/BalloonRush/Editor/BalloonRushProjectBuilder.cs";

        private const string RuntimePath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushOperatorMenuFinalFixV191.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.9.1 OPERATOR MENU FINAL FIX",
            priority = -38)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.9.1: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(RuntimePath) ||
                !File.Exists(OperatorPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.9.1: patch files or OperatorMenuManager.cs missing.");
                return;
            }

            string backup =
                CreateBackupFolder();

            BackupIfExists(
                OperatorPath,
                backup);

            BackupIfExists(
                MainVisualPath,
                backup);

            BackupIfExists(
                BuilderPath,
                backup);

            int operatorChanges =
                PatchOperatorManager();

            int hintChanges =
                PatchPlayerFacingHints();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.9.1 OPERATOR FINAL FIX APPLIED\n\n" +
                "Operator source changes: " +
                operatorChanges +
                "\nPlayer-facing hint files changed: " +
                hintChanges +
                "\n\n" +
                "The current Operator scene does NOT need rebuilding. " +
                "The runtime component fixes the existing generated scene.\n\n" +
                "Backup:\n" +
                backup);
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.9.1 OPERATOR MENU FINAL FIX",
            priority = -37)]
        public static void Verify()
        {
            bool runtime =
                File.Exists(RuntimePath);

            bool operatorSource = false;

            if (File.Exists(OperatorPath))
            {
                string text =
                    File.ReadAllText(OperatorPath);

                operatorSource =
                    text.Contains(
                        "GameObject row = CreateRow(label, 108f);") &&
                    text.Contains(
                        "labelText.textWrappingMode = TextWrappingModes.Normal;") &&
                    !text.Contains(
                        "AddInfoRow(\"KEYBOARD\"");
            }

            bool mainHintGone = true;

            if (File.Exists(MainVisualPath))
            {
                string text =
                    File.ReadAllText(MainVisualPath);

                mainHintGone =
                    !text.Contains(
                        "M = OPERATOR     ESC = SERVICE / DEBUG");
            }

            if (runtime &&
                operatorSource &&
                mainHintGone)
            {
                Debug.Log(
                    "Balloon Rush v1.9.1 VERIFY PASS:\n" +
                    "- full-width stacked Operator labels installed\n" +
                    "- keyboard/operator instruction row removed\n" +
                    "- gameplay Operator key hint removed\n" +
                    "- runtime compatibility fix present");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.9.1 VERIFY\n" +
                    "Runtime: " + runtime +
                    "\nOperator stacked source: " + operatorSource +
                    "\nGameplay M hint removed: " + mainHintGone);
            }
        }

        private static int PatchOperatorManager()
        {
            string text =
                Normalize(
                    File.ReadAllText(OperatorPath));

            string original = text;

            // Remove the big keyboard instruction row from the Operator Menu.
            text = Regex.Replace(
                text,
                @"(?s)\s*AddInfoRow\(""KEYBOARD"",.*?\);\n\s*AddHeader\(""GAME AND CREDIT SETTINGS""\);",
                "\n            AddHeader(\"GAME AND CREDIT SETTINGS\");",
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            const string stackedMethods =
@"        private void AddTextFieldInternal(string label, Func<string> getter, Action<string> setter, TMP_InputField.ContentType contentType)
        {
            GameObject row = CreateRow(label, 108f);

            TMP_Text labelText = CreateText(
                row.transform,
                label,
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.025f, 0.46f);
            labelRect.anchorMax = new Vector2(0.975f, 0.965f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 14f;
            labelText.fontSizeMax = 23f;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.lineSpacing = -4f;

            TMP_InputField input =
                CreateInputField(row.transform, contentType);

            RectTransform inputRect =
                (RectTransform)input.transform;

            inputRect.anchorMin =
                new Vector2(0.54f, 0.075f);

            inputRect.anchorMax =
                new Vector2(0.965f, 0.405f);

            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            input.pointSize = 22f;

            input.onEndEdit.AddListener(
                value => setter(value));

            Action refresh =
                () => input.SetTextWithoutNotify(getter());

            rowRefreshers.Add(refresh);
            refresh();
        }

        private void AddToggleField(string label, Func<bool> getter, Action<bool> setter)
        {
            GameObject row = CreateRow(label, 108f);

            TMP_Text labelText = CreateText(
                row.transform,
                label,
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.025f, 0.46f);
            labelRect.anchorMax = new Vector2(0.975f, 0.965f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 14f;
            labelText.fontSizeMax = 23f;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.lineSpacing = -4f;

            Toggle toggle =
                CreateToggle(row.transform);

            RectTransform toggleRect =
                (RectTransform)toggle.transform;

            toggleRect.anchorMin =
                new Vector2(0.78f, 0.055f);

            toggleRect.anchorMax =
                new Vector2(0.965f, 0.415f);

            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            toggle.onValueChanged.AddListener(
                value => setter(value));

            Action refresh =
                () => toggle.SetIsOnWithoutNotify(getter());

            rowRefreshers.Add(refresh);
            refresh();
        }

        private GameObject CreateRow";

            string updated = Regex.Replace(
                text,
                @"(?s)        private void AddTextFieldInternal\(.*?\n        private GameObject CreateRow",
                stackedMethods,
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (updated != text)
                text = updated;

            // Clean any obsolete wrapping properties in this file too.
            text = text.Replace(
                ".enableWordWrapping = false;",
                ".textWrappingMode = TextWrappingModes.NoWrap;");

            text = text.Replace(
                ".enableWordWrapping = true;",
                ".textWrappingMode = TextWrappingModes.Normal;");

            if (text == original)
                return 0;

            WriteText(
                OperatorPath,
                text);

            return 1;
        }

        private static int PatchPlayerFacingHints()
        {
            int changed = 0;

            if (File.Exists(MainVisualPath))
            {
                string text =
                    File.ReadAllText(MainVisualPath);

                string original = text;

                text = text.Replace(
                    "\"M = OPERATOR     ESC = SERVICE / DEBUG\"",
                    "string.Empty");

                if (text != original)
                {
                    WriteText(
                        MainVisualPath,
                        Normalize(text));

                    changed++;
                }
            }

            if (File.Exists(BuilderPath))
            {
                string text =
                    File.ReadAllText(BuilderPath);

                string original = text;

                text = text.Replace(
                    "LEFT/RIGHT SELECT   UP/SPACE POPS   M OPERATOR",
                    "LEFT/RIGHT SELECT   UP/SPACE POPS");

                text = text.Replace(
                    "M = OPERATOR MENU     ESC = DEBUG / SERVICE PANEL",
                    "ESC = SERVICE / DEBUG");

                text = text.Replace(
                    "C = CREDIT     M = OPERATOR MENU",
                    "C = CREDIT");

                text = text.Replace(
                    "M OR ESC = RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE",
                    "ESC = RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE");

                if (text != original)
                {
                    WriteText(
                        BuilderPath,
                        Normalize(text));

                    changed++;
                }
            }

            return changed;
        }

        private static void BackupIfExists(
            string source,
            string backupFolder)
        {
            if (!File.Exists(source))
                return;

            string safeName =
                source.Replace("/", "_")
                      .Replace("\\", "_");

            File.Copy(
                source,
                Path.Combine(
                    backupFolder,
                    safeName),
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
                    "BalloonRush_v1.9.1_OPERATOR_FINAL_" +
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
