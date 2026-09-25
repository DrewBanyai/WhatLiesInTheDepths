// What Lies In The Depths — a full-resolution capture of the Game view, for comparing a
// surface against its spec pixel for pixel. Writes to <project>/Captures, outside Assets.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class CaptureGameView
    {
        [MenuItem("Tools/What Lies In The Depths/Capture Game View %#F12", priority = 60)]
        static void Capture() => CaptureNow();

        /// <summary>For hover states: pick this, then put the pointer back where it was
        /// wanted. The capture fires fifteen seconds later.</summary>
        [MenuItem("Tools/What Lies In The Depths/Capture Game View in 15 Seconds", priority = 61)]
        static void CaptureDelayed()
        {
            double at = EditorApplication.timeSinceStartup + 15.0;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.timeSinceStartup < at) return;
                EditorApplication.update -= tick;
                CaptureNow();
            };
            EditorApplication.update += tick;
            Debug.Log("[What Lies In The Depths] Capturing the Game view in 15 seconds.");
        }

        static void CaptureNow()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Captures");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "game_" + DateTime.Now.ToString("HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[What Lies In The Depths] Game view captured to " + path);
        }
    }
}
