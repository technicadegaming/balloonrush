#if UNITY_EDITOR
using System;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushMainGameCleanup
    {
        private static readonly string[] LegacyVisualTypeNames =
        {
            "ArcadeUIVisualRefit",
            "BalloonRushAutoVisualUpgrade",
            "BalloonRushArcadePolishV150",
            "BalloonRushReferenceStylePolish",
            "BalloonRushReferenceStylePolishV151"
        };

        [MenuItem("Tools/Balloon Rush/CLEAN MainGame Visual Components", priority = 0)]
        public static void CleanMainGame()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode before cleaning the MainGame scene.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            int missingRemoved = 0;
            int legacyRemoved = 0;
            int extraRebuildsRemoved = 0;

            Canvas gameplayCanvas = FindGameplayCanvas();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform transform in transforms)
                {
                    if (transform == null)
                        continue;

                    GameObject go = transform.gameObject;

                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (missing > 0)
                    {
                        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Balloon Rush Scripts");
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                        missingRemoved += missing;
                        EditorUtility.SetDirty(go);
                    }

                    MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (behaviour == null)
                            continue;

                        string typeName = behaviour.GetType().Name;

                        if (IsLegacyVisualType(typeName))
                        {
                            Undo.DestroyObjectImmediate(behaviour);
                            legacyRemoved++;
                            continue;
                        }

                        if (typeName == nameof(BalloonRushMainGameVisualRebuild))
                        {
                            bool keep =
                                gameplayCanvas != null &&
                                go == gameplayCanvas.gameObject &&
                                go.GetComponent<BalloonRushMainGameVisualRebuild>() == behaviour;

                            if (!keep)
                            {
                                Undo.DestroyObjectImmediate(behaviour);
                                extraRebuildsRemoved++;
                            }
                        }
                    }
                }
            }

            if (gameplayCanvas == null)
            {
                Debug.LogError("Balloon Rush: Gameplay Canvas was not found. Missing/legacy scripts were cleaned, but the current visual rebuild was not installed.");
            }
            else
            {
                BalloonRushMainGameVisualRebuild rebuild =
                    gameplayCanvas.GetComponent<BalloonRushMainGameVisualRebuild>();

                if (rebuild == null)
                    rebuild = Undo.AddComponent<BalloonRushMainGameVisualRebuild>(gameplayCanvas.gameObject);

                // Remove duplicate rebuild components on the Gameplay Canvas itself.
                BalloonRushMainGameVisualRebuild[] rebuilds =
                    gameplayCanvas.GetComponents<BalloonRushMainGameVisualRebuild>();

                for (int i = 1; i < rebuilds.Length; i++)
                {
                    if (rebuilds[i] != null)
                    {
                        Undo.DestroyObjectImmediate(rebuilds[i]);
                        extraRebuildsRemoved++;
                    }
                }

                EditorUtility.SetDirty(gameplayCanvas.gameObject);
                Selection.activeGameObject = gameplayCanvas.gameObject;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                "Balloon Rush MainGame cleanup complete.\n" +
                "Missing script components removed: " + missingRemoved + "\n" +
                "Legacy visual components removed: " + legacyRemoved + "\n" +
                "Extra MainGame visual rebuilds removed: " + extraRebuildsRemoved + "\n" +
                "Expected final state: Gameplay Canvas has UIManager, DebugPanelManager, and exactly ONE BalloonRushMainGameVisualRebuild."
            );
        }

        [MenuItem("Tools/Balloon Rush/VERIFY MainGame Visual Components", priority = 1)]
        public static void VerifyMainGame()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            int missingCount = 0;
            int legacyCount = 0;
            int rebuildCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform transform in transforms)
                {
                    GameObject go = transform.gameObject;

                    missingCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

                    MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (behaviour == null)
                            continue;

                        string typeName = behaviour.GetType().Name;

                        if (IsLegacyVisualType(typeName))
                            legacyCount++;

                        if (typeName == nameof(BalloonRushMainGameVisualRebuild))
                            rebuildCount++;
                    }
                }
            }

            if (missingCount == 0 && legacyCount == 0 && rebuildCount == 1)
            {
                Debug.Log("Balloon Rush VERIFY PASS: 0 missing scripts, 0 legacy visual components, exactly 1 MainGame visual rebuild.");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush VERIFY:\n" +
                    "Missing script components: " + missingCount + "\n" +
                    "Legacy visual components: " + legacyCount + "\n" +
                    "MainGame visual rebuild components: " + rebuildCount + "\n" +
                    "Expected: 0 / 0 / 1."
                );
            }
        }

        private static bool IsLegacyVisualType(string typeName)
        {
            foreach (string legacy in LegacyVisualTypeNames)
            {
                if (string.Equals(typeName, legacy, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static Canvas FindGameplayCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                if (canvas != null &&
                    canvas.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return canvas;
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
#endif
