#if UNITY_EDITOR
using BalloonRush.Core;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV192CabinetInputInstaller
    {
        [MenuItem("Tools/Balloon Rush/v1.9.2 - Install Cabinet Input + Auto Start")]
        public static void Install()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                Undo.RecordObject(config, "Balloon Rush v1.9.2 Cabinet Input");
                config.buildVersion = "1.9.2";
                EditorUtility.SetDirty(config);
            }

            PlayerSettings.bundleVersion = "1.9.2";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Balloon Rush v1.9.2 installed. No scene wiring required.\n" +
                "CABINET INPUTS:\n" +
                "LEFT  = LeftArrow / JoystickButton1\n" +
                "POP   = UpArrow / JoystickButton2\n" +
                "RIGHT = RightArrow / JoystickButton7\n" +
                "MENU  = M / JoystickButton4 key switch\n" +
                "CREDIT FLOW: new credit auto-starts one game after 1 second; " +
                "credits already waiting on return to Attract auto-start after 3 seconds."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.2 - Verify Cabinet Input")]
        public static void Verify()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            string version = config != null ? config.buildVersion : "<missing config>";

#if ENABLE_LEGACY_INPUT_MANAGER
            string inputStatus = "Legacy Input Manager available: exact JoystickButton1/2/4/7 mappings ENABLED.";
#else
            string inputStatus = "WARNING: Legacy Input Manager is not enabled. Set Active Input Handling to Both or Input Manager (Old) for exact cabinet JoystickButton mappings.";
#endif

            Debug.Log(
                "Balloon Rush v1.9.2 verification:\n" +
                "Build version: " + version + "\n" +
                inputStatus + "\n" +
                "Expected key switch: M / JoystickButton4.\n" +
                "Expected menu: LEFT/RIGHT navigate; POP select/edit; key switch exits.\n" +
                "Expected credit flow: paid credit -> visible credit -> auto consume exactly one -> MainGame."
            );
        }
    }
}
#endif
