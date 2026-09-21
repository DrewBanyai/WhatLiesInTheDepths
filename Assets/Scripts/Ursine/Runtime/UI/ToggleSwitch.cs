// Ursine — a switch: a mode the whole thing is in, as opposed to a checkbox.
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
    /// <summary>A switch. A checkbox modifies what is above it; a switch is a mode the whole
    /// thing is in. Keeping the two shapes apart is worth more than the pixels it costs.</summary>
    public sealed class ToggleSwitch : MonoBehaviour
    {
        public UiButton button;
        public Image track;
        public RectTransform knob;

        [Header("Tokens")]
        public int onTrackToken;
        public int offTrackToken;

        [Header("Knob travel")]
        public float knobOffX = 2f;
        public float knobOnX = 17f;

        public event Action<bool> Changed;

        bool _on;
        public bool On => _on;

        void Awake() { if (button != null) button.Clicked += () => Set(!_on, true); }
        void OnEnable() { Theme.Changed += Paint; Paint(); }
        void OnDisable() => Theme.Changed -= Paint;

        public void Set(bool on, bool notify)
        {
            _on = on;
            Paint();
            if (notify) Changed?.Invoke(_on);
        }

        void Paint()
        {
            if (track != null) track.color = Theme.Get(_on ? onTrackToken : offTrackToken);
            if (knob != null)
                knob.anchoredPosition = new Vector2(_on ? knobOnX : knobOffX, knob.anchoredPosition.y);
        }
    }
}
