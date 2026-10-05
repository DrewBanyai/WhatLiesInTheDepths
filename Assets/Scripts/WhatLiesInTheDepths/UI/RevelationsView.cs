// What Lies In The Depths — center destination. Spec section 6.
// This is the one place anything overlays anything, and it pays for it by making the field
// visibly yield rather than sitting a panel on top of live content.
//
// At its center hangs the lantern: one pane for every Revelation the dream can realize, lit in
// the order they were realized, gold for a greater one, blue for a side of a choice. A lit pane
// reads its realization back.
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
        /// <summary>Lantern units to canvas units.</summary>
        public const float LanternScale = 0.85f;
        /// <summary>How far above the field's middle the footprint nothing drifts through is
        /// centered — and the lantern with it, and the readout over it.</summary>
        public const float FootprintLift = 80f;
        /// <summary>How far below the field's middle the fork stands: its top edge is 139 above
        /// the foot of the 720 field, so its middle is 252 below the middle. The drift reads it
        /// too — nothing passes through the one still object in the field.</summary>
        public const float ForkDrop = 252f;
        /// <summary>Half the footprint kept clear around the fork, its line included.</summary>
        public const float ForkClearW = 120f, ForkClearH = 78f;

        /// <summary>Where a lit pane's readout hangs: its top, this far below the field's middle,
        /// in the band under the lantern.</summary>
        const float KeptTop = 92f;
        /// <summary>How tall that band is before a lit pane's readout has to rise into the lantern.</summary>
        const float KeptRoom = 268f;

        [Header("Field")]
        public RectTransform field;
        public LanternView lantern;
        public CanvasGroup lanternGroup;
        public RectTransform sigilLayer;
        public SigilView sigilPrefab;
        [Tooltip("Italic, 11px, ink4, 12 above the field's foot. Gone while a readout is open.")]
        public TMP_Text fieldCaption;

        [Header("Readout — 600 x 300 over the lantern; beneath it when reading a lit pane back")]
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

        [Header("The fork — one object, two halves, and it does not drift")]
        [Tooltip("168 x 62, centred, 139 above the foot of the field. Everything else in the "
               + "field drifts; this does not, and that is the whole of its announcement.")]
        public GameObject forkRoot;
        public CanvasGroup forkGroup;
        public UiButton forkHover;
        public Image forkGlyphA, forkGlyphB;
        public Image forkHaloA, forkHaloB;
        public Image forkRingA, forkRingB;
        public TMP_Text forkWord;
        public TMP_Text forkCaption;
        public ChoicePanelView choice;

        [Header("Pointer — the field and the readout each report entering and leaving")]
        [Tooltip("On the field itself. uGUI sends a container its exit only when the pointer "
               + "leaves its whole subtree, so this means 'left the field', not 'left a sigil'.")]
        public UiButton fieldHover;
        [Tooltip("On the readout itself. Likewise, moving onto Realize is not leaving the readout.")]
        public UiButton readoutHover;

        /// <summary>Which face the fork's panel is wearing, if it is open at all.</summary>
        enum Fork { Shut, Offer, Past }

        readonly List<SigilView> _sigils = new List<SigilView>();
        RevelationDef _forkA, _forkB;
        Fork _fork = Fork.Shut;
        SigilView _open;
        bool _touchedReadout;
        float _closeAt = -1f;                 // when a pending close fires; -1 is none
        const float Grace = 1f / 6f;          // "about a sixth of a second"
        float _t;                             // the field's own clock, in seconds

        static Color DisabledInk => Theme.Mix(Tok.Ink3, Tok.Ink4, 0.55f);   // spec's raw
        float _judgeIn;
        readonly List<bool> _greats = new List<bool>();
        readonly List<bool> _choices = new List<bool>();
        float _captionAlpha = 1f, _captionWant = 1f;

        string _built;

        void Start()
        {
            Build();
            if (fieldHover != null) fieldHover.Hovered += OnFieldHover;
            if (readoutHover != null) readoutHover.Hovered += OnReadoutHover;
            if (lantern != null) lantern.PaneHovered += OnPaneHover;
            // Realizing is a single click, no confirmation. The sigil leaves the field, the
            // next pane of the lantern lights, and the field is drawn again without it.
            if (realize != null)
                realize.Clicked += () =>
                {
                    var d = _open != null ? _open.Def : null;
                    if (d == null || GameState.I == null) return;
                    if (GameState.I.Realize(d)) { Close(); Build(); }
                };
            if (forkHover != null) forkHover.Hovered += OnForkHover;
            if (choice != null)
            {
                choice.Hovered += OnChoiceHover;
                choice.Finished += () => { Close(); Build(); };
                if (choice.left != null && choice.left.realize != null)
                    choice.left.realize.Clicked += () => choice.Press(choice.left);
                if (choice.right != null && choice.right.realize != null)
                    choice.right.realize.Clicked += () => choice.Press(choice.right);
            }
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
            // Never while the fork is being spent: the player is watching something be given
            // up, and rebuilding the field would take the whole panel out from under them.
            bool busy = choice != null && choice.Spending;
            if (Signature() != _built && _open == null && !busy) Build();
            // The purse moved: an open readout's pills, button and reason line follow it, so a
            // cost that becomes payable while you are reading it turns payable in front of you.
            if (_open != null && _open.Def != null) PaintCost(_open.Def);
            if (_fork == Fork.Offer && choice != null) choice.PaintCosts();
        }

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var r in s.ShownRevelations) b.Append(r.k).Append(',');
            b.Append('|').Append(s.kept.Count);
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

            if (lantern != null)
            {
                lantern.Build(s.Realizable);
                _greats.Clear();
                _choices.Clear();
                foreach (var k in s.kept)
                {
                    var d = s.revelations.Find(r => r.k == k);
                    _greats.Add(d != null && d.great);
                    _choices.Add(d != null && d.Choice);
                }
                lantern.Show(s.kept.Count, _greats, _choices);
            }

            // The fork comes out of the drift. No side of a choice drifts any more, so those
            // two glyphs appear in this field only on the one still object in it.
            s.Fork(out _forkA, out _forkB);
            PaintFork();

            int index = 0;
            foreach (var r in s.ShownRevelations)
            {
                if (r == _forkA || r == _forkB) continue;
                var v = Instantiate(sigilPrefab, sigilLayer);
                r.state = s.Judge(r.cost);
                v.Bind(r, index++);
                v.avoidFork = _forkA != null && _forkB != null;
                v.HoverChanged += OnSigilHover;
                _sigils.Add(v);
            }
        }

        /// <summary>The fork, drawn or not drawn. Both sides have to be on offer: half a fork
        /// is a worse object than no fork.</summary>
        void PaintFork()
        {
            bool on = _forkA != null && _forkB != null;
            if (forkRoot != null) forkRoot.SetActive(on);
            if (!on) return;

            Art.Apply(forkGlyphA, Art.Sigil(string.IsNullOrEmpty(_forkA.g) ? _forkA.k : _forkA.g));
            Art.Apply(forkGlyphB, Art.Sigil(string.IsNullOrEmpty(_forkB.g) ? _forkB.k : _forkB.g));
            if (forkWord != null) forkWord.text = Strings.T("ui.choice.or").ToUpperInvariant();
            if (forkCaption != null) forkCaption.text = Strings.T("ui.revelations.fork");
            PaintForkInk(false);
        }

        void PaintForkInk(bool hover)
        {
            var ink = hover ? Theme.Mix(Tok.BlueD, Tok.Ink, 0.35f) : Theme.Get(Tok.BlueD);
            if (forkGlyphA != null) forkGlyphA.color = ink;
            if (forkGlyphB != null) forkGlyphB.color = ink;
            var halo = Theme.Get(Tok.Blue, hover ? 0.40f : 0.22f);
            if (forkHaloA != null) forkHaloA.color = halo;
            if (forkHaloB != null) forkHaloB.color = halo;
            var ring = Theme.Get(hover ? Tok.Blue : Tok.BlueB, hover ? 1f : 0.55f);
            if (forkRingA != null) forkRingA.color = ring;
            if (forkRingB != null) forkRingB.color = ring;
            if (forkWord != null) forkWord.color = Theme.Get(Tok.BlueD);
            if (forkCaption != null) forkCaption.color = Theme.Get(Tok.Ink3);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            foreach (var v in _sigils)
                if (v.Def != null) v.Tick(_t, dt, v == _open);

            if (_closeAt >= 0f && Time.unscaledTime >= _closeAt) Close();

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

        /// <summary>The fork behaves like any other realization: the player comes into the menu,
        /// rests on it, and the panel comes up. It never opens itself.</summary>
        void OnForkHover(bool entering)
        {
            PaintForkInk(entering);
            if (entering)
            {
                _closeAt = -1f;
                if (_fork != Fork.Offer) OpenChoice();
            }
            else if (_touchedReadout && _fork == Fork.Offer)
            {
                ScheduleClose();
            }
        }

        void OnChoiceHover(bool entering)
        {
            if (entering)
            {
                _touchedReadout = true;
                _closeAt = -1f;
            }
            else if (_fork != Fork.Shut)
            {
                ScheduleClose();
            }
        }

        /// <summary>The fork on offer. The field yields as it does for a sigil, and the fork
        /// does not: it stays at full strength beneath the panel, so the panel reads as
        /// belonging to the thing being pointed at rather than as a window over the menu.</summary>
        /// <summary>Puts the fork's panel away without touching anything else, so a sigil or a
        /// pane can take the field's attention from it.</summary>
        void ShutChoice()
        {
            if (_fork == Fork.Shut) return;
            _fork = Fork.Shut;
            if (choice != null) choice.gameObject.SetActive(false);
        }

        void OpenChoice()
        {
            if (choice == null || _forkA == null || _forkB == null) return;
            if (_open != null || _keptOpen >= 0) Close();
            if (readout != null) readout.SetActive(false);
            _fork = Fork.Offer;
            _forkA.seen = true;
            _forkB.seen = true;
            choice.gameObject.SetActive(true);
            choice.ShowOffer(_forkA, _forkB);
            Yield(forkLit: true);
        }

        /// <summary>The pair read back from its lit pane, for the rest of the run.</summary>
        void OpenChoicePast(RevelationDef taken, RevelationDef givenUp)
        {
            if (choice == null || taken == null || givenUp == null) return;
            if (_open != null) Close();
            _fork = Fork.Past;
            _touchedReadout = false;
            if (readout != null) readout.SetActive(false);
            choice.gameObject.SetActive(true);
            choice.ShowPast(taken, givenUp);
            Yield(forkLit: false);
        }

        /// <summary>The field, attending to one thing. The lantern drops behind the panel and
        /// the drift drops around it; the caption goes.</summary>
        void Yield(bool forkLit)
        {
            if (lanternGroup != null) lanternGroup.alpha = 0.25f;
            _captionWant = 0f;
            foreach (var v in _sigils)
                if (v.group != null) v.group.alpha = 0.18f * v.BaseAlpha;
            if (forkGroup != null) forkGroup.alpha = forkLit ? 1f : 0.18f;
        }

        void ScheduleClose() => _closeAt = Time.unscaledTime + Grace;

        void Open(SigilView v)
        {
            ShutChoice();
            _open = v;
            var d = v.Def;
            d.seen = true;
            Purchasable(true);
            if (readout != null) readout.SetActive(true);
            FillBody(d);
            if (costs != null && costPillPrefab != null)
            {
                foreach (Transform c in costs) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                _pills.Clear();
                foreach (var a in d.cost)
                {
                    var pill = Instantiate(costPillPrefab, costs);
                    pill.ShowGlyph(false);   // the readout names a cost in words alone
                    ResourceHover.On(pill.gameObject, a.k);   // lights its row in the ledger
                    _pills.Add(pill);
                }
            }
            PaintCost(d);
            Place(false);                            // sized to what it now says

            // The field yields: the lantern behind the readout to .25, unhovered sigils to .18,
            // and the caption goes.
            if (lanternGroup != null) lanternGroup.alpha = 0.25f;
            _captionWant = 0f;
            foreach (var s in _sigils)
                if (s.group != null) s.group.alpha = (s == v ? 1f : 0.18f) * s.BaseAlpha;
            // The fork yields with the drift: it is not more important than the thing the
            // player actually asked to read.
            if (forkGroup != null) forkGroup.alpha = 0.18f;
        }

        /// <summary>The part of the readout that says what a realization is: its mark, kind,
        /// name, text and effects, gold for a greater one, blue for a side of a choice. Shared by a sigil still in the
        /// field and a realization already lit in the lantern.</summary>
        void FillBody(RevelationDef d)
        {
            Art.Apply(markGlyph, Art.Sigil(string.IsNullOrEmpty(d.g) ? d.k : d.g));
            if (kindCaption != null)
            {
                kindCaption.text = (d.kind ?? string.Empty).ToUpperInvariant();
                kindCaption.color = Theme.Get(d.great ? Tok.GoldD : d.Choice ? Tok.BlueD : Tok.IrisD);
            }
            if (revelationName != null) revelationName.text = d.n;
            if (realization != null) realization.text = d.bl;
            // A greater realization keeps the veil ground; its border, mark and shadow turn gold.
            // A side of a choice does the same in blue.
            if (readoutGround != null) readoutGround.color = Theme.Get(Tok.Veil);
            bool choice = d.Choice;
            if (readoutBorder != null) readoutBorder.color = Theme.Get(d.great ? Tok.GoldB : choice ? Tok.BlueB : Tok.Haze);
            if (readoutShadow != null)
                readoutShadow.color = d.great ? Theme.Get(Tok.GoldD, 0.40f)
                                    : choice ? Theme.Get(Tok.BlueD, 0.40f) : Theme.Get(Tok.Ink, 0.45f);
            if (markPanel != null) markPanel.color = Theme.Get(d.great ? Tok.GoldL : choice ? Tok.BlueL : Tok.IrisL);
            if (markRule != null) markRule.color = Theme.Get(d.great ? Tok.GoldB : choice ? Tok.BlueB : Tok.Haze2);
            if (markGlyph != null) markGlyph.color = Theme.Get(d.great ? Tok.GoldD : choice ? Tok.BlueD : Tok.IrisD);

            if (effects != null && effectRowPrefab != null)
            {
                // Switched off before they go: Destroy waits for the frame's end, and the
                // readout is measured before then.
                foreach (Transform c in effects) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
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
                        Ursine.Text.Typeset.Resize(t, 12.5f);
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
                    ? Theme.Get(d.great ? Tok.GoldD : d.Choice ? Tok.BlueD : Tok.Iris)
                    : Theme.Get(Tok.Track);
            if (realizeLabel != null)
            {
                realizeLabel.text = Strings.T("ui.revelations.realize");
                realizeLabel.color = state == Refusal.None ? Theme.Get(Tok.OnIris) : DisabledInk;
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
            // Never mid-beat. The hold after a press is the one moment in this menu the
            // pointer does not govern.
            if (choice != null && choice.Spending) return;
            _open = null;
            _keptOpen = -1;
            _fork = Fork.Shut;
            if (choice != null) choice.gameObject.SetActive(false);
            Purchasable(true);
            _laidOut = null;
            _touchedReadout = false;
            _closeAt = -1f;
            if (readout != null) readout.SetActive(false);
            if (lanternGroup != null) lanternGroup.alpha = 1f;
            _captionWant = 1f;
            foreach (var s in _sigils)
                if (s.group != null) s.group.alpha = s.BaseAlpha;
            if (forkGroup != null) forkGroup.alpha = 1f;
            PaintForkInk(false);
        }


        // ---- A lit pane reads its realization back, as a golden Vision does from the iris:
        // resting on it opens the readout for it, with nothing to pay and nothing to press, in
        // the band beneath the lantern so the pane stays in sight. The readout lets the pointer
        // through while it shows a kept one, and the pointer can step from pane to pane.

        int _keptOpen = -1;

        void OnPaneHover(int i, bool entering)
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
            if (s == null || i < 0 || i >= s.kept.Count) return;
            var d = s.revelations.Find(r => r.k == s.kept[i]);
            if (d == null) return;
            // A pane lit by one side of a choice reads the whole fork back, not the single
            // realization that was taken: it records what the player chose BETWEEN.
            if (d.Choice)
            {
                var other = s.Partner(d);
                if (other != null) { OpenChoicePast(d, other); _keptOpen = i; return; }
            }
            ShutChoice();
            if (_open != null) Close();
            _keptOpen = i;
            _touchedReadout = false;
            FillBody(d);
            Purchasable(false);
            if (readoutContents != null) readoutContents.alpha = 1f;
            if (markPanel != null)
            {
                var mg = markPanel.GetComponent<CanvasGroup>();
                if (mg != null) mg.alpha = 1f;
            }
            _laidOut = null;
            if (readout != null) readout.SetActive(true);
            Place(true);
            // The sigils yield around it as they do for one another; the lantern does not,
            // because the pane being read is the point.
            if (lanternGroup != null) lanternGroup.alpha = 1f;
            _captionWant = 0f;
            foreach (var v in _sigils)
                if (v.group != null) v.group.alpha = 0.18f * v.BaseAlpha;
            if (forkGroup != null) forkGroup.alpha = 0.18f;
        }

        /// <summary>Where the readout sits, and how tall it is. Over the lantern for a sigil in the
        /// field: 600 x 300, or taller when what it says needs more, growing from the middle out.
        /// For a lit pane, in the band beneath the lantern, only as tall as what it says (there
        /// is no foot: nothing to pay, nothing to press); one too tall for the band rises into
        /// the lantern rather than past the field's foot. Call it after the body is filled.</summary>
        void Place(bool kept)
        {
            if (readout == null) return;
            var rt = (RectTransform)readout.transform;
            float h = kept ? 150f : 300f;
            if (readoutContents != null)
            {
                var body = (RectTransform)readoutContents.transform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(body);
                h = Mathf.Max(h, Mathf.Ceil(LayoutUtility.GetPreferredHeight(body)));
            }
            rt.pivot = new Vector2(0.5f, kept ? 1f : 0.5f);
            rt.sizeDelta = new Vector2(600f, h);
            rt.anchoredPosition = new Vector2(0f, kept ? -KeptTop + Mathf.Max(0f, h - KeptRoom) : FootprintLift);
            if (readoutContents != null)
            {
                var c = (RectTransform)readoutContents.transform;
                c.sizeDelta = new Vector2(c.sizeDelta.x, h);
                LayoutRebuilder.ForceRebuildLayoutImmediate(c);
            }
            if (markPanel != null)
            {
                var mp = markPanel.rectTransform;
                mp.sizeDelta = new Vector2(mp.sizeDelta.x, h);
                if (markRule != null) markRule.rectTransform.sizeDelta = new Vector2(markRule.rectTransform.sizeDelta.x, h);
            }
            if (markGlyph != null)
            {
                var g = markGlyph.rectTransform;
                g.anchoredPosition = new Vector2(g.anchoredPosition.x, -(h - g.sizeDelta.y) * 0.5f);
            }
        }

        /// <summary>Shows or hides the parts of the readout that are about paying: the cost
        /// pills, Realize and the reason line. A kept realization has none of them, and the
        /// readout then lets the pointer through.</summary>
        void Purchasable(bool on)
        {
            if (costs != null) costs.gameObject.SetActive(on);
            if (realize != null) realize.gameObject.SetActive(on);
            // A kept realization's readout has no foot at all, rule included.
            var foot = readoutContents != null ? readoutContents.transform.Find("Foot") : null;
            if (foot != null) foot.gameObject.SetActive(on);
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
