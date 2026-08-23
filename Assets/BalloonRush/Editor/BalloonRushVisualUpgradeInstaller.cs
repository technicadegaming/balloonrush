#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.UI.Editor
{
    public static class BalloonRushVisualUpgradeInstaller
    {
        [MenuItem("Tools/Balloon Rush/Install Visual Upgrade In Current Scene")]
        public static void InstallCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush: No Canvas found in the current scene.");
                return;
            }

            BalloonRushAutoVisualUpgrade upgrade = canvas.GetComponent<BalloonRushAutoVisualUpgrade>();
            if (upgrade == null)
                upgrade = Undo.AddComponent<BalloonRushAutoVisualUpgrade>(canvas.gameObject);

            // Remove the older refit component so both scripts do not fight over anchors.
            ArcadeUIVisualRefit oldRefit = canvas.GetComponent<ArcadeUIVisualRefit>();
            if (oldRefit != null)
                Undo.DestroyObjectImmediate(oldRefit);

            GameObject oldObject = GameObject.Find("ArcadeUIVisualRefit");
            if (oldObject != null && oldObject != canvas.gameObject)
                Undo.DestroyObjectImmediate(oldObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = canvas.gameObject;

            Debug.Log("Balloon Rush: v1.4.3 visual upgrade installed on " + canvas.name +
                      ". No Inspector wiring is required.");
        }
    }
}
#endif
