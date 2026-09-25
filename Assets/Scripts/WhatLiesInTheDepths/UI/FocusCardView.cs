// What Lies In The Depths — one Focus card. Spec section 5.
using System.Collections.Generic;
using System.Linq;
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
    public sealed class FocusCardView : MonoBehaviour
    {
        public Image cardGround;
        public Image cardBorder;
        public Image iconTile;
        public Image glyph;
        public TMP_Text taskName;
        public TMP_Text blurb;
        public RectTransform spentColumn;
        public RectTransform gainedColumn;
        public GameObject ledgerLinePrefab;
        public TMP_Text rateLine;
        public Stepper stepper;
        public UiButton action;
        public Image actionGround;
        public TMP_Text actionLabel;
        public RectTransform actionProgress;
        public Image actionFill;
        public Image actionBorder;
        public Image actionArrow;
        public Image actionMark;
        public Image tileBorder;
        public Image ledgerRule;
        public RectTransform ledger;

        /// <summary>The card's height once its ledger has grown to its longest column.
        /// The Focus grid sizes each section's row to the tallest of these.</summary>
        public float PreferredHeight { get; private set; }

        // The spec's raw values where it does not use a token.
        static readonly Color FillOn = new Color32(0x7C, 0x61, 0xAE, 0xFF);     // the lit button's fill
        static readonly Color TileOn = new Color32(0xE8, 0xE0, 0xF7, 0xFF);     // the lit card's tile
        bool _cardHover, _actionHover;
        public AttentionDot dot;
        public TMP_Text limitLine;

        FocusTask _t;
        string _heldShown;

        /// <summary>The line a held piece of work shows: what it is short of, or what is full.</summary>
        public static string HoldLine(GameState s, Hold hold, List<Amount> cost, List<Amount> gain)
        {
            if (s == null || hold == Hold.None) return null;
            if (hold == Hold.Short) return Strings.T("ui.hold.short", s.ReasonLine(cost));
            if (gain != null && gain.Count == 1)
                return Strings.T("ui.hold.full", s.Find(gain[0].k)?.n ?? gain[0].k);
            return Strings.T("ui.hold.fullMany");
        }
        GameObject _oneiriGlyph, _oneiriLabel;

        public void Bind(FocusTask t)
        {
            _t = t;
            Art.Apply(glyph, Art.Focus(t.g));
            if (taskName != null) taskName.text = t.n;
            if (blurb != null) blurb.text = t.b;
            if (dot != null) dot.Set(!t.seen);

            // The stepper grows onto the card the moment Oneiri can be bound, and not before:
            // what you have not unlocked does not exist, parts of a card included.
            _oneiriGlyph = Rebuild.Deep(transform, "OneiriGlyph")?.gameObject;
            _oneiriLabel = Rebuild.Deep(transform, "OneiriLabel")?.gameObject;

            if (stepper != null)
            {
                stepper.Configure(() => GameState.I != null ? GameState.I.OneiriFree : 0);
                stepper.Set(t.w, t.cap);
                stepper.Changed += w => { _t.w = w; GameState.I?.Dirty(); Refresh(); };
            }

            if (action != null)
                action.Clicked += () =>
                {
                    var s = GameState.I;
                    if (s == null) return;
                    // Pressing another card moves your effort; pressing the lit one puts
                    // it down, and then nothing is filled.
                    s.attendedTaskId = s.attendedTaskId == _t.id ? null : _t.id;
                    s.attendingDive = false;
                    s.Dirty();
                };

            _lines.Clear();
            FillColumn(spentColumn, t.cost, Tok.RoseD);
            FillColumn(gainedColumn, t.gain, Tok.TealD);

            GrowLedger(t);

            // With nothing spent, the gained column takes the whole block and the rule goes.
            bool spends = t.cost != null && t.cost.Count > 0;
            if (ledgerRule != null) ledgerRule.gameObject.SetActive(spends);
            if (gainedColumn != null && spentColumn != null && !spends)
                gainedColumn.anchoredPosition = spentColumn.anchoredPosition;

            // A newly unlocked card's dot clears the first time the pointer enters it.
            var btn = GetComponent<UiButton>();
            if (btn != null) btn.Hovered += h =>
            {
                if (h && !_t.seen) { _t.seen = true; dot?.Set(false); }
                _cardHover = h;
                Refresh();
            };
            if (action != null) action.Hovered += h => { _actionHover = h; Refresh(); };

            Refresh();
        }

        void FillColumn(RectTransform col, List<Amount> amounts, Tok tone)
        {
            if (col == null || ledgerLinePrefab == null) return;
            // The caption stays; only the lines are rebuilt.
            foreach (Transform c in col)
            {
                if (c.name == "Caption")
                {
                    var cap = c.GetComponent<TMP_Text>();
                    if (cap != null) cap.color = Theme.Get(tone);
                    continue;
                }
                Destroy(c.gameObject);
            }
            bool any = amounts != null && amounts.Count > 0;
            col.gameObject.SetActive(any);   // a column with nothing in it is absent, not empty
            if (!any) return;

            // Spent reads as a loss, gained as a gain: "−12 Silt", "+6 Silt".
            bool spent = tone == Tok.RoseD;
            string sign = spent ? "\u2212" : "+";
            foreach (var a in amounts)
            {
                var go = Instantiate(ledgerLinePrefab, col);
                var res = GameState.I?.Find(a.k);
                var line = LedgerLine.On(go, a, spent, tone, true);
                if (line.figure != null) line.figure.text = sign + Fmt.Count(a.n);
                if (line.name != null) line.name.text = res != null ? res.n : a.k;
                _lines.Add(line);
                line.Paint();
            }
        }

        readonly List<LedgerLine> _lines = new List<LedgerLine>();
        float _judgeIn;

        // The prefab's ledger holds one line per column. Each further line (21, plus the
        // column's 5 gap) pushes everything under the ledger down, and the card with it.
        void GrowLedger(FocusTask t)
        {
            var root = (RectTransform)transform;
            PreferredHeight = root.rect.height;
            if (ledger == null) return;
            int lines = Mathf.Max(t.cost?.Count ?? 0, t.gain?.Count ?? 0);
            float extra = Mathf.Max(0, lines - 1) * 26f;   // a 21 line and the 5 gap
            if (extra <= 0f) return;

            float ledgerBottom = ledger.localPosition.y + ledger.rect.yMin;
            foreach (RectTransform c in root)
            {
                if (c == ledger) continue;
                float top = c.localPosition.y + c.rect.yMax;
                if (top <= ledgerBottom + 0.5f) c.anchoredPosition -= new Vector2(0f, extra);
            }
            ledger.sizeDelta += new Vector2(0f, extra);
            foreach (RectTransform c in ledger)
                if (c.name == "Rule") c.sizeDelta += new Vector2(0f, extra);
            PreferredHeight += extra;
        }

        public void Refresh()
        {
            if (_t == null) return;
            var s = GameState.I;
            bool mine = s != null && s.attendedTaskId == _t.id;
            bool binding = s != null && s.BindingOpen;

            if (stepper != null)
            {
                if (stepper.gameObject.activeSelf != binding) stepper.gameObject.SetActive(binding);
                stepper.Set(_t.w, _t.cap);
            }
            if (_oneiriGlyph != null && _oneiriGlyph.activeSelf != binding) _oneiriGlyph.SetActive(binding);
            if (_oneiriLabel != null && _oneiriLabel.activeSelf != binding) _oneiriLabel.SetActive(binding);

            if (rateLine != null)
            {
                // The rate states the interval the player is actually getting, so it
                // changes when attention arrives or leaves. Absent when nothing works it.
                double period = _t.Period(mine);
                bool working = _t.w > 0 || mine;
                // Work with nothing to pay with, or nothing left to earn, stands still and says why.
                string held = working ? HoldLine(s, s.HoldOf(_t), _t.cost, _t.gain) : null;
                rateLine.gameObject.SetActive(working);
                if (held != null) rateLine.text = held;
                else if (working) rateLine.text = Strings.T("ui.focus.rate", Fmt.Period(period));
                _heldShown = held;
            }

            // Yours: filled iris, the progress a darker iris (#7C61AE), white label and the
            // attention mark. Not yours: outlined, a faint iris fill, an arrow.
            if (actionLabel != null)
            {
                actionLabel.text = _t.n;
                actionLabel.color = Theme.Get(mine ? Tok.Veil : Tok.IrisD);
            }
            if (actionGround != null)
                actionGround.color = mine ? Theme.Get(Tok.Iris)
                                   : Theme.Get(Tok.Iris, _actionHover ? 0.08f : 0f);
            // .13 in the spec, blended in gamma; .17 gives the same result blended in linear.
            if (actionFill != null) actionFill.color = mine ? FillOn : Theme.Get(Tok.Iris, 0.17f);
            if (actionBorder != null) actionBorder.color = Theme.Get(mine ? Tok.Iris : Tok.IrisB);
            if (actionArrow != null) actionArrow.gameObject.SetActive(!mine);
            if (actionMark != null) actionMark.gameObject.SetActive(mine);

            // The lit card, or one under the pointer: iris-b border, white ground, a deeper tile.
            bool lit = mine || _cardHover;
            if (cardBorder != null) cardBorder.color = Theme.Get(lit ? Tok.IrisB : Tok.Haze);
            if (cardGround != null) cardGround.color = lit ? Color.white : Theme.Get(Tok.Veil);
            if (iconTile != null) iconTile.color = mine ? TileOn : Theme.Get(Tok.IrisL);
            if (tileBorder != null) tileBorder.color = Theme.Get(mine ? Tok.IrisB : Tok.Haze2);
            if (actionProgress != null)
                actionProgress.anchorMax = new Vector2(Mathf.Clamp01(_t.p / 100f), 1f);

            if (limitLine != null)
            {
                string limit = binding && stepper != null ? stepper.BindingLimit() : null;
                limitLine.gameObject.SetActive(!string.IsNullOrEmpty(limit));
                limitLine.text = limit ?? string.Empty;
            }

            // A gain above your ceiling does not dim the card here, though it dims a
            // construct: in Focus it clears the moment the player spends the resource.
        }

        void Update()
        {
            if (_t == null) return;
            var s = GameState.I;
            if (s == null) return;
            // The purse moves every tick; the ledger's refusals follow it twice a second.
            if ((_judgeIn -= Time.unscaledDeltaTime) <= 0f)
            {
                _judgeIn = 0.5f;
                foreach (var l in _lines) l.Paint();
                var st = GameState.I;
                bool mineNow = st.attendedTaskId == _t.id;
                string heldNow = (_t.w > 0 || mineNow) ? HoldLine(st, st.HoldOf(_t), _t.cost, _t.gain) : null;
                if (rateLine != null && heldNow != _heldShown) Refresh();
            }

            // The work itself is done by the dream (GameState), so it carries on while the
            // player is looking at something else. The card only draws where it has got to.
            if (actionProgress != null)
                actionProgress.anchorMax = new Vector2(Mathf.Clamp01(_t.p / 100f), 1f);

        }
    }
}
