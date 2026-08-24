#if UNITY_EDITOR
using System;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushV182PresentationInstaller
    {
        private const string MainGamePath = "Assets/BalloonRush/Scenes/MainGame.unity";
        private const string ResultsPath = "Assets/BalloonRush/Scenes/Results.unity";

        [MenuItem("Tools/Balloon Rush/v1.8.2 - Install Visual Refinement", priority = 0)]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush v1.8.2: Stop Play Mode before installing.");
                return;
            }

            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                InstallMainGame();
                InstallResults();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Balloon Rush v1.8.2 installed. This pass changes presentation only; gameplay/ticket systems were not modified.");
            }
            finally
            {
                if (original != null && original.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        [MenuItem("Tools/Balloon Rush/v1.8.2 - Verify Visual Refinement", priority = 1)]
        public static void Verify()
        {
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(MainGamePath, OpenSceneMode.Single);
                Canvas mainCanvas = FindGameplayCanvas();
                int mainVisual = mainCanvas != null ? mainCanvas.GetComponents<BalloonRushMainGameVisualRebuild>().Length : 0;
                int mainV181 = mainCanvas != null ? mainCanvas.GetComponents<BalloonRushPresentationFXV181>().Length : 0;
                int mainV182 = mainCanvas != null ? mainCanvas.GetComponents<BalloonRushPresentationFXV182>().Length : 0;

                EditorSceneManager.OpenScene(ResultsPath, OpenSceneMode.Single);
                Canvas resultsCanvas = FindFirstCanvas();
                int resultsV180 = resultsCanvas != null ? resultsCanvas.GetComponents<BalloonRushResultsPresentationV180>().Length : 0;
                int resultsV181 = resultsCanvas != null ? resultsCanvas.GetComponents<BalloonRushResultsPresentationV181>().Length : 0;
                int resultsV182 = resultsCanvas != null ? resultsCanvas.GetComponents<BalloonRushResultsPresentationV182>().Length : 0;

                if (mainVisual == 1 && mainV181 == 0 && mainV182 == 1 && resultsV180 == 0 && resultsV181 == 0 && resultsV182 == 1)
                {
                    Debug.Log("Balloon Rush v1.8.2 VERIFY PASS: Main visual=1, v1.8.1 Main FX=0, v1.8.2 Main FX=1, old Results FX=0, v1.8.2 Results FX=1.");
                }
                else
                {
                    Debug.LogWarning(
                        "Balloon Rush v1.8.2 VERIFY:\n" +
                        "Main visual=" + mainVisual + "\n" +
                        "Main v1.8.1 FX=" + mainV181 + "\n" +
                        "Main v1.8.2 FX=" + mainV182 + "\n" +
                        "Results v1.8.0 FX=" + resultsV180 + "\n" +
                        "Results v1.8.1 FX=" + resultsV181 + "\n" +
                        "Results v1.8.2 FX=" + resultsV182 + "\n" +
                        "Expected: 1 / 0 / 1 / 0 / 0 / 1.");
                }
            }
            finally
            {
                if (original != null && original.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(original);
            }
        }

        private static void InstallMainGame()
        {
            Scene scene = EditorSceneManager.OpenScene(MainGamePath, OpenSceneMode.Single);
            Canvas canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush v1.8.2: Gameplay Canvas not found in MainGame.");
                return;
            }

            BalloonRushMainGameVisualRebuild visual = canvas.GetComponent<BalloonRushMainGameVisualRebuild>();
            if (visual == null)
                visual = Undo.AddComponent<BalloonRushMainGameVisualRebuild>(canvas.gameObject);
            RemoveExtras(canvas.GetComponents<BalloonRushMainGameVisualRebuild>());

            BalloonRushPresentationFXV181 oldFx = canvas.GetComponent<BalloonRushPresentationFXV181>();
            while (oldFx != null)
            {
                Undo.DestroyObjectImmediate(oldFx);
                oldFx = canvas.GetComponent<BalloonRushPresentationFXV181>();
            }

            BalloonRushPresentationFXV182 fx = canvas.GetComponent<BalloonRushPresentationFXV182>();
            if (fx == null)
                fx = Undo.AddComponent<BalloonRushPresentationFXV182>(canvas.gameObject);
            RemoveExtras(canvas.GetComponents<BalloonRushPresentationFXV182>());

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void InstallResults()
        {
            Scene scene = EditorSceneManager.OpenScene(ResultsPath, OpenSceneMode.Single);
            Canvas canvas = FindFirstCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush v1.8.2: Canvas not found in Results.");
                return;
            }

            BalloonRushResultsPresentationV180 v180 = canvas.GetComponent<BalloonRushResultsPresentationV180>();
            while (v180 != null)
            {
                Undo.DestroyObjectImmediate(v180);
                v180 = canvas.GetComponent<BalloonRushResultsPresentationV180>();
            }

            BalloonRushResultsPresentationV181 v181 = canvas.GetComponent<BalloonRushResultsPresentationV181>();
            while (v181 != null)
            {
                Undo.DestroyObjectImmediate(v181);
                v181 = canvas.GetComponent<BalloonRushResultsPresentationV181>();
            }

            BalloonRushResultsPresentationV182 v182 = canvas.GetComponent<BalloonRushResultsPresentationV182>();
            if (v182 == null)
                v182 = Undo.AddComponent<BalloonRushResultsPresentationV182>(canvas.gameObject);
            RemoveExtras(canvas.GetComponents<BalloonRushResultsPresentationV182>());

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveExtras<T>(T[] components) where T : Component
        {
            if (components == null)
                return;

            for (int i = 1; i < components.Length; i++)
            {
                if (components[i] != null)
                    Undo.DestroyObjectImmediate(components[i]);
            }
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
