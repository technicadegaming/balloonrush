#if UNITY_EDITOR
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushVisualComponentCleanupV164
    {
        [MenuItem("Tools/Balloon Rush/FIX Visual Components v1.6.4", priority = 0)]
        public static void Fix()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode first.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            Canvas canvas = FindGameplayCanvas();
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush: Gameplay Canvas not found.");
                return;
            }

            int removed = 0;
            removed += RemoveAll<ArcadeUIVisualRefit>(canvas.gameObject);
            removed += RemoveAll<BalloonRushAutoVisualUpgrade>(canvas.gameObject);
            removed += RemoveAll<BalloonRushArcadePolishV150>(canvas.gameObject);
            removed += RemoveAll<BalloonRushReferenceStylePolishV151>(canvas.gameObject);

            BalloonRushMainGameVisualRebuild[] rebuilds = canvas.GetComponents<BalloonRushMainGameVisualRebuild>();
            if (rebuilds.Length == 0)
            {
                Undo.AddComponent<BalloonRushMainGameVisualRebuild>(canvas.gameObject);
            }
            else if (rebuilds.Length > 1)
            {
                for (int i = 1; i < rebuilds.Length; i++)
                {
                    Undo.DestroyObjectImmediate(rebuilds[i]);
                    removed++;
                }
            }

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = canvas.gameObject;

            Debug.Log("Balloon Rush v1.6.4 visual cleanup complete. Removed " + removed +
                      " legacy/duplicate visual component(s). Gameplay Canvas should now have exactly one BalloonRushMainGameVisualRebuild.");
        }

        private static int RemoveAll<T>(GameObject go) where T : Component
        {
            int removed = 0;
            T[] items = go.GetComponents<T>();
            foreach (T item in items)
            {
                if (item == null)
                    continue;
                Undo.DestroyObjectImmediate(item);
                removed++;
            }
            return removed;
        }

        private static Canvas FindGameplayCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c != null && c.name.Contains("Gameplay"))
                    return c;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }
    }
}
#endif
