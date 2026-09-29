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

        /// <summary>Which resource this row is.</summary>
        public string Key => _res?.k;

        // Pointed at from a price elsewhere (ResourceHint). It has to be seen from across the
        // screen, so it is loud: an iris wash over the row that breathes (28–44%), a 2px iris
        // outline round the whole row, a 5px dark-iris bar at its left edge, and the name in
        // bold. Over the fill and under the words. Made on first use.
        Image _hint, _hintBar;
        readonly Image[] _edges = new Image[4];
        bool _hinted;
        float _pulse;
        int _layers;

        public void SetHint(bool on)
        {
            if (on == _hinted && (_hint != null || !on)) return;
            _hinted = on;
            _pulse = 0f;
            if (_hint == null)
            {
                if (!on) return;
                _hint = Layer("Hint", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _hintBar = Layer("HintBar", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));
                // The outline, as four 2px edges: top, bottom, left, right.
                _edges[0] = Layer("HintTop", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -2f), Vector2.zero);
                _edges[1] = Layer("HintBottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 2f));
                _edges[2] = Layer("HintLeft", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(2f, 0f));
                _edges[3] = Layer("HintRight", new Vector2(1f, 0f), Vector2.one, new Vector2(-2f, 0f), Vector2.zero);
            }
            if (nameLabel != null) nameLabel.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
            PaintHint();
        }

        Image Layer(string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            // Straight after the fill, so it marks the row but never covers its words.
            // Each layer after the one before, so the outline and bar sit on the wash.
            // The fill is drawn by a child named "Fill" (the FillBar component sits on the row
            // itself), so the layers go straight after that child, never under it.
            var fillChild = transform.Find("Fill");
            int at = (fillChild != null ? fillChild.GetSiblingIndex() + 1 : 0) + _layers++;
            rt.SetSiblingIndex(at);
            rt.anchorMin = min; rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;           // the ledger is still not something to point at
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            return img;
        }

        void PaintHint()
        {
            if (_hint == null) return;
            float wash = 0.28f + 0.16f * (0.5f - 0.5f * Mathf.Cos(_pulse * Mathf.PI * 2f / 1.2f));
            _hint.color = _hinted ? Theme.Get(Tok.Iris, wash) : Color.clear;
            _hintBar.color = _hinted ? Theme.Get(Tok.IrisD) : Color.clear;
            foreach (var e in _edges) if (e != null) e.color = _hinted ? Theme.Get(Tok.Iris) : Color.clear;
        }

        void Update()
        {
            if (!_hinted || _hint == null) return;
            _pulse += Time.unscaledDeltaTime;
            PaintHint();
        }

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
            PaintHint();                         // a palette change repaints it with everything else

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
                held.text = Fmt.Held(shown);           // no abbreviation, ever
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
