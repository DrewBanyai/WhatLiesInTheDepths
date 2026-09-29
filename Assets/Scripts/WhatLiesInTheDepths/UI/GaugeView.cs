// What Lies In The Depths — the right column. Spec section 11.
// The entry never scrolls: a veil's story is short by design, so the whole panel,
// including the control that parts the veil, always fits its track.
//
// At the bottom (the last veil parted) there is no next veil: the plate, name and story stay
// on the last one, sounded, and the readout says the bottom has been reached instead of
// counting fathoms. The dive carries on as "Collect the remains" (Dream.remains): endless,
// with its own spend and haul, its own Oneiri and your attention carried across.
using System.Collections.Generic;
using System.Text;
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
    public sealed class GaugeView : MonoBehaviour
    {
        [Header("Plate")]
        public Image plate;
        public TMP_Text ordinal;
        public TMP_Text veilName;

        [Header("Dive block")]
        public TMP_Text fathomCaption;
        public TMP_Text fathomSunk;
        public TMP_Text fathomOf;
        public TMP_Text fathomNeed;
        public ProgressTrack progress;
        public RectTransform spentColumn;
        public RectTransform broughtColumn;
        public TMP_Text rateLine;
        public Stepper stepper;
        public UiButton diveControl;
        public TMP_Text diveLabel;
        public RectTransform diveFill;
        public TMP_Text diveSubLine;
        [Tooltip("The dive block, which grows at full so its closing line gets the spec's spacing.")]
        public RectTransform diveBlock;
        public Image diveGround;
        public Image diveFillImage;
        public Image diveBorder;
        public Image diveArrow;
        public Image diveMark;

        static Color FillOn => Theme.Get(Tok.IrisDeep);   // the lit button's fill
        bool _diveHover;

        // The spent and brought-up columns have room for two lines each. When either holds more
        // (the remains bring up four), the block grows by the difference and everything under
        // the columns moves down with it, so nothing overlaps the rate line or the stepper.
        const float LedgerTop = 62f;
        readonly List<RectTransform> _below = new List<RectTransform>();
        readonly List<float> _belowY = new List<float>();
        float _ledgerRoom = 64f, _extra;
        bool _measure = true;
        RectTransform _midRule;
        float _midRuleH;

        [Header("The entry")]
        public RectTransform entryColumn;
        [Tooltip("The clip the entry column sits in; its height is the part of the entry shown.")]
        public RectTransform entryClip;
        public GameObject paragraphPrefab;
        public GameObject ruledBlockPrefab;
        public UiButton partControl;
        public RectTransform partRoot;
        public Image partGround;
        public TMP_Text partSubLine;
        [Tooltip("The panel root, whose height follows the entry and the part control.")]
        public RectTransform panel;

        // Spec .turn:hover — one step darker than iris; no token carries it.
        // Spec: the sounded dive control's ground, a gray-violet no token carries.
        static Color SoundedGround => Theme.Mix(Tok.Veil, Tok.Mist, 0.5f);
        const float DiveBlockH = 260f;

        // Nothing in this panel snaps into place. A paragraph the dive has just written fades
        // up; the part control waits for the panel to make room for it before it arrives.
        const float WritingSeconds = 0.75f;
        const float RoomSeconds = 0.75f;
        const float ArrivalSeconds = 0.5f;

        readonly List<CanvasGroup> _writing = new List<CanvasGroup>();
        float _writingT = 1f;              // 1 is "nothing is being written"
        CanvasGroup _partGroup;
        float _roomT, _arrivalT;           // the panel growing, then the control fading in

        static Color PartHover => Theme.Mix(Tok.Iris, Tok.IrisDeep, 0.55f);
        bool _partHover;
        int _shown = -1;

        [Header("Ledger rows")]
        public GameObject ledgerLinePrefab;

        GameObject _oneiriGlyph, _oneiriLabel;

        void Start()
        {
            _oneiriGlyph = Rebuild.Deep(transform, "OneiriGlyph")?.gameObject;
            _oneiriLabel = Rebuild.Deep(transform, "OneiriLabel")?.gameObject;
            CaptureBelow();

            if (stepper != null)
            {
                stepper.Configure(() => GameState.I != null ? GameState.I.OneiriFree : 0);
                stepper.Changed += w =>
                {
                    // Oneiri belong to the task, not the veil. The dive task is persistent;
                    // the assignment never changes when a veil is parted, the last one included.
                    if (GameState.I?.Dive != null) GameState.I.Dive.w = w;
                    GameState.I?.Dirty();
                    Refresh();
                };
            }

            if (diveControl != null)
            {
                // Diving is a Focus in a separate place. Pressing it moves your effort here
                // from whatever Focus card had it; pressing it again puts it down.
                diveControl.Clicked += () =>
                {
                    var s = GameState.I;
                    if (s?.Dive == null || s.Dive.AtFull) return;
                    s.attendingDive = !s.attendingDive;
                    if (s.attendingDive) s.attendedTaskId = null;
                    s.Dirty();
                };
                diveControl.Hovered += h => { _diveHover = h; Refresh(); };
            }
            if (partControl != null)
            {
                partControl.Clicked += PartTheVeil;
                partControl.Hovered += h => { _partHover = h; Refresh(); };
            }

            if (GameState.I != null) GameState.I.Changed += Refresh;
            if (GameClock.I != null) GameClock.I.Frame += OnFrame;
            Theme.Changed += Refresh;

            if (partRoot != null)
            {
                _partGroup = partRoot.GetComponent<CanvasGroup>();
                if (_partGroup == null) _partGroup = partRoot.gameObject.AddComponent<CanvasGroup>();
                _partGroup.alpha = 0f;
            }

            BuildEntry();
            Refresh();
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= Refresh;
            if (GameClock.I != null) GameClock.I.Frame -= OnFrame;
            Theme.Changed -= Refresh;
        }

        void OnFrame(float dt)
        {
            // The dive itself is done by the dream (GameState), so it carries on whatever the
            // center is showing. The control only draws where it has got to.
            var dv = GameState.I?.Dive;
            if (dv == null) return;
            if (diveFill != null)
                diveFill.anchorMax = new Vector2(Mathf.Clamp01(dv.p), 1f);
        }

        VeilDef _entryFor;

        void BuildEntry()
        {
            var v = GameState.I?.veil;
            if (v == null || entryColumn == null) return;

            // What is new this time: on the first build of a veil nothing is new, so the
            // story it has already told is simply there.
            int before = _entryFor == v ? _shown : int.MaxValue;

            Rebuild.Clear(entryColumn);
            _writing.Clear();
            _entryFor = v;

            // The story is revealed at thresholds, a paragraph at a time. One ruled block
            // per unrevealed paragraph, so the page reads as having room left. A sounded
            // shore has told all of it — the spec's finished gauge has no rules left.
            int shown = Shown(v);
            _shown = shown;
            for (int i = 0; i < v.entry.Count; i++)
            {
                if (i < shown && paragraphPrefab != null)
                {
                    var go = Instantiate(paragraphPrefab, entryColumn);
                    var t = go.GetComponentInChildren<TMP_Text>();
                    if (t != null)
                    {
                        t.text = v.entry[i];
                        // The newest paragraph sits a step darker than the rest.
                        t.color = Theme.Get(i == shown - 1 ? Tok.Ink : Tok.Prose);
                    }

                    if (i >= before)
                    {
                        var g = go.GetComponent<CanvasGroup>();
                        if (g == null) g = go.AddComponent<CanvasGroup>();
                        g.alpha = 0f;
                        _writing.Add(g);
                        _writingT = 0f;
                    }
                }
                else if (ruledBlockPrefab != null)
                {
                    Instantiate(ruledBlockPrefab, entryColumn);
                }
            }
        }

        static int Shown(VeilDef v) =>
            v.entry == null ? 0 : (v.AtFull ? v.entry.Count : Mathf.Min(v.revealed, v.entry.Count));

        public void Refresh()
        {
            var v = GameState.I?.veil;
            if (v == null) return;
            // The veil owns the plate, the name, the readout and the story; the dive (the veil
            // itself, or the remains at the bottom) owns everything you work.
            var dv = GameState.I.Dive ?? v;
            if (v != _entryFor || Shown(v) != _shown) BuildEntry();

            // The Oneiri stepper grows onto the dive with the realization that allows binding,
            // on the same beat it grows onto every Focus card.
            bool binding = GameState.I.BindingOpen;
            if (stepper != null && stepper.gameObject.activeSelf != binding) stepper.gameObject.SetActive(binding);
            if (_oneiriGlyph != null && _oneiriGlyph.activeSelf != binding) _oneiriGlyph.SetActive(binding);
            if (_oneiriLabel != null && _oneiriLabel.activeSelf != binding) _oneiriLabel.SetActive(binding);

            // The ordinal climbs in words — First Veil, Second Veil — and never states a total.
            if (ordinal != null) ordinal.text = Strings.T("ui.gauge.ordinal", Strings.Ordinal(v.ord)).ToUpperInvariant();
            if (veilName != null) veilName.text = v.n;

            // The fathom readout goes teal at full — caption, both figures, the "of" at .7,
            // and the bar. Everything else on the panel keeps its color.
            bool sounded = v.AtFull;
            bool bottom = GameState.I.AtBottom;
            if (fathomCaption != null) fathomCaption.color = Theme.Get(sounded ? Tok.TealD : Tok.Ink3);
            // At the bottom the count gives way to a sentence, set in the "of"'s serif: there is
            // no total left to count toward.
            if (fathomSunk != null)
            {
                if (fathomSunk.gameObject.activeSelf == bottom) fathomSunk.gameObject.SetActive(!bottom);
                fathomSunk.text = Fmt.Count(v.sunk);     // exact and unabbreviated at any scale
                fathomSunk.color = Theme.Get(sounded ? Tok.TealD : Tok.Ink);
            }
            if (fathomOf != null)
            {
                fathomOf.text = bottom ? Strings.T("ui.gauge.bottom") : "of";
                fathomOf.color = bottom ? Theme.Get(Tok.TealD) : sounded ? Theme.Get(Tok.TealD, 0.7f) : Theme.Get(Tok.Ink3);
            }
            if (fathomNeed != null)
            {
                if (fathomNeed.gameObject.activeSelf == bottom) fathomNeed.gameObject.SetActive(!bottom);
                fathomNeed.text = Fmt.Count(v.need);
                fathomNeed.color = Theme.Get(sounded ? Tok.TealD : Tok.Ink);
            }
            if (progress != null)
            {
                progress.Set(bottom ? 1f : v.Fill);
                var fillImg = progress.fill != null ? progress.fill.GetComponent<Image>() : null;
                if (fillImg != null) fillImg.color = Theme.Get(sounded ? Tok.Teal : Tok.Iris);
            }

            FillLedgerColumn(spentColumn, dv.spend, Tok.RoseD, Strings.T("ui.gauge.spent"));
            // Only what a dive actually brings: a resource not yet shown is not listed.
            FillLedgerColumn(broughtColumn, GameState.I.Dream.DiveBring, Tok.TealD, Strings.T("ui.gauge.brought"));

            if (stepper != null) stepper.Set(dv.w, dv.cap);

            // Full is the dive's: a sounded veil has nothing to sink, the remains never fill.
            bool full = dv.AtFull;

            if (rateLine != null)
            {
                // Period, not frequency. Replaced outright at full fathoms.
                rateLine.gameObject.SetActive(true);
                bool mine = GameState.I.attendingDive;
                double period = dv.Period(mine);
                string held = !full && !double.IsInfinity(period)
                    ? FocusCardView.HoldLine(GameState.I, GameState.I.HoldOfDive, dv.spend, dv.bring) : null;
                rateLine.text = full
                    ? Strings.T("ui.gauge.noDive")
                    : double.IsInfinity(period) ? Strings.T("ui.gauge.nothingDiving")
                    : held ?? Strings.T("ui.gauge.rate", Fmt.Period(period));
                rateLine.color = Theme.Get(full ? Tok.Ink4 : Tok.Ink2);
            }

            // Yours: filled iris, the progress in the darker iris, white label, the mark.
            // Not yours: outlined, a faint iris fill, an arrow — exactly the Focus button.
            bool lit = GameState.I.attendingDive && !full;
            if (diveControl != null) diveControl.SetInteractable(!full);
            if (diveLabel != null)
            {
                // The control keeps its name at full (the line under it says why); at the bottom
                // it is a different task.
                diveLabel.text = Strings.T(bottom ? "ui.gauge.remains" : "ui.gauge.sink");
                diveLabel.color = full ? Theme.Get(Tok.Ink4) : Theme.Get(lit ? Tok.OnIris : Tok.IrisD);
            }
            if (diveGround != null)
                diveGround.color = full ? SoundedGround
                                 : lit ? Theme.Get(Tok.Iris) : Theme.Get(Tok.Iris, _diveHover ? 0.08f : 0f);
            if (diveFillImage != null)
                diveFillImage.color = full ? Color.clear : lit ? FillOn : Theme.Get(Tok.Iris, 0.17f);
            if (diveBorder != null) diveBorder.color = Theme.Get(full ? Tok.Haze : lit ? Tok.Iris : Tok.IrisB);
            if (diveArrow != null) diveArrow.gameObject.SetActive(!lit && !full);
            if (diveMark != null) diveMark.gameObject.SetActive(lit);
            if (diveFill != null) diveFill.anchorMax = new Vector2(Mathf.Clamp01(dv.p), 1f);
            if (diveSubLine != null)
            {
                // At full the line names why the control is off, centered and italic like the
                // part control's; otherwise it carries the stepper's binding limit, if any.
                diveSubLine.text = full
                    ? Strings.T("ui.gauge.sounded")
                    : binding && stepper != null ? (stepper.BindingLimit() ?? string.Empty) : string.Empty;
                diveSubLine.alignment = full ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
                diveSubLine.fontStyle = full ? FontStyles.Italic : FontStyles.Normal;
                if (full) diveSubLine.color = Theme.Get(Tok.Ink3);
            }

            // Spec, sounded: the closing line sits 7 under the control and the block's 15 of
            // padding follows it, so the block is 15 taller than while diving, and the entry
            // moves down with it (17 below the block either way).
            _full = full;
            PlaceDiveBlock();

            // At full fathoms a control appears beneath the last paragraph to part the
            // veil. Nothing auto-advances.
            // Nothing to part into yet means no control: the next shore is simply not written.
            bool partable = v.AtFull && GameState.I.HasNextVeil;
            var partGo = partRoot != null ? partRoot.gameObject : partControl != null ? partControl.gameObject : null;
            if (partGo != null && partGo.activeSelf != partable)
            {
                // The control is switched on the moment the shore is sounded, but it arrives
                // in two beats: the panel opens the room for it, then it fades into that room.
                partGo.SetActive(partable);
                _roomT = 0f;
                _arrivalT = 0f;
                if (_partGroup != null) _partGroup.alpha = 0f;
                if (partSubLine != null) partSubLine.alpha = 0f;
            }
            if (partSubLine != null)
            {
                partSubLine.gameObject.SetActive(partable);
                partSubLine.text = Strings.T("ui.gauge.partSub");
            }
            if (partGround != null) partGround.color = _partHover ? PartHover : Theme.Get(Tok.Iris);
        }

        /// <summary>Spec .body is a column with a 17 gap and 18 of padding under it: the part
        /// control follows the last paragraph, and the panel ends under whatever came last.
        /// Placed after layout so the entry's height is this frame's.</summary>
        void LateUpdate()
        {
            if (entryColumn == null) return;
            bool full = partRoot != null && partRoot.gameObject.activeSelf;

            MeasureLedger();
            Writing();

            // The panel never outgrows its track. Waiting rules are only there to say the
            // page has room left, so when there are more of them than the track can hold,
            // the last blocks go rather than the panel running off the screen.
            float limit = Layout.PanelTrackH - 18f;
            if (full)
            {
                limit -= 17f + (partRoot != null ? partRoot.rect.height : 0f);
                if (partSubLine != null && partSubLine.gameObject.activeSelf)
                    limit -= 8f + partSubLine.rectTransform.rect.height;
            }
            var top = entryClip != null ? entryClip : entryColumn;
            float entryTop = -top.anchoredPosition.y;
            for (int i = entryColumn.childCount - 1; i >= 0; i--)
            {
                if (entryTop + entryColumn.rect.height <= limit) break;
                var c = entryColumn.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                if (!c.name.StartsWith("UI_RuledBlock")) break;
                c.gameObject.SetActive(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(entryColumn);
            }

            // Written paragraphs are never dropped. If they alone are longer than the track
            // allows, the clip crops the entry's foot so the part control and the panel's
            // floor stay where the center panels end.
            float shownH = Mathf.Max(0f, Mathf.Min(entryColumn.rect.height, limit - entryTop));
            if (entryClip != null && !Mathf.Approximately(entryClip.sizeDelta.y, shownH))
                entryClip.sizeDelta = new Vector2(entryClip.sizeDelta.x, shownH);

            float entryBottom = entryTop + shownH;
            float bottom = entryBottom;
            if (full)
            {
                float partTop = entryBottom + 17f;
                partRoot.anchoredPosition = new Vector2(partRoot.anchoredPosition.x, -partTop);
                float room = 17f + partRoot.rect.height;
                bottom = partTop + partRoot.rect.height;
                if (partSubLine != null && partSubLine.gameObject.activeSelf)
                {
                    var sub = partSubLine.rectTransform;
                    sub.anchoredPosition = new Vector2(sub.anchoredPosition.x, -(bottom + 8f));
                    bottom += 8f + sub.rect.height;
                    room += 8f + sub.rect.height;
                }

                // The panel opens the room over three quarters of a second, and only once it
                // is open does the control fade into it. Held at nothing until then, so the
                // button is never seen hanging past the panel's own floor.
                bottom = entryBottom + room * Arrival(ref _roomT, RoomSeconds);
                if (_roomT >= 1f) Arrival(ref _arrivalT, ArrivalSeconds);

                float a = Mathf.SmoothStep(0f, 1f, _arrivalT);
                if (_partGroup != null)
                {
                    _partGroup.alpha = a;
                    _partGroup.interactable = a > 0.99f;
                    _partGroup.blocksRaycasts = a > 0.99f;
                }
                if (partSubLine != null) partSubLine.alpha = a;
            }
            if (panel != null)
            {
                float h = Mathf.Ceil(bottom + 18f);
                if (!Mathf.Approximately(panel.sizeDelta.y, h))
                    panel.sizeDelta = new Vector2(panel.sizeDelta.x, h);
            }
        }

        bool _full;

        /// <summary>The dive block's height and what follows it: the closing line under the
        /// control, and the entry 16 below the block. Grown by whatever the ledger needed.</summary>
        void PlaceDiveBlock()
        {
            if (diveBlock == null) return;
            // Spec, sounded: the closing line sits 7 under the control and the block's 15 of
            // padding follows it, so the block is 15 taller than while diving, and the entry
            // moves down with it (17 below the block either way).
            float h = (_full ? DiveBlockH + 15f : DiveBlockH) + _extra;
            if (!Mathf.Approximately(diveBlock.sizeDelta.y, h))
                diveBlock.sizeDelta = new Vector2(diveBlock.sizeDelta.x, h);
            if (diveSubLine != null)
            {
                var sub = diveSubLine.rectTransform;
                sub.anchoredPosition = new Vector2(sub.anchoredPosition.x, -((_full ? 247f : 242f) + _extra));
            }
            var entryTopNode = entryClip != null ? entryClip : entryColumn;
            if (entryTopNode != null)
                entryTopNode.anchoredPosition = new Vector2(entryTopNode.anchoredPosition.x,
                    diveBlock.anchoredPosition.y - h - 16f);
        }

        /// <summary>Remembers where everything under the ledger columns sits as built, so it can
        /// be moved down from there (never from wherever it was last moved to).</summary>
        void CaptureBelow()
        {
            _below.Clear(); _belowY.Clear();
            if (diveBlock == null) return;
            var sub = diveSubLine != null ? diveSubLine.rectTransform : null;
            float rateTop = float.MaxValue;
            foreach (Transform c in diveBlock)
            {
                var rt = c as RectTransform;
                if (rt == null || rt == sub || rt == spentColumn || rt == broughtColumn) continue;
                if (rt.name == "Rule" && -rt.anchoredPosition.y <= LedgerTop + 1f) { _midRule = rt; _midRuleH = rt.sizeDelta.y; continue; }
                float top = -rt.anchoredPosition.y;
                if (top < LedgerTop + 20f) continue;          // the readout and the bar, above the columns
                _below.Add(rt); _belowY.Add(rt.anchoredPosition.y);
                rateTop = Mathf.Min(rateTop, top);
            }
            // The columns may run to 4 short of whatever is first beneath them (the rate line).
            if (rateTop < float.MaxValue) _ledgerRoom = rateTop - LedgerTop - 4f;
        }

        /// <summary>After the columns are rebuilt: how much taller than their room the taller one
        /// is, and if that changed, everything under them moves by it.</summary>
        void MeasureLedger()
        {
            if (!_measure) return;
            _measure = false;
            float need = Mathf.Max(ColumnHeight(spentColumn), ColumnHeight(broughtColumn));
            float extra = Mathf.Max(0f, Mathf.Ceil(need - _ledgerRoom));
            if (Mathf.Approximately(extra, _extra)) return;
            _extra = extra;
            for (int i = 0; i < _below.Count; i++)
                if (_below[i] != null)
                    _below[i].anchoredPosition = new Vector2(_below[i].anchoredPosition.x, _belowY[i] - _extra);
            if (_midRule != null) _midRule.sizeDelta = new Vector2(_midRule.sizeDelta.x, _midRuleH + _extra);
            PlaceDiveBlock();
        }

        static float ColumnHeight(RectTransform col)
        {
            if (col == null || !col.gameObject.activeInHierarchy) return 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(col);
            return LayoutUtility.GetPreferredHeight(col);
        }

        /// <summary>A paragraph the dive has just written fades up rather than appearing.</summary>
        void Writing()
        {
            if (_writingT >= 1f || _writing.Count == 0) return;
            _writingT = Mathf.Clamp01(_writingT + Time.unscaledDeltaTime / WritingSeconds);
            float a = Mathf.SmoothStep(0f, 1f, _writingT);
            for (int i = 0; i < _writing.Count; i++)
                if (_writing[i] != null) _writing[i].alpha = a;
            if (_writingT >= 1f) _writing.Clear();
        }

        /// <summary>Advances one 0-to-1 clock by this frame, and answers where it has reached,
        /// eased.</summary>
        static float Arrival(ref float t, float seconds)
        {
            t = Mathf.Clamp01(t + Time.unscaledDeltaTime / Mathf.Max(0.001f, seconds));
            return Mathf.SmoothStep(0f, 1f, t);
        }

        void FillLedgerColumn(RectTransform col, List<Amount> amounts, Tok tone, string caption)
        {
            if (col == null || ledgerLinePrefab == null) return;
            // The caption stays; only the lines are rebuilt.
            // Switched off first, so this frame's layout already stops counting them.
            foreach (Transform c in col)
                if (c.name != "Caption") { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            _measure = true;

            bool spent = col == spentColumn;
            _lines.RemoveAll(l => l.spent == spent);

            // A column with nothing in it is absent, not empty.
            bool any = amounts != null && amounts.Count > 0;
            col.gameObject.SetActive(any);
            if (!any) return;
            foreach (var a in amounts)
            {
                var go = Instantiate(ledgerLinePrefab, col);
                var res = GameState.I.Find(a.k);
                // The gauge names its resources in words, in the column's own tone.
                var line = LedgerLine.On(go, a, spent, tone, false);
                if (line.figure != null) line.figure.text = Fmt.Amount(a.n);
                if (line.name != null) line.name.text = res != null ? res.n : a.k;
                _lines.Add(line);
                line.Paint();
            }
        }

        // The spend and the haul, judged against the purse twice a second: a price you
        // cannot meet goes rose, a haul that would overflow its ceiling goes gold.
        readonly List<LedgerLine> _lines = new List<LedgerLine>();
        float _judgeIn;

        void Update()
        {
            if ((_judgeIn -= Time.unscaledDeltaTime) > 0f) return;
            _judgeIn = 0.5f;
            // Effects on the dive (cost, haul) rebuild the veil's amount lists; follow them.
            var v = GameState.I != null ? GameState.I.Dive : null;
            if (v != null)
            {
                LedgerLine.Resync(_lines, true, v.spend, "");
                var brings = GameState.I.Dream.DiveBring;
                // A resource shown since the column was drawn joins it.
                if (brings != null && LedgerLine.CountOf(_lines, false) != brings.Count)
                    FillLedgerColumn(broughtColumn, brings, Tok.TealD, Strings.T("ui.gauge.brought"));
                else
                    LedgerLine.Resync(_lines, false, brings, "");
            }
            foreach (var l in _lines) l.Paint();
        }

        /// <summary>Parting takes about 2.4s: the plate crossfades, the ordinal and name
        /// change, fathoms reset to zero, the story clears to fresh ruled blocks. No
        /// confirmation, and it is one-way — the sub-line is the only warning.</summary>
        void PartTheVeil()
        {
            // The number of veils is never disclosed, so the next one is simply the next
            // one: no total, no remainder, nowhere, ever.
            if (GameState.I != null && GameState.I.PartVeil())
            {
                BuildEntry();
                Refresh();
            }
        }
    }
}
