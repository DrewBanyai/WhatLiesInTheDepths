// Ursine — an autosave interval, and how stale the save currently is.
using System;
using UnityEngine;

namespace Ursine
{
    /// <summary>An autosave interval, and how stale the save currently is. Ursine does not
    /// save anything — it only keeps the clock and raises the event, because what a save
    /// contains is entirely the game's business.</summary>
    public sealed class SaveClock : MonoBehaviour
    {
        public static SaveClock I { get; private set; }

        [Tooltip("Seconds between autosaves.")]
        public float intervalSeconds = 120f;

        public event Action Saved;

        /// <summary>Seconds since the last autosave.</summary>
        public double Staleness { get; private set; }

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            Staleness += UnityEngine.Time.deltaTime;
            if (Staleness < intervalSeconds) return;
            Staleness = 0;
            Saved?.Invoke();
        }

        public void SaveNow()
        {
            Staleness = 0;
            Saved?.Invoke();
        }
    }
}
