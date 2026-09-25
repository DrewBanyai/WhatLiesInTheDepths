// Ursine — the widgets an incremental game keeps needing.
//
// All mouse: hover, press and pointer enter/exit are the whole vocabulary, so nothing here
// is a Selectable and nothing sets up EventSystem navigation. A game that wants keyboard
// or gamepad support should not bolt it onto these.
//
// Palette tokens are int fields rather than baked-in constants, so the same widget can be
// dressed by whatever token set a project defines.
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
    /// <summary>A pointer target. Hover should be color only — no ground, no movement —
    /// wherever a separate mark means "this one is open", so the two can never be confused.</summary>
    [RequireComponent(typeof(Graphic))]
    public class UiButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public bool interactable = true;

        public event Action Clicked;
        public event Action<bool> Hovered;

        public bool IsHovered { get; private set; }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!interactable) return;
            IsHovered = true;
            Hovered?.Invoke(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            IsHovered = false;
            Hovered?.Invoke(false);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable || e.button != PointerEventData.InputButton.Left) return;
            Clicked?.Invoke();
        }

        public void SetInteractable(bool on)
        {
            interactable = on;
            if (!on && IsHovered) { IsHovered = false; Hovered?.Invoke(false); }
        }
    }
}
