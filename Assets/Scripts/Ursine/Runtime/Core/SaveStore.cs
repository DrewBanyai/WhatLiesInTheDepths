// Ursine — where a save lives on each platform.
//
// Windows, Android and every other native build: a file in Application.persistentDataPath
// (on Windows %USERPROFILE%\AppData\LocalLow\<company>\<product>, on Android the app's own
// internal storage). It is written to a temporary file first and then moved into place, and
// the previous save is kept beside it as .bak, so a crash or a power cut in the middle of a
// save leaves the last good one behind rather than half a file.
//
// Web: a browser page has no file system of its own that survives a reload, so the save goes
// into PlayerPrefs, which a WebGL build keeps in the browser's IndexedDB. PlayerPrefs.Save()
// is what actually commits it there.
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

        static bool UsePrefs => Application.platform == RuntimePlatform.WebGLPlayer;

        static string PrefsKey(string slot) => "save." + slot;
        static string FilePath(string slot) => Path.Combine(Application.persistentDataPath, slot + ".json");

        public static bool Write(string slot, string text)
        {
            try
            {
                if (UsePrefs)
                {
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
                if (UsePrefs)
                    return PlayerPrefs.HasKey(PrefsKey(slot)) ? PlayerPrefs.GetString(PrefsKey(slot)) : null;

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
            if (UsePrefs) return null;
            try
            {
                string bak = FilePath(slot) + ".bak";
                return File.Exists(bak) ? File.ReadAllText(bak) : null;
            }
            catch { return null; }
        }

        public static bool Exists(string slot)
            => UsePrefs ? PlayerPrefs.HasKey(PrefsKey(slot))
                        : File.Exists(FilePath(slot)) || File.Exists(FilePath(slot) + ".bak");

        public static void Delete(string slot)
        {
            try
            {
                if (UsePrefs) { PlayerPrefs.DeleteKey(PrefsKey(slot)); PlayerPrefs.Save(); return; }
                foreach (var p in new[] { FilePath(slot), FilePath(slot) + ".bak", FilePath(slot) + ".tmp" })
                    if (File.Exists(p)) File.Delete(p);
            }
            catch (Exception e) { Warn?.Invoke("[Ursine] Could not delete the save: " + e.Message); }
        }

        /// <summary>Where the save is, for a log line.</summary>
        public static string Describe(string slot)
            => UsePrefs ? "browser storage (PlayerPrefs '" + PrefsKey(slot) + "')" : FilePath(slot);
    }
}
