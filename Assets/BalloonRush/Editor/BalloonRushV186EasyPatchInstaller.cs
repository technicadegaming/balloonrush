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
    /// Easy v1.8.6 installer.
    ///
    /// Copy/merge Assets, let Unity compile, then run:
    /// Tools > Balloon Rush > Apply v1.8.6 Miss + Pop Fix
    /// </summary>
    public static class BalloonRushV186EasyPatchInstaller
    {
        private const string ScoreManagerPath =
            "Assets/BalloonRush/Scripts/Gameplay/ScoreManager.cs";

        private const string BalloonManagerPath =
            "Assets/BalloonRush/Scripts/Gameplay/BalloonManager.cs";

        [MenuItem(
            "Tools/Balloon Rush/Apply v1.8.6 Miss + Pop Fix",
            priority = -48)]
        public static void ApplyPatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.6: Stop Play Mode before applying.");
                return;
            }

            if (!File.Exists(ScoreManagerPath) ||
                !File.Exists(BalloonManagerPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.8.6: ScoreManager.cs or BalloonManager.cs " +
                    "was not found at the expected path.");
                return;
            }

            string backup = CreateBackupFolder();

            File.Copy(
                ScoreManagerPath,
                Path.Combine(backup, "ScoreManager.cs"),
                true);

            File.Copy(
                BalloonManagerPath,
                Path.Combine(backup, "BalloonManager.cs"),
                true);

            int scoreChanges = PatchScoreManager();
            int balloonChanges = PatchBalloonManager();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.8.6 MISS + POP FIX APPLIED\n" +
                "ScoreManager changes: " + scoreChanges + "\n" +
                "BalloonManager changes: " + balloonChanges + "\n" +
                "Backup: " + backup + "\n\n" +
                "Untouched balloons now use SMALL passive-miss feedback.\n" +
                "Button-attempt misses keep the LARGE MISS feedback.\n" +
                "Successful balloons get an added pop burst plus release failsafe.");
        }

        [MenuItem(
            "Tools/Balloon Rush/Verify v1.8.6 Miss + Pop Fix",
            priority = -47)]
        public static void VerifyPatch()
        {
            string score = File.Exists(ScoreManagerPath)
                ? Normalize(File.ReadAllText(ScoreManagerPath))
                : string.Empty;

            string balloon = File.Exists(BalloonManagerPath)
                ? Normalize(File.ReadAllText(BalloonManagerPath))
                : string.Empty;

            bool runtimeExists = File.Exists(
                "Assets/BalloonRush/Scripts/UI/" +
                "BalloonRushMissPopPolishV186.cs");

            bool scoreSupportsPassive =
                score.Contains(
                    "RecordMiss(bool showTimingFeedback = true)");

            bool passedUsesPassive =
                balloon.Contains("RecordMiss(false)");

            bool passiveUiHook =
                balloon.Contains(
                    "BalloonRushMissPopPolishV186.NotifyPassiveMiss");

            if (runtimeExists &&
                scoreSupportsPassive &&
                passedUsesPassive &&
                passiveUiHook)
            {
                Debug.Log(
                    "Balloon Rush v1.8.6 VERIFY PASS: " +
                    "passive misses are separated from player-attempt misses, " +
                    "and pop-guarantee runtime is installed.");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.8.6 VERIFY\n" +
                    "Runtime feedback script: " + runtimeExists + "\n" +
                    "ScoreManager passive flag: " + scoreSupportsPassive + "\n" +
                    "BalloonManager RecordMiss(false): " + passedUsesPassive + "\n" +
                    "Passive UI hook: " + passiveUiHook);
            }
        }

        private static int PatchScoreManager()
        {
            string text =
                Normalize(File.ReadAllText(ScoreManagerPath));

            if (text.Contains(
                    "RecordMiss(bool showTimingFeedback = true)"))
            {
                Debug.Log(
                    "Balloon Rush v1.8.6: ScoreManager already patched.");
                return 0;
            }

            const string pattern =
                @"(?s)        public void RecordMiss\(\)\n" +
                @"        \{\n" +
                @"            Misses\+\+;\n" +
                @"            comboManager\?\.RegisterMiss\(\);\n" +
                @"            GameEvents\.RaiseTimingJudged\(TimingRating\.Miss\);\n" +
                @"        \}";

            const string replacement =
@"        public void RecordMiss(bool showTimingFeedback = true)
        {
            Misses++;
            comboManager?.RegisterMiss();

            // Only a MISS caused by an actual player POP attempt should drive
            // the large central timing feedback. A balloon that simply passes
            // still counts as a miss / combo break, but gets quieter feedback.
            if (showTimingFeedback)
            {
                GameEvents.RaiseTimingJudged(TimingRating.Miss);
            }
        }";

            string updated = Regex.Replace(
                text,
                pattern,
                replacement,
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (updated == text)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.6: Could not locate ScoreManager.RecordMiss(). " +
                    "No ScoreManager changes were written.");
                return 0;
            }

            WriteText(ScoreManagerPath, updated);
            return 1;
        }

        private static int PatchBalloonManager()
        {
            string text =
                Normalize(File.ReadAllText(BalloonManagerPath));

            if (text.Contains(
                    "BalloonRushMissPopPolishV186.NotifyPassiveMiss"))
            {
                Debug.Log(
                    "Balloon Rush v1.8.6: BalloonManager already patched.");
                return 0;
            }

            // This is deliberately narrow: only the HandleBalloonPassed()
            // passive-miss pair is changed. HandleMissAt() remains untouched,
            // so a real player POP miss still gets the large MISS feedback.
            const string pattern =
                @"scoreManager\?\.RecordMiss\(\);\n" +
                @"[ \t]*uiManager\?\.ShowRating\(TimingRating\.Miss\);";

            const string replacement =
@"scoreManager?.RecordMiss(false);
                BalloonRush.UI.BalloonRushMissPopPolishV186.NotifyPassiveMiss(
                    balloon.LaneIndex);";

            string updated = Regex.Replace(
                text,
                pattern,
                replacement,
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (updated == text)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.6: Could not locate the passive-miss " +
                    "block in BalloonManager.HandleBalloonPassed(). " +
                    "No BalloonManager changes were written.");
                return 0;
            }

            WriteText(BalloonManagerPath, updated);
            return 1;
        }

        private static string CreateBackupFolder()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Directory.GetCurrentDirectory();

            string folder = Path.Combine(
                projectRoot,
                "Backups",
                "BalloonRush_v1.8.6_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            Directory.CreateDirectory(folder);
            return folder;
        }

        private static string Normalize(string text)
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
