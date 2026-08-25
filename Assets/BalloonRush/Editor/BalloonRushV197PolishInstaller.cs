#if UNITY_EDITOR
using BalloonRush.Audio;
using BalloonRush.Core;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV197PolishInstaller
    {
        private const string AudioConfigPath = "Assets/BalloonRush/Resources/AudioConfig.asset";

        [MenuItem("Tools/Balloon Rush/v1.9.7 - Install Attract + Audio + Visual Polish", priority = 1970)]
        public static void Install()
        {
            AudioConfig audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            if (audio == null)
            {
                Debug.LogError("v1.9.7: AudioConfig.asset not found.");
                return;
            }

            string musicRoot = "Assets/BalloonRush/Audio/GameplayV197/";
            string sfxRoot = "Assets/BalloonRush/Audio/SFXV197/";
            ConfigureFolder(musicRoot);
            ConfigureFolder(sfxRoot);

            Undo.RecordObject(audio, "Install Balloon Rush v1.9.7 audio");
            audio.gameplayMusic = Load(musicRoot + "BR197_NeonCarnival.wav");
            audio.gameplayMusicAlt1 = Load(musicRoot + "BR197_SkySprint.wav");
            audio.gameplayMusicAlt2 = Load(musicRoot + "BR197_PrizeFever.wav");

            audio.balloonPop = Load(sfxRoot + "BR197_BalloonPop.wav");
            audio.perfectPop = Load(sfxRoot + "BR197_Perfect.wav");
            audio.greatPop = Load(sfxRoot + "BR197_Great.wav");
            audio.goodPop = Load(sfxRoot + "BR197_Good.wav");
            audio.miss = Load(sfxRoot + "BR197_Miss.wav");
            audio.bombExplosion = Load(sfxRoot + "BR197_Bomb.wav");
            audio.buttonClick = Load(sfxRoot + "BR197_Button.wav");
            audio.laneMove = Load(sfxRoot + "BR197_LaneMove.wav");
            audio.comboIncrease = Load(sfxRoot + "BR197_Combo.wav");
            audio.comboMilestone = Load(sfxRoot + "BR197_ComboMilestone.wav");
            audio.goldenBalloonAppear = Load(sfxRoot + "BR197_GoldenAppear.wav");
            audio.goldenBalloonPop = Load(sfxRoot + "BR197_GoldenPop.wav");
            audio.bonusStart = Load(sfxRoot + "BR197_BonusStart.wav");
            audio.countdown = Load(sfxRoot + "BR197_Countdown.wav");
            audio.gameOver = Load(sfxRoot + "BR197_GameOver.wav");
            audio.ticketCount = Load(sfxRoot + "BR197_Ticket.wav");
            audio.jackpot = Load(sfxRoot + "BR197_Jackpot.wav");
            EditorUtility.SetDirty(audio);

            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                Undo.RecordObject(config, "Balloon Rush v1.9.7 version");
                config.buildVersion = "1.9.7";
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Balloon Rush v1.9.7 installed.\n" +
                "- Pink left / green right cabinet-edge lighting\n" +
                "- Erratic attract flicker; gameplay pulse follows DifficultyManager\n" +
                "- Public M OPERATOR hint removed\n" +
                "- Rounded/pill UI polish\n" +
                "- One gameplay song per round; next round advances to next song\n" +
                "- Gameplay song pitch still rises with difficulty\n" +
                "- Rush/Golden/Jackpot use stingers instead of replacing the round song\n" +
                "- 17 enhanced modern arcade SFX installed"
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.7 - Verify Attract + Audio + Visual Polish", priority = 1971)]
        public static void Verify()
        {
            AudioConfig audio = AssetDatabase.LoadAssetAtPath<AudioConfig>(AudioConfigPath);
            MonoScript visual = FindScript("ArcadeVisualPolishV197");
            MonoScript rails = FindScript("CabinetEdgeLightControllerV197");
            Debug.Log(
                "Balloon Rush v1.9.7 VERIFY\n" +
                "AudioConfig: " + (audio != null ? "FOUND" : "MISSING") + "\n" +
                "Track 1: " + ClipName(audio != null ? audio.gameplayMusic : null) + "\n" +
                "Track 2: " + ClipName(audio != null ? audio.gameplayMusicAlt1 : null) + "\n" +
                "Track 3: " + ClipName(audio != null ? audio.gameplayMusicAlt2 : null) + "\n" +
                "Visual polish: " + (visual != null ? "FOUND" : "MISSING") + "\n" +
                "Edge lights: " + (rails != null ? "FOUND" : "MISSING") + "\n" +
                "Lane SFX: " + ClipName(audio != null ? audio.laneMove : null) + "\n" +
                "Pop SFX: " + ClipName(audio != null ? audio.balloonPop : null) + "\n" +
                "Jackpot SFX: " + ClipName(audio != null ? audio.jackpot : null)
            );
        }

        private static AudioClip Load(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        private static string ClipName(AudioClip clip) => clip != null ? clip.name : "<missing>";

        private static void ConfigureFolder(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder.TrimEnd('/') });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                importer.forceToMono = true;
                importer.loadInBackground = true;
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = path.Contains("GameplayV197") ? 0.78f : 0.72f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        private static MonoScript FindScript(string className)
        {
            string[] guids = AssetDatabase.FindAssets(className + " t:MonoScript");
            foreach (string guid in guids)
            {
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() != null && script.GetClass().Name == className) return script;
            }
            return null;
        }
    }
}
#endif
