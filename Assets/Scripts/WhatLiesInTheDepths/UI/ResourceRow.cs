// What Lies In The Depths — one row of the ResourceLedger. Spec section 2.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine.Economy;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    /// <summary>One row: icon, name, held over maximum, rate. Height 28 — the floor, below
    /// which the mono numerals crowd their baseline. No separators: the fill is what
    /// separates rows.</summary>
    public sealed class ResourceRow : MonoBehaviour
    {
        public FillBar fill;
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text held;
        public TMP_Text divider;
        public TMP_Text maximum;
        public TMP_Text rate;

        Res _res;

        public void Bind(Res r)
        {
            _res = r;
            Art.Apply(icon, Art.Resource(r.glyph));
            if (nameLabel != null) nameLabel.text = r.n;
            if (divider != null) divider.text = "/";
            if (fill != null) fill.SetWidth(Layout.SideColumnW);
            Refresh(true);
        }

        public void Refresh(bool immediate = false)
        {
            if (_res == null) return;

            // Oneiri read as free / housed: the held figure is how many are not bound to a
            // task or the dive, and the fill and the gold follow that figure, not the total.
            double shown = _res.c;
            bool oneiri = _res.k == "oneiri" && GameState.I != null;
            if (oneiri) shown = GameState.I.OneiriFree;
            bool full = oneiri ? _res.m > 0 && shown >= _res.m : _res.Full;

            // Three row states: ordinary, at the ceiling, hostile.
            Tok inkTok, fillTok, iconTok, maxTok;
            if (_res.hostile)
            {
                fillTok = Tok.RoseT; inkTok = Tok.RoseD; iconTok = Tok.RoseD; maxTok = Tok.Ink3;
            }
            else if (full)
            {
                fillTok = Tok.GoldL; inkTok = Tok.GoldD; iconTok = Tok.GoldD; maxTok = Tok.GoldD;
            }
            else
            {
                fillTok = Tok.IrisL; inkTok = Tok.Ink; iconTok = Tok.IrisD; maxTok = Tok.Ink3;
            }

            // At the ceiling the fill is drawn full width.
            float f = full ? 1f : oneiri ? (_res.m > 0 ? Mathf.Clamp01((float)(shown / _res.m)) : 0f) : _res.Fill;
            if (fill != null) fill.Set(f, Theme.Get(fillTok), immediate);

            if (icon != null) icon.color = Theme.Get(iconTok);
            if (nameLabel != null) nameLabel.color = Theme.Get(inkTok);
            if (held != null)
            {
                held.text = Fmt.Count(shown);           // no abbreviation, ever
                held.color = Theme.Get(inkTok);
            }
            if (maximum != null)
            {
                maximum.text = Fmt.Count(_res.m);
                maximum.color = Theme.Get(maxTok);
            }
            if (rate != null)
            {
                // The rate stays honest at a ceiling — it reports what is being produced,
                // not what is being kept. Under 0.05 it reads 0.0 /s, never blank.
                rate.text = Fmt.Rate(_res.r);
                bool standingStill = _res.r > -0.05 && _res.r < 0.05;
                Tok t;
                if (standingStill) t = Tok.Ink3;
                else if (_res.hostile) t = Tok.RoseD;          // sign convention inverts
                else t = _res.r > 0 ? Tok.TealD : Tok.RoseD;
                rate.color = Theme.Get(t);
                rate.fontStyle = (_res.hostile && _res.r > 0) ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }
}
