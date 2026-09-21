// Ursine — a plain track-and-fill readout.
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
    /// <summary>A plain track-and-fill readout.</summary>
    public sealed class ProgressTrack : MonoBehaviour
    {
        public RectTransform fill;
        public float width = 100f;

        public void Set(float normalised)
        {
            if (fill == null) return;
            fill.sizeDelta = new Vector2(width * Mathf.Clamp01(normalised), fill.sizeDelta.y);
        }
    }
}
