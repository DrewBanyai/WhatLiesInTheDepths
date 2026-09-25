// Ursine — a bar that eases to a new width and color rather than snapping.
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
    /// <summary>A bar that eases to its new width and color rather than snapping, in either
    /// direction. A value going backwards is often correct — a ceiling rose — and it should
    /// read as a movement rather than as a glitch.</summary>
    public sealed class FillBar : MonoBehaviour
    {
        public RectTransform fill;
        public Image fillImage;

        [Tooltip("Seconds for the ease. A fifth of a second reads as movement without lag.")]
        public float slideSeconds = 0.22f;

        float _target, _shown, _vel;
        Color _targetColor = Color.clear, _shownColor = Color.clear;
        float _width = 100f;

        public void SetWidth(float w) => _width = w;

        public void Set(float normalized, Color color, bool immediate = false)
        {
            _target = Mathf.Clamp01(normalized);
            _targetColor = color;
            if (!immediate) return;
            _shown = _target;
            _shownColor = color;
            Push();
        }

        void Update()
        {
            if (Mathf.Approximately(_shown, _target) && _shownColor == _targetColor) return;
            _shown = Mathf.SmoothDamp(_shown, _target, ref _vel, slideSeconds * 0.5f);
            if (Mathf.Abs(_shown - _target) < 0.0005f) _shown = _target;
            _shownColor = Color.Lerp(_shownColor, _targetColor,
                                     1f - Mathf.Exp(-UnityEngine.Time.deltaTime / (slideSeconds * 0.4f)));
            Push();
        }

        void Push()
        {
            if (fill != null) fill.sizeDelta = new Vector2(_width * _shown, fill.sizeDelta.y);
            if (fillImage != null) fillImage.color = _shownColor;
        }
    }
}
