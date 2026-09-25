// Ursine — a bar that is set rather than filled; the one place a bar asks.
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
    /// <summary>A bar that is set rather than filled — the one place a bar asks instead of
    /// reporting. Takes effect on the frame of the drag, not on a later Apply.</summary>
    public sealed class VolumeBar : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public RectTransform track;
        public RectTransform fill;
        public RectTransform knob;
        public float width = 240f;

        public event Action<float> Changed;

        /// <summary>Where it starts. Serialized, so a builder that draws the fill and the knob
        /// at a value does not also have to hope the component agrees with it.</summary>
        [Range(0f, 1f)] public float level = 0.7f;

        public float Value => level;

        void Awake() => Set(level, false);

        public void Set(float v, bool notify)
        {
            level = Mathf.Clamp01(v);
            if (fill != null) fill.sizeDelta = new Vector2(width * level, fill.sizeDelta.y);
            if (knob != null) knob.anchoredPosition = new Vector2(width * level, knob.anchoredPosition.y);
            if (notify) Changed?.Invoke(level);
        }

        public void OnPointerDown(PointerEventData e) => Drag(e);
        public void OnDrag(PointerEventData e) => Drag(e);

        void Drag(PointerEventData e)
        {
            if (track == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    track, e.position, e.pressEventCamera, out var local)) return;
            Set(local.x / Mathf.Max(1f, width), true);
        }
    }
}
