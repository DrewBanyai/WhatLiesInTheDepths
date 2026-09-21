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
    /// document is one UI unit. Extra width goes to the margins; the layout does not reflow.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class FixedStage : MonoBehaviour
    {
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Range(0f, 1f)]
        public float matchWidthOrHeight = 0.5f;

        void OnEnable()
        {
            var s = GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = referenceResolution;
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = matchWidthOrHeight;
        }
    }
}
