// What Lies In The Depths — what decides which shape the stage is in.
//
// The stage itself cannot decide this. It knows the glass, not what is being drawn on it,
// which is why FixedStage takes a design and this hands it one. Kept on the screen root
// beside the clocks, because choosing a layout is a fact about this game's screen rather
// than a service any game would want.
using System;
using WhatLiesInTheDepths.Core;
using UnityEngine;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>A panel whose composition cannot simply be made shorter, and which therefore
    /// has to be told. Most things do not need this: a scroller that is anchored to its
    /// column's floor is already as tall as the column, and the gauge crops itself to the
    /// track every frame. This is for the handful that have to be rearranged.</summary>
    public interface IStageFit
    {
        void Fit(StageProfile profile);
    }

    [DisallowMultipleComponent]
    public sealed class StageDirector : MonoBehaviour
    {
        public static StageDirector I { get; private set; }

        /// <summary>After the shape changes, never during. Subscribers unsubscribe in
        /// OnDisable, as everything subscribing to a static event in this project does.</summary>
        public static event Action<StageProfile> Changed;

        public StageProfile Profile { get; private set; }
        public StageMode Mode { get; private set; } = StageMode.Auto;

        ScreenRoot _screen;
        int _w = -1, _h = -1;

        void Awake()
        {
            I = this;
            _screen = GetComponent<ScreenRoot>();
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Auto, or the player overruling it from Options. Applied at once, because
        /// every control on that page is applied at once.</summary>
        public void SetMode(StageMode mode)
        {
            Mode = mode;
            Evaluate(true);
        }

        void Start()
        {
            // Settings may or may not have been read yet — GameState reads them in its own
            // Start — so take whatever is there and let the read apply the saved one after.
            SetMode(GameSettings.LayoutMode);
        }

        void Update()
        {
            // A window being dragged changes size on a great many frames; the work is only
            // done when the answer changes, and the answer only changes at the breakpoint.
            if (Screen.width == _w && Screen.height == _h) return;
            Evaluate(false);
        }

        void Evaluate(bool force)
        {
            _w = Screen.width;
            _h = Screen.height;

            // Deliberately the raw fit, not the fit times the player's own scale: scaling the
            // stage down is someone asking for a smaller picture, and it should not be read as
            // the screen having got smaller and answered by taking a column away from them.
            var next = StageProfile.Choose(Mode, Profile);
            if (!force && ReferenceEquals(next, Profile)) return;

            Profile = next;
            StageProfile.MarkCurrent(next);
            if (FixedStage.I != null) FixedStage.I.SetReference(next.Size);
            if (_screen != null) _screen.ApplyProfile(next);
            Changed?.Invoke(next);
        }
    }
}
