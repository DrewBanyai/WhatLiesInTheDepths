// What Lies In The Depths — center destination. Spec section 6.
// This is the one place anything overlays anything, and it pays for it by making the field
// visibly yield rather than sitting a panel on top of live content.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine;
using Ursine.UI;
using Ursine.Economy;

namespace WhatLiesInTheDepths.UI
{
    public sealed class RevelationsView : MonoBehaviour
    {
        [Header("Field")]
        public RectTransform field;
        public RectTransform mass;
        public CanvasGroup massGroup;
        public RectTransform sigilLayer;
        public SigilView sigilPrefab;
        [Tooltip("Italic, 11px, ink4, 12 above the field's foot. Gone while a readout is open.")]
        public TMP_Text fieldCaption;
        [Tooltip("Five places inside the mass for kept greater realizations, filled in order.")]
        public Image[] absorbedSlots;

        [Header("Readout — 600 x 300, dead center, one fixed position")]
        public GameObject readout;
        public Image readoutGround;
        public Image readoutBorder;
        public Image readoutShadow;
        public Image markRule;
        public Image markPanel;
        public Image markGlyph;
        public TMP_Text kindCaption;
        public TMP_Text revelationName;
        public TMP_Text realization;
        public RectTransform effects;
        public GameObject effectRowPrefab;
        public RectTransform costs;
        public CostPill costPillPrefab;
        public UiButton realize;
        public Image realizeGround;
        public TMP_Text realizeLabel;
        public TMP_Text reasonLine;
        public CanvasGroup readoutContents;

        [Header("Pointer — the field and the readout each report entering and leaving")]
        [Tooltip("On the field itself. uGUI sends a container its exit only when the pointer "
               + "leaves its whole subtree, so this means 'left the field', not 'left a sigil'.")]
        public UiButton fieldHover;
        [Tooltip("On the readout itself. Likewise, moving onto Realize is not leaving the readout.")]
        public UiButton readoutHover;

        readonly List<SigilView> _sigils = new List<SigilView>();
        SigilView _open;
        bool _touchedReadout;
        float _closeAt = -1f;                 // when a pending close fires; -1 is none
        const float Grace = 1f / 6f;          // "about a sixth of a second"
        float _t;                             // the field's own clock, in seconds

        // Absorbed sigils remember: at no fixed interval one warms to gold and cools back
        // over about two seconds. Nothing is being announced.
        static readonly Color DisabledInk = new Color32(0xB6, 0xAE, 0xCB, 0xFF);   // spec's raw
        static readonly Color Rest = new Color32(0x8D, 0x74, 0xB4, 0xFF);   // artwork, not a token
        static readonly Color Warm = new Color32(0xD6, 0xAC, 0x6E, 0xFF);
        const float RestAlpha = 0.46f, WarmAlpha = 0.80f, RememberFor = 1.9f;
        float[] _nextRemember;
        float _judgeIn;
        float _captionAlpha = 1f, _captionWant = 1f;

        string _built;

        void Start()
        {
            Build();
            if (fieldHover != null) fieldHover.Hovered += OnFieldHover;
            if (readoutHover != null) readoutHover.Hovered += OnReadoutHover;
            HookKept();
            // Realizing is a single click, no confirmation. The sigil leaves the field (a
            // greater one is taken into the mass) and the field is drawn again without it.
            if (realize != null)
                realize.Clicked += () =>
                {
                    var d = _open != null ? _open.Def : null;
                    if (d == null || GameState.I == null) return;
                    if (GameState.I.Realize(d)) { Close(); Build(); }
                };
            if (GameState.I != null) GameState.I.Changed += OnChanged;
            Close();
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= OnChanged;
        }

        /// <summary>A realization arriving while the field is open joins it at once.</summary>
        void OnChanged()
        {
            if (Signature() != _built && _open == null) Build();
            // The purse moved: an open readout's pills, button and reason line follow it, so a
            // cost that becomes payable while you are reading it turns payable in front of you.
            if (_open != null && _open.Def != null) PaintCost(_open.Def);
        }

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var r in s.ShownRevelations) b.Append(r.k).Append(',');
            b.Append('|').Append(s.absorbed.Count);
            return b.ToString();
        }

        void Build()
        {
            var s = GameState.I;
            if (s == null || sigilLayer == null || sigilPrefab == null) return;

            foreach (var old in _sigils) if (old != null) Destroy(old.gameObject);
            _sigils.Clear();
            _built = Signature();

            if (fieldCaption != null)
            {
                int within = 0;
                foreach (var r in s.ShownRevelations) within++;
                fieldCaption.text = Strings.T(within == 1 ? "ui.revelations.within.one" : "ui.revelations.within.many",
                                              Strings.Number(within));
            }

            if (absorbedSlots != null)
            {
                _nextRemember = new float[absorbedSlots.Length];
                for (int i = 0; i < absorbedSlots.Length; i++)
                {
                    var slot = absorbedSlots[i];
                    if (slot == null) continue;
                    bool kept = i < s.absorbed.Count;
                    slot.gameObject.SetActive(kept);
                    if (!kept) continue;
                    var kept_ = s.revelations.Find(r => r.k == s.absorbed[i]);
                    Art.Apply(slot, Art.Sigil(kept_ != null && !string.IsNullOrEmpty(kept_.g) ? kept_.g : s.absorbed[i]));
                    slot.color = new Color(Rest.r, Rest.g, Rest.b, RestAlpha);
                    _nextRemember[i] = 2f + i * 3.5f + Random.value * 5f;
                }
            }

            int index = 0;
            foreach (var r in s.ShownRevelations)
            {
                var v = Instantiate(sigilPrefab, sigilLayer);
                r.state = s.Judge(r.cost);
                v.Bind(r, index++);
                v.HoverChanged += OnSigilHover;
                _sigils.Add(v);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            foreach (var v in _sigils)
                if (v.Def != null) v.Tick(_t, dt, v == _open);

            if (_closeAt >= 0f && Time.unscaledTime >= _closeAt) Close();

            Remember();

            // A sigil says whether it can be afforded before anyone rests on it: short
            // grays it, above a ceiling sets it to .45. Judged twice a second, not per frame.
            if ((_judgeIn -= dt) <= 0f)
            {
                _judgeIn = 0.5f;
                Rejudge();
            }

            // The caption fades over .2s rather than blinking.
            if (fieldCaption != null && !Mathf.Approximately(_captionAlpha, _captionWant))
            {
                _captionAlpha = Mathf.MoveTowards(_captionAlpha, _captionWant, dt / 0.2f);
                fieldCaption.alpha = _captionAlpha;
            }
        }

        void Rejudge()
        {
            var s = GameState.I;
            if (s == null) return;
            foreach (var v in _sigils)
            {
                if (v.Def == null) continue;
                var was = v.Def.state;
                v.Def.state = s.Judge(v.Def.cost);
                if (v.Def.state == was) continue;
                v.Paint();
                if (v.group != null)
                    v.group.alpha = ((_open == null && _keptOpen < 0) || _open == v ? 1f : 0.18f) * v.BaseAlpha;
            }
        }

        void Remember()
        {
            if (absorbedSlots == null || _nextRemember == null) return;
            for (int i = 0; i < absorbedSlots.Length; i++)
            {
                var slot = absorbedSlots[i];
                if (slot == null || !slot.gameObject.activeSelf) continue;
                float d = _t - _nextRemember[i];
                if (d > 0f && d < RememberFor)
                {
                    float u = Mathf.Sin(d / RememberFor * Mathf.PI);   // up and back down
                    var c = Color.Lerp(Rest, Warm, u);
                    c.a = Mathf.Lerp(RestAlpha, WarmAlpha, u);
                    slot.color = c;
                }
                else if (d >= RememberFor)
                {
                    slot.color = new Color(Rest.r, Rest.g, Rest.b, RestAlpha);
                    _nextRemember[i] = _t + 7f + Random.value * 13f;   // a while before it surfaces again
                }
            }
        }

        // The rule, spec section 6:
        //   Before the pointer has touched the readout, the readout closes only when the
        //   pointer leaves the whole field — so crossing from a sigil to it is free, and
        //   there is no dead zone between a sigil and the panel that describes it.
        //   Once it has been inside the readout, stepping off closes it, unless the pointer
        //   reaches a sigil within about a sixth of a second: its own, which keeps it, or
        //   another, which swaps it.

        void OnSigilHover(SigilView v, bool entering)
        {
            if (entering)
            {
                _closeAt = -1f;                   // reaching a sigil in time keeps or swaps it
                if (_open != v) { if (_keptOpen >= 0) Close(); Open(v); }
            }
            else if (_touchedReadout && _open == v)
            {
                ScheduleClose();
            }
        }

        void OnReadoutHover(bool entering)
        {
            if (entering)
            {
                _touchedReadout = true;
                _closeAt = -1f;
            }
            else if (_open != null)
            {
                ScheduleClose();
            }
        }

        void OnFieldHover(bool entering)
        {
            // Leaving the whole field closes it at once, touched or not.
            if (!entering) Close();
        }

        void ScheduleClose() => _closeAt = Time.unscaledTime + Grace;

        void Open(SigilView v)
        {
            _open = v;
            var d = v.Def;
            d.seen = true;
            Purchasable(true);
            if (readout != null) readout.SetActive(true);
            FillBody(d);
            if (costs != null && costPillPrefab != null)
            {
                foreach (Transform c in costs) Destroy(c.gameObject);
                _pills.Clear();
                foreach (var a in d.cost)
                {
                    var pill = Instantiate(costPillPrefab, costs);
                    pill.ShowGlyph(false);   // the readout names a cost in words alone
                    _pills.Add(pill);
                }
            }
            PaintCost(d);

            // The field yields: mass to .25, unhovered sigils to .18, and the caption goes.
            if (massGroup != null) massGroup.alpha = 0.25f;
            _captionWant = 0f;
            foreach (var s in _sigils)
                if (s.group != null) s.group.alpha = (s == v ? 1f : 0.18f) * s.BaseAlpha;
        }

        /// <summary>The part of the readout that says what a realization is: its mark, kind,
        /// name, text and effects, gold for a greater one. Shared by a sigil still in the
        /// field and a greater one already kept in the mass.</summary>
        void FillBody(RevelationDef d)
        {
            Art.Apply(markGlyph, Art.Sigil(string.IsNullOrEmpty(d.g) ? d.k : d.g));
            if (kindCaption != null)
            {
                kindCaption.text = (d.kind ?? string.Empty).ToUpperInvariant();
                kindCaption.color = Theme.Get(d.great ? Tok.GoldD : Tok.IrisD);
            }
            if (revelationName != null) revelationName.text = d.n;
            if (realization != null) realization.text = d.bl;
            // A greater realization keeps the veil ground; its border, mark and shadow turn gold.
            if (readoutGround != null) readoutGround.color = Theme.Get(Tok.Veil);
            if (readoutBorder != null) readoutBorder.color = Theme.Get(d.great ? Tok.GoldB : Tok.Haze);
            if (readoutShadow != null)
                readoutShadow.color = d.great ? Theme.Get(Tok.GoldD, 0.40f) : Theme.Get(Tok.Ink, 0.45f);
            if (markPanel != null) markPanel.color = Theme.Get(d.great ? Tok.GoldL : Tok.IrisL);
            if (markRule != null) markRule.color = Theme.Get(d.great ? Tok.GoldB : Tok.Haze2);
            if (markGlyph != null) markGlyph.color = Theme.Get(d.great ? Tok.GoldD : Tok.IrisD);

            if (effects != null && effectRowPrefab != null)
            {
                foreach (Transform c in effects) Destroy(c.gameObject);
                effects.gameObject.SetActive(d.fx != null && d.fx.Count > 0);
                foreach (var f in d.fx)
                {
                    var go = Instantiate(effectRowPrefab, effects);
                    var t = go.GetComponentInChildren<TMP_Text>();
                    // Effects are final values, always — what the thing gives. The row is prose;
                    // the value that changes is teal and heavier.
                    if (t != null)
                    {
                        string teal = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.TealD));
                        t.text = f.Replace("<b>", "<b><color=" + teal + ">").Replace("</b>", "</color></b>");
                        t.color = Theme.Get(Tok.Prose);
                        t.fontSize = 12.5f;
                    }
                }
            }
        }

        readonly List<CostPill> _pills = new List<CostPill>();
        RevelationDef _laidOut;

        /// <summary>Everything on the readout that depends on the purse: each pill's refusal,
        /// the Realize button, the reason line, and the half-strength body above a ceiling.
        /// Painted on opening and again every time the ledger changes while it is open.</summary>
        void PaintCost(RevelationDef d)
        {
            var state = GameState.I.Judge(d.cost);
            d.state = state;

            for (int i = 0; i < _pills.Count && i < d.cost.Count; i++)
            {
                var a = d.cost[i];
                var res = GameState.I.Find(a.k);
                Refusal pillState = GameState.I.AboveCeiling(a.k, a.n) ? Refusal.AboveCeiling
                                  : GameState.I.Short(a.k, a.n) ? Refusal.Short : Refusal.None;
                if (_pills[i] != null) _pills[i].Set(res != null ? res.n : a.k, a.n, pillState);
            }

            if (realize != null) realize.SetInteractable(state == Refusal.None);
            if (realizeGround != null)
                realizeGround.color = state == Refusal.None
                    ? Theme.Get(d.great ? Tok.GoldD : Tok.Iris)
                    : Theme.Get(Tok.Track);
            if (realizeLabel != null)
            {
                realizeLabel.text = Strings.T("ui.revelations.realize");
                realizeLabel.color = state == Refusal.None ? Theme.Get(Tok.Veil) : DisabledInk;
            }
            bool relayout = false;
            if (reasonLine != null)
            {
                string reason = GameState.I.ReasonLine(d.cost) ?? string.Empty;
                bool shown = reason.Length > 0;
                relayout = shown != reasonLine.gameObject.activeSelf || reason != reasonLine.text;
                reasonLine.gameObject.SetActive(shown);
                reasonLine.text = reason;
            }

            // Above a ceiling the readout's contents drop to 50% — but its ground stays
            // opaque, because a half-transparent panel with a field behind it is unreadable.
            float body = state == Refusal.AboveCeiling ? 0.5f : 1f;
            if (readoutContents != null)
            {
                readoutContents.alpha = body;
                // Only when the reason line came, went or changed length: this runs every tick.
                if (relayout || _laidOut != d)
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)readoutContents.transform);
                _laidOut = d;
            }
            if (markPanel != null)
            {
                var mg = markPanel.GetComponent<CanvasGroup>();
                if (mg == null) mg = markPanel.gameObject.AddComponent<CanvasGroup>();
                mg.alpha = body;
            }
        }

        void Close()
        {
            _open = null;
            _keptOpen = -1;
            Purchasable(true);
            _laidOut = null;
            _touchedReadout = false;
            _closeAt = -1f;
            if (readout != null) readout.SetActive(false);
            if (massGroup != null) massGroup.alpha = 1f;
            _captionWant = 1f;
            foreach (var s in _sigils)
                if (s.group != null) s.group.alpha = s.BaseAlpha;
        }


        // ---- A greater realization kept in the mass reads back out, as a golden Vision does
        // from the iris: resting on its mark opens the readout for it, with nothing to pay and
        // nothing to press. The readout lets the pointer through while it shows a kept one, so
        // the mark underneath keeps it open and the pointer can step from one mark to the next.

        int _keptOpen = -1;
        bool _keptHooked;

        void HookKept()
        {
            if (_keptHooked || absorbedSlots == null) return;
            _keptHooked = true;
            for (int i = 0; i < absorbedSlots.Length; i++)
            {
                var slot = absorbedSlots[i];
                if (slot == null) continue;
                slot.raycastTarget = true;
                var btn = slot.GetComponent<UiButton>();
                if (btn == null) btn = slot.gameObject.AddComponent<UiButton>();
                int captured = i;
                btn.Hovered += h => OnKeptHover(captured, h);
            }
        }

        void OnKeptHover(int i, bool entering)
        {
            if (entering)
            {
                _closeAt = -1f;
                if (_keptOpen != i) OpenKept(i);
            }
            else if (_keptOpen == i)
            {
                ScheduleClose();
            }
        }

        void OpenKept(int i)
        {
            var s = GameState.I;
            if (s == null || i < 0 || i >= s.absorbed.Count) return;
            var d = s.revelations.Find(r => r.k == s.absorbed[i]);
            if (d == null) return;
            if (_open != null) Close();
            _keptOpen = i;
            _touchedReadout = false;
            if (readout != null) readout.SetActive(true);
            FillBody(d);
            Purchasable(false);
            if (readoutContents != null)
            {
                readoutContents.alpha = 1f;
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)readoutContents.transform);
            }
            if (markPanel != null)
            {
                var mg = markPanel.GetComponent<CanvasGroup>();
                if (mg != null) mg.alpha = 1f;
            }
            _laidOut = null;
            // The field yields around it as it does for a sigil.
            if (massGroup != null) massGroup.alpha = 0.25f;
            _captionWant = 0f;
            foreach (var v in _sigils)
                if (v.group != null) v.group.alpha = 0.18f * v.BaseAlpha;
        }

        /// <summary>Shows or hides the parts of the readout that are about paying: the cost
        /// pills, Realize and the reason line. A kept realization has none of them, and the
        /// readout then lets the pointer through to the mark beneath it.</summary>
        void Purchasable(bool on)
        {
            if (costs != null) costs.gameObject.SetActive(on);
            if (realize != null) realize.gameObject.SetActive(on);
            if (!on && reasonLine != null) reasonLine.gameObject.SetActive(false);
            if (readout != null)
            {
                var g = readout.GetComponent<CanvasGroup>();
                if (g == null) g = readout.AddComponent<CanvasGroup>();
                g.blocksRaycasts = on;
            }
        }
    }
}
