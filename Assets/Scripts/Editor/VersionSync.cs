// What Lies In The Depths — one version number, kept in version.json at the repository root.
// The README's version badges read that file from GitHub (the way a web project's badges
// read package.json), so it is the source of truth; this copies it into Player Settings
// whenever the editor loads scripts, so a build always carries the same number.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WhatLiesInTheDepths.EditorTools
{
    [InitializeOnLoad]
    public static class VersionSync
    {
        static VersionSync() => EditorApplication.delayCall += Sync;

        [System.Serializable]
        sealed class VersionFile { public string version; }

        [MenuItem("Tools/What Lies In The Depths/Sync Version From version.json", priority = 80)]
        public static void Sync()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "version.json"));
            if (!File.Exists(path)) return;
            var file = JsonUtility.FromJson<VersionFile>(File.ReadAllText(path));
            if (file == null || string.IsNullOrWhiteSpace(file.version)) return;
            if (PlayerSettings.bundleVersion == file.version) return;
            PlayerSettings.bundleVersion = file.version;
            Debug.Log($"[What Lies In The Depths] Version set to {file.version} from version.json.");
        }
    }
}
