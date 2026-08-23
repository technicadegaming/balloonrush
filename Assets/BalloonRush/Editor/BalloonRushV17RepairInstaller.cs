#if UNITY_EDITOR
using BalloonRush.Core;
using BalloonRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushV17RepairInstaller
    {
        [MenuItem("Tools/Balloon Rush/v1.7 - Repair MainGame + Unified Visuals", priority = 0)]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: stop Play Mode before running v1.7 repair.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: no active scene.");
                return;
            }

            Canvas canvas = FindGameplayCanvas();
            GameObject mainRoot = GameObject.Find("MainGameRoot");
            if (mainRoot == null)
                mainRoot = FindRootContaining<GameManager>();

            if (canvas == null || mainRoot == null)
            {
                Debug.LogError("Balloon Rush v1.7: MainGameRoot and/or Gameplay Canvas could not be found. Open MainGame.unity first.");
                return;
            }

            RemoveAll<ArcadeUIVisualRefit>(canvas.gameObject);
            RemoveAll<BalloonRushAutoVisualUpgrade>(canvas.gameObject);
            RemoveAll<BalloonRushArcadePolishV150>(canvas.gameObject);
            RemoveAll<BalloonRushReferenceStylePolishV151>(canvas.gameObject);

            BalloonRushMainGameVisualRebuild[] visualSystems = canvas.GetComponents<BalloonRushMainGameVisualRebuild>();
            if (visualSystems.Length == 0)
            {
                Undo.AddComponent<BalloonRushMainGameVisualRebuild>(canvas.gameObject);
            }
            else
            {
                for (int i = 1; i < visualSystems.Length; i++)
                    Undo.DestroyObjectImmediate(visualSystems[i]);
            }

            MainGameRuntimeRepair[] repairs = mainRoot.GetComponents<MainGameRuntimeRepair>();
            if (repairs.Length == 0)
            {
                Undo.AddComponent<MainGameRuntimeRepair>(mainRoot);
            }
            else
            {
                for (int i = 1; i < repairs.Length; i++)
                    Undo.DestroyObjectImmediate(repairs[i]);
            }

            EnsureMainCameraAudioListener();

            EditorUtility.SetDirty(canvas.gameObject);
            EditorUtility.SetDirty(mainRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = mainRoot;
            Debug.Log(
                "Balloon Rush v1.7 repair installed. MainGame now has one unified visual system " +
                "and one pre-start runtime wiring repair. Save complete.");
        }

        [MenuItem("Tools/Balloon Rush/v1.7 - Verify MainGame", priority = 1)]
        public static void Verify()
        {
            Canvas canvas = FindGameplayCanvas();
            GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            RoundManager round = Object.FindFirstObjectByType<RoundManager>(FindObjectsInactive.Include);
            BalloonRush.Gameplay.BalloonSpawner spawner = Object.FindFirstObjectByType<BalloonRush.Gameplay.BalloonSpawner>(FindObjectsInactive.Include);
            BalloonRush.Gameplay.BalloonPool pool = Object.FindFirstObjectByType<BalloonRush.Gameplay.BalloonPool>(FindObjectsInactive.Include);
            MainGameRuntimeRepair repair = Object.FindFirstObjectByType<MainGameRuntimeRepair>(FindObjectsInactive.Include);

            int visuals = canvas != null ? canvas.GetComponents<BalloonRushMainGameVisualRebuild>().Length : 0;

            Debug.Log(
                "Balloon Rush v1.7 VERIFY\n" +
                "GameManager: " + (gm != null) + "\n" +
                "RoundManager: " + (round != null) + "\n" +
                "BalloonSpawner: " + (spawner != null) + "\n" +
                "BalloonPool: " + (pool != null) + "\n" +
                "RuntimeRepair: " + (repair != null) + "\n" +
                "Unified visual systems on Gameplay Canvas: " + visuals + " (expected 1)");
        }

        private static void EnsureMainCameraAudioListener()
        {
            AudioListener existing = Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
            if (existing != null)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                camera = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include);

            if (camera != null)
                Undo.AddComponent<AudioListener>(camera.gameObject);
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

        private static GameObject FindRootContaining<T>() where T : Component
        {
            T component = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
                return null;

            Transform t = component.transform;
            while (t.parent != null)
                t = t.parent;
            return t.gameObject;
        }

        private static void RemoveAll<T>(GameObject go) where T : Component
        {
            T[] components = go.GetComponents<T>();
            foreach (T component in components)
            {
                if (component != null)
                    Undo.DestroyObjectImmediate(component);
            }
        }
    }
}
#endif
