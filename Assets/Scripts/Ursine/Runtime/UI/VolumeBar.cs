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

        float _v = 0.7f;
        public float Value => _v;

        public void Set(float v, bool notify)
        {
            _v = Mathf.Clamp01(v);
            if (fill != null) fill.sizeDelta = new Vector2(width * _v, fill.sizeDelta.y);
            if (knob != null) knob.anchoredPosition = new Vector2(width * _v, knob.anchoredPosition.y);
            if (notify) Changed?.Invoke(_v);
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
