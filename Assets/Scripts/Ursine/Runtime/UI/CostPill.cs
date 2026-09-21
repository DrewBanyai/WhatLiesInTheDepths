// Ursine — a glyph, an amount and a resource name, tinted by whether it can be paid.
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
    /// <summary>A glyph, an amount and a resource name, tinted by whether it can be paid.</summary>
    public sealed class CostPill : MonoBehaviour
    {
        public Image ground;
        [Tooltip("Optional. Drawn only when the pill refuses.")]
        public Image border;
        public Image glyph;
        public TMP_Text amount;
        public TMP_Text label;

        [Header("Tokens — payable")]
        public int payableGroundToken;
        public int payableInkToken;
        [Tooltip("Payable pills usually want no ground at all.")]
        public bool payableGroundTransparent = true;

        [Header("Tokens — short of a resource")]
        public int shortGroundToken;
        public int shortInkToken;
        public int shortBorderToken;

        [Header("Tokens — above a ceiling")]
        public int overGroundToken;
        public int overInkToken;
        public int overBorderToken;

        public void Set(string resourceName, double n, Refusal state)
        {
            if (amount != null) amount.text = Fmt.Count(n);
            if (label != null) label.text = resourceName;

            int g, ink;
            bool transparent = false;
            switch (state)
            {
                case Refusal.Short: g = shortGroundToken; ink = shortInkToken; break;
                case Refusal.AboveCeiling: g = overGroundToken; ink = overInkToken; break;
                default: g = payableGroundToken; ink = payableInkToken; transparent = payableGroundTransparent; break;
            }

            if (ground != null) ground.color = Theme.Get(g, transparent ? 0f : 1f);
            if (amount != null) amount.color = Theme.Get(ink);
            if (label != null) label.color = Theme.Get(ink);
            if (glyph != null) glyph.color = Theme.Get(ink);
            if (border != null)
                border.color = state == Refusal.Short ? Theme.Get(shortBorderToken)
                             : state == Refusal.AboveCeiling ? Theme.Get(overBorderToken)
                             : Theme.Get(payableGroundToken, 0f);
        }

        /// <summary>Some surfaces name the resource with its glyph, some with words alone.</summary>
        public void ShowGlyph(bool show)
        {
            if (glyph != null) glyph.gameObject.SetActive(show);
        }
    }
}
