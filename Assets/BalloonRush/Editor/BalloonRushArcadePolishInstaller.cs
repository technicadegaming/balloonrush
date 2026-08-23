#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.UI.Editor
{
    public static class BalloonRushArcadePolishInstaller
    {
        [MenuItem("Tools/Balloon Rush/Install v1.5 Arcade Polish In Current Scene")]
        public static void InstallCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No valid active scene.");
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("Balloon Rush: No Canvas found in current scene.");
                return;
            }

            BalloonRushArcadePolishV150 polish = canvas.GetComponent<BalloonRushArcadePolishV150>();
            if (polish == null)
                polish = Undo.AddComponent<BalloonRushArcadePolishV150>(canvas.gameObject);

            BalloonRushAutoVisualUpgrade oldUpgrade = canvas.GetComponent<BalloonRushAutoVisualUpgrade>();
            if (oldUpgrade != null)
                Undo.DestroyObjectImmediate(oldUpgrade);

            ArcadeUIVisualRefit oldRefit = canvas.GetComponent<ArcadeUIVisualRefit>();
            if (oldRefit != null)
                Undo.DestroyObjectImmediate(oldRefit);

            GameObject oldLoose = GameObject.Find("ArcadeUIVisualRefit");
            if (oldLoose != null && oldLoose != canvas.gameObject)
                Undo.DestroyObjectImmediate(oldLoose);

            Selection.activeGameObject = canvas.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Balloon Rush: v1.5 arcade polish installed on " + canvas.name + ".");
        }
    }
}
#endif
