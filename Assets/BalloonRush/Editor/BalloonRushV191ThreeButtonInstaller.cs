#if UNITY_EDITOR
using BalloonRush.Core;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV191ThreeButtonInstaller
    {
        [MenuItem("Tools/Balloon Rush/v1.9.1 - Install 3-Button Cabinet Controls")]
        public static void Install()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                Undo.RecordObject(config, "Balloon Rush v1.9.1 Cabinet Controls");
                config.buildVersion = "1.9.1";
                EditorUtility.SetDirty(config);
            }

            PlayerSettings.bundleVersion = "1.9.1";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Balloon Rush v1.9.1 installed. No scene wiring is required.\n" +
                "CABINET: POP starts from Attract; gameplay remains LEFT/POP/RIGHT.\n" +
                "OPERATOR ENTRY: L L R R L R L R R L R L within 6 seconds.\n" +
                "OPERATOR MENU: LEFT/RIGHT move, POP select/edit/confirm."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.1 - Verify 3-Button Cabinet Controls")]
        public static void Verify()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            string version = config != null ? config.buildVersion : "<missing config>";
            Debug.Log(
                "Balloon Rush v1.9.1 verification:\n" +
                "Build version: " + version + "\n" +
                "ThreeButtonCabinetControls is runtime auto-installed; test from Boot.\n" +
                "Expected controls: Attract POP=start; Operator LEFT/RIGHT=navigate, POP=select/edit."
            );
        }
    }
}
#endif
