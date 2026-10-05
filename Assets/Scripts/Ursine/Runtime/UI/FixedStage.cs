// Ursine — guards a canvas at a fixed design resolution, so one design pixel is one UI unit.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ursine.Economy;
using Ursine.Theming;

namespace Ursine.UI
{
    /// <summary>Guards a canvas at a fixed design resolution, so every number in a design
    /// document is one UI unit. Extra width goes to the margins; the layout does not reflow.
    ///
    /// The whole stage is kept on screen whatever the display's shape: the scale is the smaller
    /// of the two axes' ratios, so a wider screen gains margins and a taller one gains bands,
    /// and nothing is ever cropped. Blending the two ratios instead — the scaler's default —
    /// scales a 20:9 phone up by the extra width and takes the top and bottom off the design.
    ///
    /// The player may take the stage DOWN from that fit, and only down. The fit is already the
    /// largest the whole design can be drawn at without losing an edge: raising the scale shrinks
    /// the canvas on both axes at once, so the margin a wide screen has to spare is never on the
    /// axis that would pay for it. A big monitor draws this interface very large, though, and
    /// wanting it smaller and closer to hand is an ordinary thing to want.
    ///
    /// A game may have more than one design — a shorter, narrower one for a small window — in
    /// which case it hands the stage the design it is currently drawing through SetReference.
    /// Which design to use is not a question a stage can answer: it knows the glass, not what
    /// is being drawn on it.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class FixedStage : MonoBehaviour
    {
        /// <summary>The design size. One number in a design document, one UI unit — this is
        /// the thing the whole project is drawn against, and the player's scale never moves it.</summary>
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        /// <summary>The smallest fraction of the fit the stage may be taken to.</summary>
        public const float MinScale = 0.6f;

        [Range(MinScale, 1f)]
        [Tooltip("The player's size for the stage, as a fraction of what fits. Never above 1: " +
                 "the fit is the largest the whole design can be drawn without losing an edge.")]
        public float userScale = 1f;

        /// <summary>The one stage, so whatever keeps the player's settings can reach it without
        /// hunting for it. Set in Awake, so anything running from Start can count on it.</summary>
        public static FixedStage I { get; private set; }

        [Tooltip("Off: fit the whole design on screen, whatever its shape. On: blend the two " +
                 "axes by Match, which crops whichever axis is short.")]
        public bool blendAxes;

        [Range(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        /// <summary>What one unit of a given design would be worth in screen pixels right
        /// now — the fit, before the player's own scale. Whatever decides between two designs
        /// asks this of each of them.</summary>
        public static float FitFor(Vector2 design)
            => Mathf.Min(Screen.width / Mathf.Max(1f, design.x),
                         Screen.height / Mathf.Max(1f, design.y));

        /// <summary>Raised after the design changes, so anything holding a measurement taken
        /// against the old one can take it again.</summary>
        public event Action<Vector2> ReferenceChanged;

        void Awake() { I = this; }

        void OnDestroy() { if (I == this) I = null; }

        void OnEnable() { Apply(); }

        /// <summary>Takes effect on the frame of the drag, like every other control on the
        /// Options page. Keeping it between sessions is the caller's business: a stage does not
        /// know where a game puts what belongs to the player.</summary>
        public void SetUserScale(float v)
        {
            userScale = Mathf.Clamp(v, MinScale, 1f);
            Apply();
        }

        /// <summary>Swaps the design the stage is drawn against. The player's scale rides on
        /// top of it and is not disturbed: it is a fraction of whatever fits, so it means the
        /// same thing in either design.</summary>
        public void SetReference(Vector2 design)
        {
            if (design.x < 1f || design.y < 1f) return;
            if (referenceResolution == design) return;
            referenceResolution = design;
            Apply();
            ReferenceChanged?.Invoke(design);
        }

#if UNITY_EDITOR
        void OnValidate() { Apply(); }
#endif

        public void Apply()
        {
            var s = GetComponent<CanvasScaler>();
            if (s == null) return;
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // A larger reference is a smaller stage: the scaler divides the screen by this, so
            // asking it to fit a bigger design into the same glass is the same arithmetic as
            // drawing the real design smaller, and Expand keeps doing the fitting.
            s.referenceResolution = referenceResolution / Mathf.Clamp(userScale, MinScale, 1f);
            s.screenMatchMode = blendAxes
                ? CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                : CanvasScaler.ScreenMatchMode.Expand;
            s.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
