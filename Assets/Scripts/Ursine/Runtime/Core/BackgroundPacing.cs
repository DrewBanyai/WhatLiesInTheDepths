// Ursine — keep an incremental game running when it is not in front, without letting it
// spin a core flat out while nobody can see it.
//
// Run In Background is switched on, so losing focus no longer pauses anything. While the
// window is minimized, frames are paced down to a low rate instead: a minimized window has
// no screen to sync to, so vsync stops holding it back and it would otherwise draw as fast as
// the machine allows. The clock is unaffected either way; ticks still come once a second.
// Restoring the window restores the frame pacing it had before.
//
// Where it applies:
//   Windows desktop — paced down while minimized (asked of Windows directly).
//   Other desktops  — paced down while unfocused, the nearest thing they report.
//   Web             — nothing to do: an unfocused tab keeps running, and a hidden tab is
//                     stopped by the browser itself, which no game can override.
//   The Editor      — Run In Background only; the Editor's own pacing is left alone.
using System;
using UnityEngine;

namespace Ursine
{
    [DisallowMultipleComponent]
    public sealed class BackgroundPacing : MonoBehaviour
    {
        public static BackgroundPacing I { get; private set; }

        [Tooltip("Frames per second while the window is minimized.")]
        public int backgroundFps = 10;

        bool _slowed;
        int _savedVSync, _savedTarget;

        /// <summary>Switches Run In Background on and paces a minimized window at
        /// <paramref name="fps"/>. Safe to call more than once.</summary>
        public static BackgroundPacing Install(int fps = 10)
        {
            Application.runInBackground = true;
            if (I != null) { I.backgroundFps = fps; return I; }
            var go = new GameObject("[Ursine] Background Pacing");
            DontDestroyOnLoad(go);
            var p = go.AddComponent<BackgroundPacing>();
            p.backgroundFps = fps;
            return p;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            Application.runInBackground = true;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // The game's own window. At startup it is the active one.
            _hwnd = GetActiveWindow();
#endif
        }

        void OnDestroy()
        {
            if (I != this) return;
            Pace(false);
            I = null;
        }

        void Update() => Pace(ShouldSlow());

        bool ShouldSlow()
        {
#if UNITY_EDITOR || UNITY_WEBGL
            return false;
#elif UNITY_STANDALONE_WIN
            if (_hwnd == IntPtr.Zero && Application.isFocused) _hwnd = GetActiveWindow();
            return _hwnd != IntPtr.Zero ? IsIconic(_hwnd) : !Application.isFocused;
#elif UNITY_STANDALONE
            return !Application.isFocused;
#else
            return false;   // phones and consoles pause the app themselves
#endif
        }

        void Pace(bool slow)
        {
            if (slow == _slowed) return;
            _slowed = slow;
            if (slow)
            {
                _savedVSync = QualitySettings.vSyncCount;
                _savedTarget = Application.targetFrameRate;
                // A target frame rate is ignored while vsync is on.
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.Max(1, backgroundFps);
            }
            else
            {
                QualitySettings.vSyncCount = _savedVSync;
                Application.targetFrameRate = _savedTarget;
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr _hwnd;
        [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
        [System.Runtime.InteropServices.DllImport("user32.dll")] [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);
#endif
    }
}
