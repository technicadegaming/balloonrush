#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    public static class BalloonRushMissingScriptScanner
    {
        private const string ReportPath = "Assets/BalloonRush/Generated/MissingScriptReport.txt";

        [MenuItem("Tools/Balloon Rush/Missing Scripts/1 - Scan Current Scene", priority = 0)]
        public static void ScanCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            List<string> findings = new List<string>();
            ScanSceneObjects(scene, findings);

            WriteReport("CURRENT SCENE: " + scene.path, findings);
            PrintSummary("current scene", findings);
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/2 - Scan ALL Project Scenes and Prefabs", priority = 1)]
        public static void ScanProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode before running the full project scan.");
                return;
            }

            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            List<string> findings = new List<string>();

            try
            {
                ScanAllScenes(findings);
                ScanAllPrefabs(findings);
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            WriteReport("FULL PROJECT SCAN", findings);
            PrintSummary("project scenes and prefabs", findings);

            if (findings.Count > 0)
                Debug.LogWarning("Balloon Rush: Open " + ReportPath + " for exact object paths.");
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/3 - Remove Missing Scripts From Current Scene", priority = 2)]
        public static void CleanCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode before cleaning.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("Balloon Rush: No active scene.");
                return;
            }

            int removed = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in all)
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (count <= 0)
                        continue;

                    Undo.RegisterCompleteObjectUndo(t.gameObject, "Remove Missing Scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    EditorUtility.SetDirty(t.gameObject);
                    removed += count;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("Balloon Rush: removed " + removed + " missing-script component(s) from " + scene.path);
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/4 - Remove Missing Scripts From ALL Prefabs", priority = 3)]
        public static void CleanAllPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Balloon Rush: Stop Play Mode before cleaning.");
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int removed = 0;
            int changedPrefabs = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = null;

                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    int prefabRemoved = RemoveMissingRecursive(root);

                    if (prefabRemoved > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        removed += prefabRemoved;
                        changedPrefabs++;
                        Debug.Log("Balloon Rush: cleaned " + prefabRemoved + " missing script(s) from prefab " + path);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Balloon Rush: could not inspect prefab " + path + "\n" + ex.Message);
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Balloon Rush prefab cleanup complete. " +
                "Changed prefabs: " + changedPrefabs +
                ", missing components removed: " + removed
            );
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/5 - Scan PLAY MODE Objects", priority = 4)]
        public static void ScanPlayModeObjects()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Balloon Rush: Enter Play Mode first, then run this command while the warnings are occurring.");
                return;
            }

            List<string> findings = new List<string>();
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform t in all)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count <= 0)
                    continue;

                string sceneName = t.gameObject.scene.IsValid()
                    ? t.gameObject.scene.name
                    : "<DontDestroyOnLoad or runtime>";

                findings.Add(
                    "[PLAY MODE] scene=" + sceneName +
                    " object=" + GetHierarchyPath(t) +
                    " missing=" + count
                );

                Debug.LogWarning(
                    "MISSING SCRIPT -> scene=" + sceneName +
                    " object=" + GetHierarchyPath(t) +
                    " missing=" + count,
                    t.gameObject
                );
            }

            WriteReport("PLAY MODE OBJECT SCAN", findings);
            PrintSummary("Play Mode objects", findings);
        }

        private static void ScanAllScenes(List<string> findings)
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                try
                {
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    ScanSceneObjects(scene, findings);
                }
                catch (Exception ex)
                {
                    findings.Add("[SCENE ERROR] " + path + " :: " + ex.Message);
                }
            }
        }

        private static void ScanSceneObjects(Scene scene, List<string> findings)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] all = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in all)
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (count <= 0)
                        continue;

                    string message =
                        "[SCENE] " + scene.path +
                        " :: " + GetHierarchyPath(t) +
                        " :: missing=" + count;

                    findings.Add(message);
                    Debug.LogWarning("MISSING SCRIPT -> " + message, t.gameObject);
                }
            }
        }

        private static void ScanAllPrefabs(List<string> findings)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = null;

                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    Transform[] all = root.GetComponentsInChildren<Transform>(true);

                    foreach (Transform t in all)
                    {
                        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        if (count <= 0)
                            continue;

                        string message =
                            "[PREFAB] " + path +
                            " :: " + GetHierarchyPath(t) +
                            " :: missing=" + count;

                        findings.Add(message);
                        Debug.LogWarning("MISSING SCRIPT -> " + message);
                    }
                }
                catch (Exception ex)
                {
                    findings.Add("[PREFAB ERROR] " + path + " :: " + ex.Message);
                }
                finally
                {
                    if (root != null)
                        PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static int RemoveMissingRecursive(GameObject root)
        {
            int removed = 0;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in all)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (count <= 0)
                    continue;

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                EditorUtility.SetDirty(t.gameObject);
                removed += count;
            }

            return removed;
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null)
                return "<null>";

            Stack<string> names = new Stack<string>();
            Transform cursor = t;

            while (cursor != null)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", names);
        }

        private static void PrintSummary(string scope, List<string> findings)
        {
            if (findings.Count == 0)
            {
                Debug.Log("Balloon Rush missing-script scan PASS: no missing script components found in " + scope + ".");
            }
            else
            {
                Debug.LogWarning(
                    "Balloon Rush missing-script scan found " + findings.Count +
                    " object(s) with missing scripts in " + scope +
                    ". See Console or " + ReportPath
                );
            }
        }

        private static void WriteReport(string title, List<string> findings)
        {
            string directory = Path.GetDirectoryName(ReportPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BALLOON RUSH - MISSING SCRIPT REPORT");
            sb.AppendLine(title);
            sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine(new string('-', 72));

            if (findings.Count == 0)
            {
                sb.AppendLine("No missing script components found.");
            }
            else
            {
                foreach (string item in findings)
                    sb.AppendLine(item);
            }

            File.WriteAllText(ReportPath, sb.ToString());
            AssetDatabase.ImportAsset(ReportPath);
        }
    }
}
#endif
