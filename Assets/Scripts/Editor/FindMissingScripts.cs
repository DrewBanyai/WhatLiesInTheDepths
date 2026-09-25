// What Lies In The Depths — find components and assets whose script reference is missing.
//
// Unity's "The referenced script (Unknown) on this Behaviour is missing!" names nothing, so
// this walks the open scenes, every prefab and every ScriptableObject asset under Assets and
// logs each one it finds, with a clickable context.
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class FindMissingScripts
    {
        [MenuItem("Tools/What Lies In The Depths/Find Missing Scripts", priority = 60)]
        public static void Run()
        {
            int found = 0;

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    found += Walk(root, $"scene {scene.name}");
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) found += Walk(go, path);
            }

            foreach (var path in AssetDatabase.GetAllAssetPaths()
                         .Where(p => (p.StartsWith("Assets/") || p.StartsWith("ProjectSettings/")) && p.EndsWith(".asset")))
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o == null)
                    {
                        Debug.LogWarning($"[What Lies In The Depths] Missing script: an object in {path}");
                        found++;
                    }
                }
            }

            Debug.Log($"[What Lies In The Depths] Missing-script scan finished: {found} found.");
        }

        static int Walk(GameObject go, string where)
        {
            int n = 0;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing <= 0) continue;
                Debug.LogWarning($"[What Lies In The Depths] Missing script ×{missing}: {Path(t)} in {where}", t.gameObject);
                n += missing;
            }
            return n;
        }

        static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
