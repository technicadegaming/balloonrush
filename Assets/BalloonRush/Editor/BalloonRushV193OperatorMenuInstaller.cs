#if UNITY_EDITOR
using BalloonRush.Core;
using BalloonRush.UI;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV193OperatorMenuInstaller
    {
        [MenuItem("Tools/Balloon Rush/v1.9.3 - Install Operator Menu Cleanup", priority = 1930)]
        public static void Install()
        {
            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            if (config != null)
            {
                SerializedObject serialized = new SerializedObject(config);
                SerializedProperty version = serialized.FindProperty("buildVersion");
                if (version != null)
                {
                    version.stringValue = "1.9.3";
                }

                // Keep the previously approved fast Results return.
                SerializedProperty timeout = serialized.FindProperty("resultsTimeout");
                if (timeout != null)
                {
                    timeout.floatValue = 3f;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                "Balloon Rush v1.9.3 Operator Menu Cleanup installed.\n" +
                "- settings ScrollRect normalized\n" +
                "- dynamic rows forced to proper heights\n" +
                "- TEST INPUTS hidden (Diagnostics owns input testing)\n" +
                "- RESET STATS made visible in footer\n" +
                "- Diagnostics starts closed and opens as an opaque full page\n" +
                "- cabinet/keyboard help text updated\n" +
                "Test from Boot, enter Operator using M or JoystickButton4."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9.3 - Verify Operator Menu Cleanup", priority = 1931)]
        public static void Verify()
        {
            MonoScript script = FindScript(nameof(OperatorMenuCabinetPolishV193));
            if (script == null)
            {
                Debug.LogError("v1.9.3 VERIFY FAIL: OperatorMenuCabinetPolishV193.cs not found.");
                return;
            }

            GameConfig config = Resources.Load<GameConfig>("BalloonRushConfig");
            string version = config != null ? config.buildVersion : "<missing config>";
            Debug.Log(
                "Balloon Rush v1.9.3 VERIFY\n" +
                "Runtime polish script: FOUND\n" +
                "Config build version: " + version + "\n" +
                "Expected cabinet inputs remain: JOY1 LEFT / JOY2 POP / JOY7 RIGHT / JOY4 OPERATOR."
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
