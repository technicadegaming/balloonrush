#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    /// <summary>
    /// Balloon Rush v1.9.0 cabinet-final installer.
    ///
    /// Runtime presentation is additive. This editor step permanently changes
    /// the source default for passedBalloonBreaksCombo to false so future
    /// resets/new installs also use the cabinet-final behavior.
    /// </summary>
    public static class BalloonRushV190CabinetFinalInstaller
    {
        private const string SettingsPath =
            "Assets/BalloonRush/Scripts/SaveSystem/OperatorSettings.cs";

        private const string RuntimePath =
            "Assets/BalloonRush/Scripts/UI/BalloonRushCabinetFinalV190.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.9.0 CABINET FINAL",
            priority = -40)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.9.0: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(RuntimePath))
            {
                Debug.LogError(
                    "Balloon Rush v1.9.0: runtime file is missing. " +
                    "Merge the patch Assets folder first.");
                return;
            }

            if (!File.Exists(SettingsPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.9.0: OperatorSettings.cs was not found.");
                return;
            }

            string backup =
                CreateBackupFolder();

            File.Copy(
                SettingsPath,
                Path.Combine(
                    backup,
                    "OperatorSettings.cs"),
                true);

            string source =
                Normalize(
                    File.ReadAllText(
                        SettingsPath));

            string original = source;

            source = source.Replace(
                "public bool passedBalloonBreaksCombo = true;",
                "public bool passedBalloonBreaksCombo = false;");

            source = source.Replace(
                "passedBalloonBreaksCombo = true;",
                "passedBalloonBreaksCombo = false;");

            if (source != original)
            {
                WriteText(
                    SettingsPath,
                    source);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.9.0 CABINET FINAL APPLIED\n\n" +
                "Permanent source default:\n" +
                "  Passed reward balloons break combo = OFF\n\n" +
                "Runtime final polish installed:\n" +
                "  - less-obstructive hit zone\n" +
                "  - shorter rating feedback\n" +
                "  - larger live meter copy\n" +
                "  - calmer lane/background scanners\n" +
                "  - clearer Results stats\n\n" +
                "Backup:\n" +
                backup);
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.9.0 CABINET FINAL",
            priority = -39)]
        public static void Verify()
        {
            bool runtimeExists =
                File.Exists(RuntimePath);

            bool settingsExist =
                File.Exists(SettingsPath);

            bool comboDefaultOff = false;

            if (settingsExist)
            {
                string text =
                    File.ReadAllText(
                        SettingsPath);

                comboDefaultOff =
                    text.Contains(
                        "public bool passedBalloonBreaksCombo = false;") &&
                    !text.Contains(
                        "passedBalloonBreaksCombo = true;");
            }

            bool liveMeters =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushLiveMetersV189.cs");

            bool hitPop =
                File.Exists(
                    "Assets/BalloonRush/Scripts/UI/" +
                    "BalloonRushHitPopV187.cs");

            if (runtimeExists &&
                comboDefaultOff &&
                liveMeters &&
                hitPop)
            {
                Debug.Log(
                    "Balloon Rush v1.9.0 VERIFY PASS:\n" +
                    "- cabinet-final runtime installed\n" +
                    "- ignored reward balloons do not break combo by default\n" +
                    "- v1.8.9 live meters present\n" +
                    "- v1.8.7 balloon-body pop present");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.9.0 VERIFY\n" +
                    "Final runtime: " +
                    runtimeExists +
                    "\nCombo default OFF: " +
                    comboDefaultOff +
                    "\nLive meters: " +
                    liveMeters +
                    "\nBalloon-body pop: " +
                    hitPop);
            }
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
                    "BalloonRush_v1.9.0_FINAL_" +
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
            string text)
        {
            File.WriteAllText(
                path,
                text.Replace(
                    "\n",
                    Environment.NewLine),
                new UTF8Encoding(false));
        }
    }
}
#endif
