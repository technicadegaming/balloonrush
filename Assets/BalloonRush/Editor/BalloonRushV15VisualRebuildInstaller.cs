#if UNITY_EDITOR
using System;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    /// <summary>
    /// Installs the single MainGame visual system and removes every older visual
    /// pass that could fight for the same layout.
    /// </summary>
    public static class BalloonRushV15VisualRebuildInstaller
    {
        private static readonly string[] LegacyVisualTypeNames =
        {
            "ArcadeUIVisualRefit",
            "BalloonRushAutoVisualUpgrade",
            "BalloonRushArcadePolishV150",
            "BalloonRushReferenceStylePolishV151",
            "BalloonRushReferenceStylePolish",
            "BalloonRushMainGameVisualRebuild"
        };

        [MenuItem("Tools/Balloon Rush/Install Single UI Visual System", priority = 1)]
        public static void InstallSingleVisualSystem()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode before installing the UI visual system.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            if (scene.name.IndexOf("MainGame", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Debug.LogWarning("Balloon Rush: This installer is intended for the MainGame scene. Current scene: " + scene.name);
            }

            Canvas canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush: Gameplay Canvas was not found.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Install Balloon Rush Single UI System");

            RemoveAllLegacyVisualComponents();
            RemoveLegacyRuntimeObjects();

            BalloonRushMainGameVisualRebuild rebuild = canvas.GetComponent<BalloonRushMainGameVisualRebuild>();
            if (rebuild == null)
                rebuild = Undo.AddComponent<BalloonRushMainGameVisualRebuild>(canvas.gameObject);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = canvas.gameObject;

            Debug.Log(
                "Balloon Rush: Single UI visual system installed. " +
                "Removed legacy UI polish components and kept only BalloonRushMainGameVisualRebuild on '" +
                canvas.name + "'.");
        }

        [MenuItem("Tools/Balloon Rush/Check MainGame Visual Components", priority = 2)]
        public static void CheckVisualComponents()
        {
            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            int visualCount = 0;
            foreach (MonoBehaviour component in all)
            {
                if (component == null)
                    continue;

                string typeName = component.GetType().Name;
                if (!IsVisualType(typeName))
                    continue;

                visualCount++;
                Debug.Log("Balloon Rush visual component: " + typeName + " on " + component.gameObject.name, component);
            }

            Debug.Log("Balloon Rush: Found " + visualCount + " visual-system component(s). Expected after cleanup: 1.");
        }

        private static Canvas FindGameplayCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
                    return canvas;
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static void RemoveAllLegacyVisualComponents()
        {
            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (MonoBehaviour component in all)
            {
                if (component == null)
                    continue;

                string typeName = component.GetType().Name;
                if (!IsVisualType(typeName))
                    continue;

                Undo.DestroyObjectImmediate(component);
            }
        }

        private static bool IsVisualType(string typeName)
        {
            for (int i = 0; i < LegacyVisualTypeNames.Length; i++)
            {
                if (string.Equals(typeName, LegacyVisualTypeNames[i], StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void RemoveLegacyRuntimeObjects()
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = all.Length - 1; i >= 0; i--)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                string n = t.name;
                if (n.StartsWith("V15_", StringComparison.OrdinalIgnoreCase) ||
                    n.StartsWith("BRUI_", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("BalloonRushUnifiedHUD", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("ArcadeUIVisualRefit", StringComparison.OrdinalIgnoreCase))
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
        }
    }
}
#endif
