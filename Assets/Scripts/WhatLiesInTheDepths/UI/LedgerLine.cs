// What Lies In The Depths — one line of a spent / gained ledger, and whether it can be met.
//
// Used by the Focus cards and the Depth Gauge alike, so a cost you are short of looks the
// same wherever it is written: rose ground, rose edge, rose ink. A gain that would run past
// its ceiling goes gold the same way.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.Economy;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;

namespace WhatLiesInTheDepths.UI
{
    public sealed class LedgerLine
    {
        public Amount amount;
        public bool spent;
        public Tok tone;
        public Image ground, border, glyph;
        public TMP_Text figure, name;
        Refusal _shown = (Refusal)(-1);

        /// <summary>Wraps an instance of the UI_LedgerLine prefab.</summary>
        public static LedgerLine On(GameObject go, Amount a, bool spent, Tok tone, bool showGlyph)
        {
            var l = new LedgerLine { amount = a, spent = spent, tone = tone };
            var g = go.transform.Find("Glyph");
            if (g != null)
            {
                g.gameObject.SetActive(showGlyph);
                if (showGlyph)
                {
                    l.glyph = g.GetComponent<Image>();
                    Art.Apply(l.glyph, Art.Resource(a.k));
                }
            }
            var gr = go.transform.Find("Ground");
            if (gr != null) l.ground = gr.GetComponent<Image>();
            var bd = go.transform.Find("Border");
            if (bd != null) l.border = bd.GetComponent<Image>();
            var ts = go.GetComponentsInChildren<TMP_Text>(true);
            if (ts.Length > 0) l.figure = ts[0];
            if (ts.Length > 1) l.name = ts[1];
            return l;
        }

        /// <summary>Repaints only when the verdict has changed.</summary>
        public void Paint()
        {
            var s = GameState.I;
            if (s == null) return;
            Refusal state = Refusal.None;
            if (spent && s.Short(amount.k, amount.n)) state = Refusal.Short;
            else if (!spent && s.Ceiling(amount.k) > 0
                     && s.Held(amount.k) + amount.n > s.Ceiling(amount.k)) state = Refusal.AboveCeiling;
            if (state == _shown) return;
            _shown = state;

            Tok groundTok = state == Refusal.Short ? Tok.RoseL : Tok.GoldL;
            Tok edge = state == Refusal.Short ? Tok.RoseB : Tok.GoldB;
            Tok ink = state == Refusal.Short ? Tok.RoseD : Tok.GoldD;
            bool refusing = state != Refusal.None;

            if (ground != null) ground.color = Theme.Get(groundTok, refusing ? 1f : 0f);
            if (border != null) border.color = Theme.Get(edge, refusing ? 1f : 0f);
            if (figure != null) figure.color = Theme.Get(refusing ? ink : tone);
            if (name != null) name.color = Theme.Get(refusing ? ink : (glyph != null ? Tok.Prose : tone));
            if (glyph != null) glyph.color = refusing ? Theme.Get(ink) : Theme.Get(Tok.Prose, 0.8f);
        }
    }
}
