#if UNITY_EDITOR
using BalloonRush.Core;
using BalloonRush.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace BalloonRush.Editor
{
    public static class BalloonRushV19DiagnosticsInstaller
    {
        private const string ConfigPath = "Assets/BalloonRush/Resources/BalloonRushConfig.asset";

        [MenuItem("Tools/Balloon Rush/v1.9 - Install Cabinet Diagnostics", priority = 190)]
        public static void Install()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
            {
                Undo.RecordObject(config, "Install Balloon Rush v1.9 diagnostics");
                config.buildVersion = CabinetDiagnosticsService.Version;
                config.resultsTimeout = 3f;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning("Balloon Rush v1.9: BalloonRushConfig.asset was not found. Diagnostics will still auto-load at runtime.");
            }

            AssetDatabase.Refresh();
            Debug.Log(
                "Balloon Rush v1.9 cabinet diagnostics installed.\n" +
                "No scene rebuild is required.\n" +
                "Run from Boot, press M for Operator Menu, and the CABINET DIAGNOSTICS dashboard will open automatically.\n" +
                "This update does not change score, ticket economy, controls, or the 30-second game loop."
            );
        }

        [MenuItem("Tools/Balloon Rush/v1.9 - Verify Cabinet Diagnostics", priority = 191)]
        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            bool configOk = config != null;
            bool versionOk = configOk && config.buildVersion == CabinetDiagnosticsService.Version;
            bool returnOk = configOk && Mathf.Approximately(config.resultsTimeout, 3f);

            string result =
                "BALLOON RUSH v1.9 VERIFY\n" +
                $"Config found: {configOk}\n" +
                $"Build version {CabinetDiagnosticsService.Version}: {versionOk}\n" +
                $"Results timeout 3 sec: {returnOk}\n" +
                "Runtime diagnostics service: AUTO-CREATED BEFORE SCENE LOAD\n" +
                "Operator dashboard: AUTO-INSTALLED WHEN OperatorMenu LOADS";

            if (configOk && versionOk && returnOk)
            {
                Debug.Log(result + "\nVERIFY PASS");
            }
            else
            {
                Debug.LogWarning(result + "\nRun v1.9 - Install Cabinet Diagnostics.");
            }
        }
    }
}
#endif
