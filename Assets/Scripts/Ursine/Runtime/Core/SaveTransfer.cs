// Ursine — a save the player can hold: export it to a file, import one back.
//
// Web: Export downloads the file; Import opens the browser's file picker (UrsineWeb.jslib).
// Editor: the Editor's own save and open dialogs.
// Desktop builds: Export writes the file to the player's Downloads folder (the save folder's
// Exports folder if there is none), and Import reads back the newest file there whose name
// starts with the export prefix — a desktop build has no file picker of its own to ask with.
//
// Ursine does not know what is in a save. It moves text; the game decides whether it is one.
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ursine
{
    public sealed class SaveTransfer : MonoBehaviour
    {
        /// <summary>Where a desktop build's exports go and where its imports are looked for.</summary>
        public static string Folder
        {
            get
            {
                try
                {
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string downloads = string.IsNullOrEmpty(home) ? null : Path.Combine(home, "Downloads");
                    if (!string.IsNullOrEmpty(downloads) && Directory.Exists(downloads)) return downloads;
                }
                catch { }
                return Path.Combine(Application.persistentDataPath, "Exports");
            }
        }

        /// <summary>A short name for <see cref="Folder"/>, for a line of UI.</summary>
        public static string FolderName => Path.GetFileName(Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        public enum Outcome { Done, Cancelled, NoneFound, Failed }

        /// <summary>Hands <paramref name="text"/> to the player as <paramref name="fileName"/>.
        /// <paramref name="where"/> is the path written (desktop, Editor) or null (web).</summary>
        public static Outcome Export(string fileName, string text, out string where)
        {
            where = null;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return WebBridge.Download(fileName, text) ? Outcome.Done : Outcome.Failed;
            try
            {
#if UNITY_EDITOR
                string path = UnityEditor.EditorUtility.SaveFilePanel("Export the save", Folder, fileName, "json");
                if (string.IsNullOrEmpty(path)) return Outcome.Cancelled;
#else
                Directory.CreateDirectory(Folder);
                string path = Path.Combine(Folder, fileName);
#endif
                File.WriteAllText(path, text);
                where = path;
                return Outcome.Done;
            }
            catch (Exception e)
            {
                SaveStore.Warn?.Invoke("[Ursine] Could not export the save: " + e.Message);
                return Outcome.Failed;
            }
        }

        static SaveTransfer _receiver;
        Action<Outcome, string, string> _pending;

        /// <summary>Asks the player for a save. <paramref name="done"/> gets the outcome, the
        /// file's text (when Done) and where it came from (a path, or null on the web).</summary>
        public static void Import(string prefix, Action<Outcome, string, string> done)
        {
            if (done == null) return;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (_receiver == null)
                {
                    var go = new GameObject("[Ursine] Save Transfer");
                    DontDestroyOnLoad(go);
                    _receiver = go.AddComponent<SaveTransfer>();
                }
                _receiver._pending = done;
                if (!WebBridge.PickFile(_receiver.gameObject.name, nameof(OnPicked), ".json,application/json,text/plain"))
                {
                    _receiver._pending = null;
                    done(Outcome.Failed, null, null);
                }
                return;
            }
            try
            {
#if UNITY_EDITOR
                string path = UnityEditor.EditorUtility.OpenFilePanel("Import a save", Folder, "json");
                if (string.IsNullOrEmpty(path)) { done(Outcome.Cancelled, null, null); return; }
#else
                string path = null;
                if (Directory.Exists(Folder))
                    path = new DirectoryInfo(Folder).GetFiles(prefix + "*.json")
                        .OrderByDescending(f => f.LastWriteTimeUtc).Select(f => f.FullName).FirstOrDefault();
                if (path == null) { done(Outcome.NoneFound, null, Folder); return; }
#endif
                done(Outcome.Done, File.ReadAllText(path), path);
            }
            catch (Exception e)
            {
                SaveStore.Warn?.Invoke("[Ursine] Could not import a save: " + e.Message);
                done(Outcome.Failed, null, null);
            }
        }

        // From the page, through SendMessage: the chosen file's text, or "" for none.
        void OnPicked(string text)
        {
            var d = _pending;
            _pending = null;
            if (d == null) return;
            if (string.IsNullOrEmpty(text)) d(Outcome.Cancelled, null, null);
            else d(Outcome.Done, text, null);
        }
    }
}
