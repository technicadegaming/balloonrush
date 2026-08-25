#if UNITY_EDITOR
using BalloonRush.Audio;
using BalloonRush.Core;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV196GameplayMusicInstaller
    {
        private const string AudioConfigPath = "Assets/BalloonRush/Resources/AudioConfig.asset";
        private const string Track1Path = "Assets/BalloonRush/Audio/Gameplay/BR_Gameplay_NeonPop.wav";
        private const string Track2Path = "Assets/BalloonRush/Audio/Gameplay/BR_Gameplay_BalloonBounce.wav";
        private const string Track3Path = "Assets/BalloonRush/Audio/Gameplay/BR_Gameplay_ArcadeRush.wav";

        [MenuItem("Tools/Balloon Rush/v1.9.6 - Install Gameplay Music Rotation", priority = 1960)]
        public static void Install()
        {
            AssetDatabase.ImportAsset(Track1Path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Track2Path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(Track3Path, ImportAssetOptions.ForceUpdate);

            ConfigureImporter(Track1Path);
            ConfigureImporter(Track2Path);
            ConfigureImporter(Track3Path);

            AudioConfig audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            if (audio == null)
            {
                Debug.LogError("v1.9.6: AudioConfig.asset not found.");
                return;
            }

            AudioClip track1 = AssetDatabase.LoadAssetAtPath<AudioClip>(Track1Path);
            AudioClip track2 = AssetDatabase.LoadAssetAtPath<AudioClip>(Track2Path);
            AudioClip track3 = AssetDatabase.LoadAssetAtPath<AudioClip>(Track3Path);

            Undo.RecordObject(audio, "Install Balloon Rush gameplay music rotation");

            // Preserve a custom primary track if one already exists. Otherwise use
            // Neon Pop as the primary track.
            if (audio.gameplayMusic == null)
            {
                audio.gameplayMusic = track1;
            }

            audio.gameplayMusicAlt1 = track2;
            audio.gameplayMusicAlt2 = track3;
            EditorUtility.SetDirty(audio);

            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                Undo.RecordObject(config, "Balloon Rush v1.9.6 version");
                config.buildVersion = "1.9.6";
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Balloon Rush v1.9.6 Gameplay Music Rotation installed.\n" +
                "- 3-track normal gameplay playlist\n" +
                "- crossfade rotation\n" +
                "- pitch follows DifficultyManager progress\n" +
                "- Rush / Golden / Jackpot / Results keep their separate cues\n" +
                "- Operator settings: rotation, rotate seconds, start pitch, end pitch\n" +
                "Defaults: rotate every 8 sec, pitch 0.96 -> 1.18."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.6 - Verify Gameplay Music Rotation", priority = 1961)]
        public static void Verify()
        {
            AudioConfig audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            AudioClip one = AssetDatabase.LoadAssetAtPath<AudioClip>(Track1Path);
            AudioClip two = AssetDatabase.LoadAssetAtPath<AudioClip>(Track2Path);
            AudioClip three = AssetDatabase.LoadAssetAtPath<AudioClip>(Track3Path);

            Debug.Log(
                "Balloon Rush v1.9.6 VERIFY\n" +
                "AudioConfig: " + (audio != null ? "FOUND" : "MISSING") + "\n" +
                "Neon Pop: " + (one != null ? "FOUND" : "MISSING") + "\n" +
                "Balloon Bounce: " + (two != null ? "FOUND" : "MISSING") + "\n" +
                "Arcade Rush: " + (three != null ? "FOUND" : "MISSING") + "\n" +
                "Primary: " + (audio != null && audio.gameplayMusic != null ? audio.gameplayMusic.name : "<none>") + "\n" +
                "Alt 1: " + (audio != null && audio.gameplayMusicAlt1 != null ? audio.gameplayMusicAlt1.name : "<none>") + "\n" +
                "Alt 2: " + (audio != null && audio.gameplayMusicAlt2 != null ? audio.gameplayMusicAlt2.name : "<none>")
            );
        }

        private static void ConfigureImporter(string path)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.75f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
#endif
