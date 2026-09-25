// Ursine — the music: one track at a time, chosen at random, dissolving into the next.
//
// Two sources and never fewer. A single AudioSource cannot cross anything: the only way one
// piece of music can be heard under another is for two to be playing, so the whole component
// is two sources taking it in turns, one rising while the other falls.
//
// Nothing here knows what game it is in. What it is given — which tracks, how long the
// dissolve, how loud — is the caller's business.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ursine.Audio
{
    /// <summary>Plays a set of tracks endlessly, in an order that is random but never
    /// repetitive, each one dissolving into the next.</summary>
    [DisallowMultipleComponent]
    public sealed class Jukebox : MonoBehaviour
    {
        public static Jukebox I { get; private set; }

        public List<AudioClip> tracks = new List<AudioClip>();

        /// <summary>Seconds of overlap. The outgoing track begins fading this long before its
        /// end, so the two are heard together for the whole of it — and the first track of a
        /// session fades up out of silence over the same span.</summary>
        public float crossfadeSeconds = 6f;

        /// <summary>How many of the most recently played tracks cannot be picked again. 2 means
        /// neither what is playing nor what played before it, so the next one is always a third
        /// thing. Relaxed on its own when the set is too small to honour it.</summary>
        public int avoidLast = 2;

        /// <summary>The two sources, handed in rather than made, so a prefab shows them.</summary>
        public AudioSource a;
        public AudioSource b;

        /// <summary>What the player asked for, 0..1, before any fade is applied.</summary>
        [Range(0f, 1f)] public float volume = 1f;
        public bool muted;

        public AudioClip Playing => _near != null ? _near.clip : null;

        /// <summary>The track's own name, which for an imported file is its file name.</summary>
        public string PlayingName => Playing != null ? Playing.name : string.Empty;

        /// <summary>Where the current track has got to, and how long it is. For a debugger:
        /// nothing in the game asks.</summary>
        public float Position => _near != null && _near.clip != null ? _near.time : 0f;
        public float Length => _near != null && _near.clip != null ? _near.clip.length : 0f;
        public bool Dissolving => _dissolving;

        /// <summary>What may not be picked next, oldest first.</summary>
        public IReadOnlyList<AudioClip> Recent => _recent;

        /// <summary>Raised the moment a track starts — that is, when it begins fading up, not
        /// when it finishes. A readout should change with the first sound of the new track.</summary>
        public event Action<AudioClip> Changed;

        readonly List<AudioClip> _recent = new List<AudioClip>();
        AudioSource _near, _far;
        bool _dissolving;
        float _t;

        float Level => muted ? 0f : Mathf.Clamp01(volume);

        void Awake()
        {
            I = this;
            Ready(a);
            Ready(b);
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Music is not in the room: no attenuation, no doppler, no panning by where
        /// the listener happens to be standing.</summary>
        static void Ready(AudioSource s)
        {
            if (s == null) return;
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.dopplerLevel = 0f;
            s.volume = 0f;
        }

        void Update()
        {
            if (a == null || b == null || !Any()) return;

            if (_near == null || _near.clip == null) { Begin(Pick()); return; }

            if (_dissolving)
            {
                _t += Time.unscaledDeltaTime;
                float k = crossfadeSeconds <= 0f ? 1f : Mathf.Clamp01(_t / crossfadeSeconds);
                _near.volume = Level * k;
                if (_far != null) _far.volume = Level * (1f - k);
                if (k >= 1f) Settle();
                return;
            }

            _near.volume = Level;

            // The hand-over is scheduled by what is left, not by a timer, so a track that was
            // started part-way through still ends when it ends.
            if (Left(_near) <= crossfadeSeconds) Begin(Pick());
        }

        /// <summary>Starts a track on the idle source and fades the pair. The one that was
        /// playing is not stopped here: it is still being heard.</summary>
        public void Begin(AudioClip clip)
        {
            if (clip == null) return;

            _far = _near;
            _near = _near == a ? b : a;
            _near.clip = clip;
            _near.time = 0f;
            _near.volume = 0f;
            _near.Play();

            _dissolving = crossfadeSeconds > 0f;
            _t = 0f;
            if (!_dissolving) { _near.volume = Level; Settle(); }

            Remember(clip);
            Changed?.Invoke(clip);
        }

        /// <summary>Straight to the next track, dissolving as usual.</summary>
        public void Skip() => Begin(Pick());

        /// <summary>Moves the needle. The hand-over is scheduled by what is left to play, so
        /// dropping the needle near the end is how the real thing is watched happening rather
        /// than waiting ten minutes for it.</summary>
        public void Seek(float seconds)
        {
            if (_near == null || _near.clip == null) return;
            _near.time = Mathf.Clamp(seconds, 0f, Mathf.Max(0f, _near.clip.length - 0.05f));
        }

        public void SetVolume(float v, bool mute)
        {
            volume = Mathf.Clamp01(v);
            muted = mute;
            if (!_dissolving && _near != null) _near.volume = Level;
        }

        void Settle()
        {
            _dissolving = false;
            if (_near != null) _near.volume = Level;
            if (_far == null) return;
            _far.Stop();
            _far.clip = null;
            _far.volume = 0f;
            _far = null;
        }

        static float Left(AudioSource s)
            => s == null || s.clip == null ? 0f : Mathf.Max(0f, s.clip.length - s.time);

        bool Any()
        {
            foreach (var c in tracks) if (c != null) return true;
            return false;
        }

        /// <summary>Anything but the last few. With nothing left to choose from — a short set,
        /// or one track — the rule loosens a step at a time rather than failing.</summary>
        AudioClip Pick()
        {
            var pool = new List<AudioClip>();
            foreach (var c in tracks) if (c != null && !_recent.Contains(c)) pool.Add(c);

            if (pool.Count == 0)
                foreach (var c in tracks) if (c != null && c != Playing) pool.Add(c);

            if (pool.Count == 0)
                foreach (var c in tracks) if (c != null) pool.Add(c);

            return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        void Remember(AudioClip clip)
        {
            _recent.Add(clip);
            int keep = Mathf.Max(0, avoidLast);
            while (_recent.Count > keep) _recent.RemoveAt(0);
        }
    }
}
