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
    /// scales a 20:9 phone up by the extra width and takes the top and bottom off the design.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class FixedStage : MonoBehaviour
    {
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Tooltip("Off: fit the whole design on screen, whatever its shape. On: blend the two " +
                 "axes by Match, which crops whichever axis is short.")]
        public bool blendAxes;

        [Range(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        void OnEnable() { Apply(); }

#if UNITY_EDITOR
        void OnValidate() { Apply(); }
#endif

        public void Apply()
        {
            var s = GetComponent<CanvasScaler>();
            if (s == null) return;
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = referenceResolution;
            s.screenMatchMode = blendAxes
                ? CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                : CanvasScaler.ScreenMatchMode.Expand;
            s.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
