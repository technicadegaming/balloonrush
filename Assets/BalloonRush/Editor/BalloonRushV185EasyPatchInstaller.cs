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
    /// Easy, idempotent v1.8.5 patch installer.
    ///
    /// Workflow:
    /// 1. Merge the supplied Assets folder into the project.
    /// 2. Let Unity compile.
    /// 3. Tools > Balloon Rush > Apply v1.8.5 EASY Enhancement Patch.
    ///
    /// Safe to run after the earlier PowerShell patch partially succeeded.
    /// </summary>
    public static class BalloonRushV185EasyPatchInstaller
    {
        private const string AudioManagerPath =
            "Assets/BalloonRush/Scripts/Audio/AudioManager.cs";

        private const string BalloonManagerPath =
            "Assets/BalloonRush/Scripts/Gameplay/BalloonManager.cs";

        [MenuItem("Tools/Balloon Rush/Apply v1.8.5 EASY Enhancement Patch", priority = -50)]
        public static void ApplyPatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "Balloon Rush v1.8.5: Stop Play Mode before applying the patch.");
                return;
            }

            if (!File.Exists(AudioManagerPath) ||
                !File.Exists(BalloonManagerPath))
            {
                Debug.LogError(
                    "Balloon Rush v1.8.5: Expected project files were not found. " +
                    "Make sure the supplied Assets folder was merged into the Balloon Rush project.");
                return;
            }

            string backupFolder = CreateBackupFolder();

            File.Copy(
                AudioManagerPath,
                Path.Combine(backupFolder, "AudioManager.cs"),
                true);

            File.Copy(
                BalloonManagerPath,
                Path.Combine(backupFolder, "BalloonManager.cs"),
                true);

            int audioChanges = PatchAudioManager();
            int balloonChanges = PatchBalloonManager();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "BALLOON RUSH v1.8.5 EASY PATCH COMPLETE\n" +
                "AudioManager changes: " + audioChanges + "\n" +
                "BalloonManager changes: " + balloonChanges + "\n" +
                "Backup: " + backupFolder + "\n\n" +
                "The visual enhancement auto-installs at runtime. " +
                "Open MainGame and press Play.");
        }

        [MenuItem("Tools/Balloon Rush/Verify v1.8.5 EASY Patch", priority = -49)]
        public static void VerifyPatch()
        {
            bool visualExists = File.Exists(
                "Assets/BalloonRush/Scripts/UI/BalloonRushArcadeJuiceV185.cs");

            bool gateExists = File.Exists(
                "Assets/BalloonRush/Scripts/Audio/BalloonRushAudioGateV185.cs");

            string audio = File.Exists(AudioManagerPath)
                ? Normalize(File.ReadAllText(AudioManagerPath))
                : string.Empty;

            string balloon = File.Exists(BalloonManagerPath)
                ? Normalize(File.ReadAllText(BalloonManagerPath))
                : string.Empty;

            bool audioGateHook =
                audio.Contains("BalloonRushAudioGateV185.Allow(cue)");

            bool genericPopRemoved =
                !balloon.Contains(
                    "GameServices.Audio?.PlaySfx(AudioCue.BalloonPop, 1f, 0.45f);");

            bool comboChirpRemoved =
                !balloon.Contains(
                    "GameServices.Audio?.PlaySfx(AudioCue.ComboIncrease, comboPitch, 0.28f);");

            if (visualExists &&
                gateExists &&
                audioGateHook &&
                genericPopRemoved &&
                comboChirpRemoved)
            {
                Debug.Log(
                    "Balloon Rush v1.8.5 VERIFY PASS: " +
                    "visual system present, audio gate connected, " +
                    "old stacked normal-pop sounds removed.");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.8.5 VERIFY\n" +
                    "Visual script: " + visualExists + "\n" +
                    "Audio gate script: " + gateExists + "\n" +
                    "AudioManager gate hook: " + audioGateHook + "\n" +
                    "Generic normal-pop removed: " + genericPopRemoved + "\n" +
                    "Per-pop combo chirp removed: " + comboChirpRemoved);
            }
        }

        private static int PatchAudioManager()
        {
            string text = Normalize(File.ReadAllText(AudioManagerPath));

            if (text.Contains("BalloonRushAudioGateV185.Allow(cue)"))
            {
                Debug.Log(
                    "Balloon Rush v1.8.5: AudioManager was already patched; leaving it intact.");
                return 0;
            }

            int changes = 0;

            const string oldPlaySfxPattern =
                @"(?s)        public void PlaySfx\(AudioCue cue, float pitch = 1f, float volumeScale = 1f\)\n" +
                @"        \{.*?\n" +
                @"        \}\n\n" +
                @"        public void PlayUi";

            const string newPlaySfx =
@"        public void PlaySfx(AudioCue cue, float pitch = 1f, float volumeScale = 1f)
        {
            EnsureSources();

            if (!BalloonRushAudioGateV185.Allow(cue))
            {
                return;
            }

            AudioClip clip = GetSfxClip(cue);
            if (clip == null)
            {
                return;
            }

            AudioSource destination =
                cue == AudioCue.Jackpot ? jackpotSource : sfxSource;

            if (cue == AudioCue.Jackpot ||
                cue == AudioCue.BombExplosion ||
                cue == AudioCue.BonusStart ||
                cue == AudioCue.GoldenBalloonPop)
            {
                destination.Stop();
            }

            destination.pitch = Mathf.Clamp(pitch, 0.5f, 2f);

            float cleanVolume = Mathf.Clamp01(
                volumeScale * BalloonRushAudioGateV185.GetVolumeScale(cue));

            destination.PlayOneShot(clip, cleanVolume);
            destination.pitch = 1f;
        }

        public void PlayUi";

            string updated = Regex.Replace(
                text,
                oldPlaySfxPattern,
                newPlaySfx,
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (updated != text)
            {
                text = updated;
                changes++;
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush v1.8.5: PlaySfx did not match the original form. " +
                    "This usually means the previous patch already changed it.");
            }

            const string oldPlayUiPattern =
                @"(?s)        public void PlayUi\(AudioCue cue\)\n" +
                @"        \{.*?\n" +
                @"        \}\n\n" +
                @"        public void PlayVoice";

            const string newPlayUi =
@"        public void PlayUi(AudioCue cue)
        {
            EnsureSources();

            if (!BalloonRushAudioGateV185.Allow(cue))
            {
                return;
            }

            AudioClip clip = GetSfxClip(cue);
            if (clip != null)
            {
                uiSource.Stop();
                uiSource.PlayOneShot(
                    clip,
                    BalloonRushAudioGateV185.GetVolumeScale(cue));
            }
        }

        public void PlayVoice";

            updated = Regex.Replace(
                text,
                oldPlayUiPattern,
                newPlayUi,
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (updated != text)
            {
                text = updated;
                changes++;
            }

            WriteText(AudioManagerPath, text);
            return changes;
        }

        private static int PatchBalloonManager()
        {
            string text = Normalize(File.ReadAllText(BalloonManagerPath));
            int changes = 0;

            // Remove the old generic pop. The timing grade sound is more useful.
            string updated = Regex.Replace(
                text,
                @"[ \t]*GameServices\.Audio\?\.PlaySfx\(AudioCue\.BalloonPop,\s*1f,\s*0\.45f\);\n",
                string.Empty);

            if (updated != text)
            {
                text = updated;
                changes++;
            }

            // Remove the old per-pop combo chirp block. Milestone audio remains.
            updated = Regex.Replace(
                text,
                @"(?s)[ \t]*if \(comboManager != null\)\n" +
                @"[ \t]*\{\n" +
                @"[ \t]*float comboPitch = .*?;\n" +
                @"[ \t]*GameServices\.Audio\?\.PlaySfx\(AudioCue\.ComboIncrease, comboPitch, 0\.28f\);\n" +
                @"[ \t]*\}\n",
                string.Empty);

            if (updated != text)
            {
                text = updated;
                changes++;
            }

            // Ensure successful normal pops still get the timing-quality sound.
            int effectIndex = text.IndexOf(
                "effectsManager?.PlaySuccessfulPop(target.transform.position, definition.VisualColor, rating, ticketAward);",
                StringComparison.Ordinal);

            int applyIndex = effectIndex >= 0
                ? text.IndexOf(
                    "ApplySpecialBehavior(target, definition, rating);",
                    effectIndex,
                    StringComparison.Ordinal)
                : -1;

            if (effectIndex >= 0 && applyIndex > effectIndex)
            {
                string segment = text.Substring(
                    effectIndex,
                    applyIndex - effectIndex);

                if (!segment.Contains("PlayTimingAudio(rating);"))
                {
                    const string anchor =
                        "effectsManager?.PlaySuccessfulPop(target.transform.position, definition.VisualColor, rating, ticketAward);";

                    text = text.Replace(
                        anchor,
                        anchor +
                        "\n            PlayTimingAudio(rating);");

                    changes++;
                }
            }

            WriteText(BalloonManagerPath, text);

            if (changes == 0)
            {
                Debug.Log(
                    "Balloon Rush v1.8.5: BalloonManager already appears to have the clean-pop audio changes.");
            }

            return changes;
        }

        private static string CreateBackupFolder()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Directory.GetCurrentDirectory();

            string folder = Path.Combine(
                projectRoot,
                "Backups",
                "BalloonRush_v1.8.5_Easy_" +
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

        private static void WriteText(string path, string normalizedText)
        {
            string windowsText = normalizedText.Replace("\n", Environment.NewLine);
            File.WriteAllText(
                path,
                windowsText,
                new UTF8Encoding(false));
        }
    }
}
#endif
