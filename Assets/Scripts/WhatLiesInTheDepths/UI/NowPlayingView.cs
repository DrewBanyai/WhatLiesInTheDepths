// What Lies In The Depths — the foot of the left column: what is playing.
//
// The quietest readout on the screen and the only one that is not about the dream. It says
// one thing, in the reserved foot under the ledger, and it says it small: a caption in the
// same small caps as every other caption, and the track's own name under it.
//
// Nothing here decides anything about the music. The Jukebox picks and dissolves; this
// listens and writes the name down.
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using Ursine.Audio;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class NowPlayingView : MonoBehaviour
    {
        public Marquee marquee;
        public TMP_Text caption;
        public CanvasGroup group;

        /// <summary>Seconds the line takes to appear, once there is something to say.</summary>
        const float Arrival = 1.5f;

        Jukebox _box;

        void OnEnable()
        {
            Bind();
            Show(_box != null ? _box.Playing : null);
        }

        void OnDisable()
        {
            if (_box != null) _box.Changed -= Show;
            _box = null;
        }

        void Update()
        {
            // The jukebox may not have picked its first track yet on the frame this switches
            // on, and with no music at all there is nothing to say and nothing to show.
            if (_box == null) { Bind(); if (_box != null) Show(_box.Playing); }
            if (group == null) return;

            // It arrives the way the first track does: not switched on, faded up, over about
            // the time the music takes to come out of silence.
            float want = _box != null && _box.Playing != null ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, want, Time.unscaledDeltaTime / Arrival);
        }

        void Bind()
        {
            if (_box != null || Jukebox.I == null) return;
            _box = Jukebox.I;
            _box.Changed += Show;
        }

        void Show(AudioClip clip)
        {
            if (marquee != null) marquee.Show(Title(clip));
        }

        /// <summary>A track's name is its file name. The one liberty taken with it is the
        /// dash: a file cannot hold an em dash, and the line is set in a face that has one.</summary>
        static string Title(AudioClip clip)
            => clip == null ? string.Empty : clip.name.Replace(" - ", " — ");
    }
}
