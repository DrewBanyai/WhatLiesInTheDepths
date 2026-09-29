// Ursine — the C# side of UrsineWeb.jslib: browser storage, a download and a file picker, for
// a WebGL build. Everywhere else every call reports that it is unavailable, so callers can fall
// back to what their platform has instead.
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ursine
{
    public static class WebBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern string UrsineStorageGet(string key);   // Unity frees the buffer
        [DllImport("__Internal")] static extern int UrsineStorageSet(string key, string value);
        [DllImport("__Internal")] static extern int UrsineStorageRemove(string key);
        [DllImport("__Internal")] static extern int UrsineDownload(string name, string text);
        [DllImport("__Internal")] static extern int UrsinePickFile(string obj, string method, string accept);
        public static bool Available => true;
#else
        public static bool Available => false;
#endif

        /// <summary>The value stored under <paramref name="key"/>, or null (none, or no storage).</summary>
        public static string Get(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return UrsineStorageGet(key); } catch { return null; }
#else
            return null;
#endif
        }

        /// <summary>Stores a value. False when the browser refused (storage off, full, or private).</summary>
        public static bool Set(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return UrsineStorageSet(key, value ?? string.Empty) == 1; } catch { return false; }
#else
            return false;
#endif
        }

        public static bool Remove(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return UrsineStorageRemove(key) == 1; } catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Offers <paramref name="text"/> to the player as a file download.</summary>
        public static bool Download(string fileName, string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return UrsineDownload(fileName, text ?? string.Empty) == 1; } catch { return false; }
#else
            return false;
#endif
        }

        /// <summary>Opens the browser's file picker; the chosen file's text (or "" for none) is
        /// sent to <paramref name="method"/> on the GameObject named <paramref name="receiver"/>.</summary>
        public static bool PickFile(string receiver, string method, string accept)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { return UrsinePickFile(receiver, method, accept ?? string.Empty) == 1; } catch { return false; }
#else
            return false;
#endif
        }
    }
}
