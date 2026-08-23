#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.Editor
{
    [InitializeOnLoad]
    public static class BalloonRushTransientMissingScriptWatcher
    {
        private static bool enabled;
        private static double nextScanTime;
        private static readonly HashSet<int> alreadyReported = new HashSet<int>();

        static BalloonRushTransientMissingScriptWatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/Enable TRANSIENT Watcher")]
        public static void EnableWatcher()
        {
            enabled = true;
            alreadyReported.Clear();
            nextScanTime = 0;
            EditorApplication.update -= Scan;
            EditorApplication.update += Scan;
            Debug.Log("Balloon Rush transient missing-script watcher ENABLED. Enter Play Mode now.");
        }

        [MenuItem("Tools/Balloon Rush/Missing Scripts/Disable TRANSIENT Watcher")]
        public static void DisableWatcher()
        {
            enabled = false;
            EditorApplication.update -= Scan;
            Debug.Log("Balloon Rush transient missing-script watcher disabled.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                alreadyReported.Clear();
            }
        }

        private static void Scan()
        {
            if (!enabled || !EditorApplication.isPlaying)
                return;

            // Scan frequently enough to catch objects created and destroyed during startup.
            if (EditorApplication.timeSinceStartup < nextScanTime)
                return;

            nextScanTime = EditorApplication.timeSinceStartup + 0.02;

            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Transform t in all)
            {
                if (t == null)
                    continue;

                GameObject go = t.gameObject;
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

                if (missing <= 0)
                    continue;

                int id = go.GetInstanceID();
                if (!alreadyReported.Add(id))
                    continue;

                string sceneName = go.scene.IsValid()
                    ? go.scene.name
                    : "<runtime/DontDestroyOnLoad>";

                string path = GetHierarchyPath(t);

                Debug.LogError(
                    "TRANSIENT MISSING SCRIPT CAUGHT\n" +
                    "Scene: " + sceneName + "\n" +
                    "Object: " + path + "\n" +
                    "Missing component count: " + missing + "\n" +
                    "Instance ID: " + id,
                    go
                );
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null)
                return "<null>";

            Stack<string> parts = new Stack<string>();
            Transform cur = t;

            while (cur != null)
            {
                parts.Push(cur.name);
                cur = cur.parent;
            }

            return string.Join("/", parts);
        }
    }
}
#endif
