// Ursine — the clocks an incremental game runs on.
//
// One heartbeat, and everything that accrues accrues on it. Nothing downstream may run an
// economy clock of its own: a second tick means two answers to "how much did I gain".
using System;
using UnityEngine;

namespace Ursine
{
    [DisallowMultipleComponent]
    public sealed class GameClock : MonoBehaviour
    {
        public static GameClock I { get; private set; }

        [Tooltip("Seconds between economy ticks. One second is the usual choice.")]
        public float tickSeconds = 1f;

        [Tooltip("How many ticks may be caught up in one frame after a stall, before the "
               + "remainder is dropped. Prevents a spiral when a window is restored.")]
        public int maxCatchUpTicks = 8;

        /// <summary>One whole tick of game time has passed.</summary>
        public event Action Tick;

        /// <summary>Every frame, for the things that are drawn rather than counted —
        /// drift, orbits, progress toward the next automatic action.</summary>
        public event Action<float> Frame;

        public double Elapsed { get; private set; }

        float _acc;

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            float dt = UnityEngine.Time.deltaTime;
            Elapsed += dt;
            Frame?.Invoke(dt);

            _acc += dt;
            int guard = 0;
            while (_acc >= tickSeconds && guard++ < maxCatchUpTicks)
            {
                _acc -= tickSeconds;
                Tick?.Invoke();
            }
            if (guard >= maxCatchUpTicks) _acc = 0f;
        }
    }
}
