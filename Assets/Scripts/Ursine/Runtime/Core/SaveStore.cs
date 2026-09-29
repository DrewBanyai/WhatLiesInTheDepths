// Ursine — where a save lives on each platform.
//
// Windows, Android and every other native build: a file in Application.persistentDataPath
// (on Windows %USERPROFILE%\AppData\LocalLow\<company>\<product>, on Android the app's own
// internal storage). It is written to a temporary file first and then moved into place, and
// the previous save is kept beside it as .bak, so a crash or a power cut in the middle of a
// save leaves the last good one behind rather than half a file.
//
// Web: the save goes into the browser's localStorage under a fixed key, <WebPrefix><slot>, with
// the previous save beside it as <key>.bak (WebBridge / UrsineWeb.jslib). Not PlayerPrefs:
// a WebGL build keeps PlayerPrefs in IndexedDB under a folder named for the page's address, and
// hosts such as itch.io give every upload a new address, so every new upload started from
// nothing. A save still sitting in PlayerPrefs from an older build is read once and carried
// over. If the browser refuses localStorage (storage switched off), PlayerPrefs is used instead.
//
// Ursine does not know what is in a save; it stores and returns text under a slot name.
using System;
using System.IO;
using UnityEngine;

namespace Ursine
{
    public static class SaveStore
    {
        /// <summary>Raised with a message when a save could not be written or read.</summary>
        public static Action<string> Warn = m => Debug.LogWarning(m);

        static bool UseWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>What every web save key starts with. The product name by default, so two
        /// games on the same site never share a key.</summary>
        public static string WebPrefix = Application.productName + ".";

        static string WebKey(string slot) => WebPrefix + slot;
        static string PrefsKey(string slot) => "save." + slot;
        static string FilePath(string slot) => Path.Combine(Application.persistentDataPath, slot + ".json");

        public static bool Write(string slot, string text)
        {
            try
            {
                if (UseWeb)
                {
                    string key = WebKey(slot);
                    string old = WebBridge.Get(key);
                    if (!string.IsNullOrEmpty(old) && old != text) WebBridge.Set(key + ".bak", old);
                    if (WebBridge.Set(key, text)) return true;
                    // No localStorage: the old way is better than none.
                    PlayerPrefs.SetString(PrefsKey(slot), text);
                    PlayerPrefs.Save();
                    return true;
                }

                string path = FilePath(slot);
                string tmp = path + ".tmp";
                string bak = path + ".bak";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(tmp, text);
                if (File.Exists(path))
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(path, bak);
                }
                File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Warn?.Invoke("[Ursine] Could not write the save: " + e.Message);
                return false;
            }
        }

        /// <summary>The saved text, or null when there is none. Falls back to the previous save
        /// if the current one is missing (a save interrupted between its two moves).</summary>
        public static string Read(string slot)
        {
            try
            {
                if (UseWeb)
                {
                    string key = WebKey(slot);
                    string v = WebBridge.Get(key);
                    if (!string.IsNullOrEmpty(v)) return v;
                    // A save made by an older build, in PlayerPrefs: carry it across.
                    if (PlayerPrefs.HasKey(PrefsKey(slot)))
                    {
                        v = PlayerPrefs.GetString(PrefsKey(slot));
                        if (!string.IsNullOrEmpty(v)) WebBridge.Set(key, v);
                        return v;
                    }
                    return WebBridge.Get(key + ".bak");
                }

                string path = FilePath(slot);
                if (File.Exists(path)) return File.ReadAllText(path);
                if (File.Exists(path + ".bak")) return File.ReadAllText(path + ".bak");
                return null;
            }
            catch (Exception e)
            {
                Warn?.Invoke("[Ursine] Could not read the save: " + e.Message);
                return null;
            }
        }

        /// <summary>The previous save, for when the current one will not load.</summary>
        public static string ReadBackup(string slot)
        {
            if (UseWeb) return WebBridge.Get(WebKey(slot) + ".bak");
            try
            {
                string bak = FilePath(slot) + ".bak";
                return File.Exists(bak) ? File.ReadAllText(bak) : null;
            }
            catch { return null; }
        }

        public static bool Exists(string slot)
            => UseWeb ? !string.IsNullOrEmpty(WebBridge.Get(WebKey(slot))) || PlayerPrefs.HasKey(PrefsKey(slot))
                      : File.Exists(FilePath(slot)) || File.Exists(FilePath(slot) + ".bak");

        public static void Delete(string slot)
        {
            try
            {
                if (UseWeb)
                {
                    WebBridge.Remove(WebKey(slot));
                    WebBridge.Remove(WebKey(slot) + ".bak");
                    PlayerPrefs.DeleteKey(PrefsKey(slot));
                    PlayerPrefs.Save();
                    return;
                }
                foreach (var p in new[] { FilePath(slot), FilePath(slot) + ".bak", FilePath(slot) + ".tmp" })
                    if (File.Exists(p)) File.Delete(p);
            }
            catch (Exception e) { Warn?.Invoke("[Ursine] Could not delete the save: " + e.Message); }
        }

        /// <summary>Where the save is, for a log line.</summary>
        public static string Describe(string slot)
            => UseWeb ? "browser storage (localStorage '" + WebKey(slot) + "')" : FilePath(slot);
    }
}
