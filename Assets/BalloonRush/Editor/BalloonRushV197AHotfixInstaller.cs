#if UNITY_EDITOR
using BalloonRush.Core;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV197AHotfixInstaller
    {
        [MenuItem(
            "Tools/Balloon Rush/v1.9.7a - Verify UI + Audio Hotfix",
            priority = 1972)]
        public static void Verify()
        {
            GameConfig config =
                Resources.Load<GameConfig>("BalloonRushConfig");

            if (config != null)
            {
                Undo.RecordObject(
                    config,
                    "Balloon Rush v1.9.7a version");

                config.buildVersion = "1.9.7a";

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            MonoScript visual =
                FindScript("ArcadeVisualPolishV197");

            MonoScript rails =
                FindScript("CabinetEdgeLightControllerV197");

            Debug.Log(
                "Balloon Rush v1.9.7a VERIFY\n" +
                "Safe visual polish: " +
                (visual != null ? "FOUND" : "MISSING") + "\n" +
                "Edge light controller: " +
                (rails != null ? "FOUND" : "MISSING") + "\n" +
                "Expected behavior:\n" +
                "- existing header/Hit Zone/button artwork stays colored\n" +
                "- no giant white rounded cards\n" +
                "- pink/green rails still animate\n" +
                "- M OPERATOR is hidden from customers\n" +
                "- one normal gameplay track stays locked for a round"
            );
        }

        private static MonoScript FindScript(string className)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    className + " t:MonoScript");

            foreach (string guid in guids)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(guid);

                MonoScript script =
                    AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script != null &&
                    script.GetClass() != null &&
                    script.GetClass().Name == className)
                {
                    return script;
                }
            }

            return null;
        }
    }
}
#endif
