// Ursine — swaps a graphic's colour between rest, hover and active tokens.
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
    /// <summary>Swaps a graphic's colour between rest, hover and active tokens.</summary>
    public sealed class TintOnHover : MonoBehaviour
    {
        public Graphic target;
        public int restToken;
        public int hoverToken;
        public int activeToken;

        bool _hovered, _active;

        void OnEnable() { Theme.Changed += Apply; Apply(); }
        void OnDisable() => Theme.Changed -= Apply;

        public void Bind(UiButton b)
        {
            if (b == null) return;
            b.Hovered += h => { _hovered = h; Apply(); };
        }

        public void SetActive(bool on) { _active = on; Apply(); }

        public void Apply()
        {
            if (target == null) return;
            target.color = Theme.Get(_active ? activeToken : (_hovered ? hoverToken : restToken));
        }
    }
}
