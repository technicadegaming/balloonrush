#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.UI.Editor
{
    public static class BalloonRushReferenceStyleInstaller
    {
        [MenuItem("Tools/Balloon Rush/Install v1.5.1 Reference Style Polish")]
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

            BalloonRushReferenceStylePolishV151 style = canvas.GetComponent<BalloonRushReferenceStylePolishV151>();
            if (style == null)
                style = Undo.AddComponent<BalloonRushReferenceStylePolishV151>(canvas.gameObject);

            BalloonRushArcadePolishV150 v150 = canvas.GetComponent<BalloonRushArcadePolishV150>();
            if (v150 != null)
                Undo.DestroyObjectImmediate(v150);

            BalloonRushAutoVisualUpgrade v143 = canvas.GetComponent<BalloonRushAutoVisualUpgrade>();
            if (v143 != null)
                Undo.DestroyObjectImmediate(v143);

            ArcadeUIVisualRefit oldRefit = canvas.GetComponent<ArcadeUIVisualRefit>();
            if (oldRefit != null)
                Undo.DestroyObjectImmediate(oldRefit);

            GameObject oldLoose = GameObject.Find("ArcadeUIVisualRefit");
            if (oldLoose != null && oldLoose != canvas.gameObject)
                Undo.DestroyObjectImmediate(oldLoose);

            Selection.activeGameObject = canvas.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Balloon Rush: v1.5.1 reference-style polish installed on " + canvas.name + ".");
        }
    }
}
#endif
