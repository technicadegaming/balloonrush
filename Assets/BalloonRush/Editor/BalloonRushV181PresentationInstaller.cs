#if UNITY_EDITOR
using System;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushV181PresentationInstaller
    {
        private const string MainGamePath = "Assets/BalloonRush/Scenes/MainGame.unity";
        private const string ResultsPath = "Assets/BalloonRush/Scenes/Results.unity";

        [MenuItem("Tools/Balloon Rush/v1.8.1 - Install Presentation FX", priority = 0)]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush v1.8.1: Stop Play Mode before installing presentation FX.");
                return;
            }

            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                InstallMainGameFx();
                InstallResultsFx();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Balloon Rush v1.8.1 installed. Gameplay, scoring, tickets, inputs and hardware were not modified.");
            }
            finally
            {
                if (original != null && original.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        [MenuItem("Tools/Balloon Rush/v1.8.1 - Verify Presentation FX", priority = 1)]
        public static void Verify()
        {
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene main = EditorSceneManager.OpenScene(MainGamePath, OpenSceneMode.Single);
                Canvas mainCanvas = FindGameplayCanvas();
                int mainVisual = mainCanvas != null ? mainCanvas.GetComponents<BalloonRushMainGameVisualRebuild>().Length : 0;
                int mainFx = mainCanvas != null ? mainCanvas.GetComponents<BalloonRushPresentationFXV181>().Length : 0;

                Scene results = EditorSceneManager.OpenScene(ResultsPath, OpenSceneMode.Single);
                Canvas resultsCanvas = FindFirstCanvas();
                int resultsFx = resultsCanvas != null ? resultsCanvas.GetComponents<BalloonRushResultsPresentationV181>().Length : 0;

                if (mainVisual == 1 && mainFx == 1 && resultsFx == 1)
                {
                    Debug.Log("Balloon Rush v1.8.1 VERIFY PASS: 1 unified MainGame visual, 1 presentation FX companion, 1 Results celebration component.");
                }
                else
                {
                    Debug.LogWarning(
                        "Balloon Rush v1.8.1 VERIFY: Main visual=" + mainVisual +
                        ", Main FX=" + mainFx +
                        ", Results FX=" + resultsFx +
                        ". Expected 1 / 1 / 1.");
                }
            }
            finally
            {
                if (original != null && original.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        private static void InstallMainGameFx()
        {
            Scene scene = EditorSceneManager.OpenScene(MainGamePath, OpenSceneMode.Single);
            Canvas canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush v1.8.1: Gameplay Canvas not found in MainGame.");
                return;
            }

            BalloonRushPresentationFXV181 fx = canvas.GetComponent<BalloonRushPresentationFXV181>();
            if (fx == null)
                fx = Undo.AddComponent<BalloonRushPresentationFXV181>(canvas.gameObject);

            BalloonRushPresentationFXV181[] all = canvas.GetComponents<BalloonRushPresentationFXV181>();
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                    Undo.DestroyObjectImmediate(all[i]);
            }

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void InstallResultsFx()
        {
            Scene scene = EditorSceneManager.OpenScene(ResultsPath, OpenSceneMode.Single);
            Canvas canvas = FindFirstCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush v1.8.1: Canvas not found in Results.");
                return;
            }

            // Replace the v1.8.0 Results decorator only. ResultsManager stays untouched.
            BalloonRushResultsPresentationV180 old = canvas.GetComponent<BalloonRushResultsPresentationV180>();
            if (old != null)
                Undo.DestroyObjectImmediate(old);

            BalloonRushResultsPresentationV181 fx = canvas.GetComponent<BalloonRushResultsPresentationV181>();
            if (fx == null)
                fx = Undo.AddComponent<BalloonRushResultsPresentationV181>(canvas.gameObject);

            BalloonRushResultsPresentationV181[] all = canvas.GetComponents<BalloonRushResultsPresentationV181>();
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                    Undo.DestroyObjectImmediate(all[i]);
            }

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Canvas FindGameplayCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
                    return canvas;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static Canvas FindFirstCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
#endif
