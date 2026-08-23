#if UNITY_EDITOR
using System;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushV18PresentationInstaller
    {
        private const string MainGamePath = "Assets/BalloonRush/Scenes/MainGame.unity";
        private const string ResultsPath = "Assets/BalloonRush/Scenes/Results.unity";

        [MenuItem("Tools/Balloon Rush/v1.8 - Install Presentation Polish", priority = 0)]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush v1.8: Stop Play Mode before installing presentation polish.");
                return;
            }

            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                InstallMainGame();
                InstallResults();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Balloon Rush v1.8 presentation polish installed. Gameplay/ticket logic was not modified.");
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
                Debug.LogError("Balloon Rush v1.8: Gameplay Canvas not found in MainGame.");
                return;
            }

            RemoveLegacyVisuals(canvas.gameObject);

            BalloonRushMainGameVisualRebuild rebuild = canvas.GetComponent<BalloonRushMainGameVisualRebuild>();
            if (rebuild == null)
                rebuild = Undo.AddComponent<BalloonRushMainGameVisualRebuild>(canvas.gameObject);

            BalloonRushMainGameVisualRebuild[] all = canvas.GetComponents<BalloonRushMainGameVisualRebuild>();
            for (int i = 1; i < all.Length; i++)
            {
                if (all[i] != null)
                    Undo.DestroyObjectImmediate(all[i]);
            }

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
                Debug.LogError("Balloon Rush v1.8: Canvas not found in Results scene.");
                return;
            }

            BalloonRushResultsPresentationV180 resultStyle = canvas.GetComponent<BalloonRushResultsPresentationV180>();
            if (resultStyle == null)
                resultStyle = Undo.AddComponent<BalloonRushResultsPresentationV180>(canvas.gameObject);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveLegacyVisuals(GameObject target)
        {
            RemoveIfPresent<ArcadeUIVisualRefit>(target);
            RemoveIfPresent<BalloonRushAutoVisualUpgrade>(target);
            RemoveIfPresent<BalloonRushArcadePolishV150>(target);
            RemoveIfPresent<BalloonRushReferenceStylePolishV151>(target);
        }

        private static void RemoveIfPresent<T>(GameObject target) where T : Component
        {
            T[] components = target.GetComponents<T>();
            foreach (T component in components)
            {
                if (component != null)
                    Undo.DestroyObjectImmediate(component);
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
