#if UNITY_EDITOR
using BalloonRush.Core;
using BalloonRush.UI;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV194CabinetOperationsInstaller
    {
        [MenuItem("Tools/Balloon Rush/v1.9.4 - Install Cabinet Operations", priority = 1940)]
        public static void Install()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                Undo.RecordObject(config, "Balloon Rush v1.9.4 cabinet settings");
                config.buildVersion = "1.9.4";
                config.targetWidth = 1080;
                config.targetHeight = 1920;
                config.enforcePortraitResolutionInPlayer = true;
                config.playerFullScreenMode = FullScreenMode.FullScreenWindow;
                config.hideCursorInPlayer = true;
                config.runInBackground = true;
                config.resultsTimeout = 3f;
                EditorUtility.SetDirty(config);
            }

            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Balloon Rush v1.9.4 Cabinet Operations installed.\n" +
                "- 1080x1920 borderless FullScreenWindow build defaults\n" +
                "- runtime fullscreen watchdog\n" +
                "- current credits / pending tickets clear controls\n" +
                "- lifetime credits / lifetime tickets display + individual reset\n" +
                "- attract music duty cycle defaults: 15 sec ON / 45 sec SILENT\n" +
                "- existing MASTER / MUSIC / SFX operator volume controls remain active\n" +
                "Test from Boot and open Operator with M / JoystickButton4."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.4 - Verify Cabinet Operations", priority = 1941)]
        public static void Verify()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            MonoScript operationsScript = FindScript(nameof(CabinetOperationsV194));
            MonoScript creditScript = FindScript("CreditManager");

            Debug.Log(
                "Balloon Rush v1.9.4 VERIFY\n" +
                "CabinetOperationsV194: " + (operationsScript != null ? "FOUND" : "MISSING") + "\n" +
                "CreditManager replacement: " + (creditScript != null ? "FOUND" : "MISSING") + "\n" +
                "Build version: " + (config != null ? config.buildVersion : "<missing config>") + "\n" +
                "Resolution: " + (config != null ? config.targetWidth + "x" + config.targetHeight : "<unknown>") + "\n" +
                "Fullscreen mode: " + (config != null ? config.playerFullScreenMode.ToString() : "<unknown>")
            );
        }

        private static MonoScript FindScript(string className)
        {
            string[] guids = AssetDatabase.FindAssets(className + " t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() != null && script.GetClass().Name == className)
                {
                    return script;
                }
            }
            return null;
        }
    }
}
#endif
