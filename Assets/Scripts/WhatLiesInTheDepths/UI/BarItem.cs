// What Lies In The Depths — one item in a bar: a destination or a utility.
using System;
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>One item in a bar. A destination takes the underline when what it names is
    /// on screen and can carry a dot when it is not; a utility never takes either, except
    /// Options, which is the one utility that drives the center and therefore lights.</summary>
    public sealed class BarItem : MonoBehaviour
    {
        public BarItemKind kind = BarItemKind.Destination;
        public UiButton button;
        public TMP_Text label;
        [Tooltip("A chapter's Roman numeral: ink-4 at rest, iris-d on the open one; hover leaves it be.")]
        public TMP_Text numeral;
        public Image glyph;
        public RectTransform underline;
        public AttentionDot dot;
        public Image outwardArrow;

        [Tooltip("Exit alone hovers to rose.")]
        public bool hoversRose;

        public Action onPress;
        Func<bool> _lit;
        Func<bool> _unseen;

        bool _hovered;

        public void Configure(Func<bool> lit, Func<bool> unseen, Action press)
        {
            _lit = lit;
            _unseen = unseen;
            onPress = press;
            if (button != null)
            {
                button.Clicked += () => onPress?.Invoke();
                button.Hovered += h => { _hovered = h; Paint(); };
            }
            Refresh();
        }

        public void Refresh()
        {
            bool lit = _lit != null && _lit();
            if (underline != null && underline.gameObject.activeSelf != lit)
                underline.gameObject.SetActive(lit);

            // The dot never appears on the open one, and never on a utility.
            bool unseen = kind == BarItemKind.Destination && !lit && _unseen != null && _unseen();
            if (dot != null) dot.Set(unseen);

            Paint();
        }

        void Paint()
        {
            bool lit = _lit != null && _lit();
            Tok c;
            if (lit) c = Tok.Ink;
            else if (_hovered) c = hoversRose ? Tok.RoseD : Tok.Ink2;
            else c = Tok.Ink3;

            var col = Theme.Get(c);
            if (label != null) label.color = col;
            if (numeral != null) numeral.color = Theme.Get(lit ? Tok.IrisD : Tok.Ink4);
            // Glyphs are always the label's color — never tinted on their own, and no
            // brand colors: Discord and Reddit are redrawn in this line family.
            if (glyph != null) glyph.color = col;
            if (underline != null)
            {
                var img = underline.GetComponent<Image>();
                if (img != null) img.color = Theme.Get(Tok.Iris);
            }
            if (outwardArrow != null) outwardArrow.color = Theme.Get(Tok.Ink4);
        }
    }
}
