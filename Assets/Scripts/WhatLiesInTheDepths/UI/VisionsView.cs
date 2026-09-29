// What Lies In The Depths — center destination. Spec section 8.
// In this menu the question is never what is this but how far along is it, so every mark
// carries its own progress around its rim. Gold means the tier here, not the finish:
// completion is marked by the ring closing, which needs no color.
// At the center, the eye's iris is a window of glass, one pane per Vision, lit clockwise from
// the top as each is first finished (IrisWindowView); resting on a lit pane reads it back.
using System.Collections.Generic;
using System.Linq;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class VisionsView : MonoBehaviour
    {
        [Header("Field")]
        public RectTransform field;
        public RectTransform eye;
        public RectTransform markLayer;
        public VisionMarkView markPrefab;
        [Tooltip("The iris window: one pane per Vision, lit as each is first finished.")]
        public IrisWindowView iris;
        public UiButton eyeHover;

        [Header("Readout — 620 x 336, dead center, two columns with a rule between")]
        public GameObject readout;
        [Tooltip("The left column. The readout grows to its height when a Vision says more than 336 holds.")]
        public RectTransform readoutLeft;
        const float ReadoutHeight = 336f;
        public TMP_Text visionName;
        public TMP_Text blurb;
        public RectTransform effects;
        public GameObject effectRowPrefab;
        public TMP_Text percentLabel;
        public ProgressTrack percentBar;
        public TMP_Text repeatLine;
        public RectTransform offers;
        public GameObject offerRowPrefab;
        public UiButton pour;
        public TMP_Text pourLabel;
        public ToggleSwitch channel;

        [Header("Readout — the parts that change with the Vision's tier and state")]
        public Image readoutBorder;
        public Image readoutShadow;
        public Image rightGround;
        public TMP_Text kindCaption;
        public Image mark;
        public TMP_Text rightCaption;
        public Image pourGround;
        public TMP_Text channelHint;

        [Header("Field")]
        public TMP_Text fieldCaption;
        public CanvasGroup eyeGroup;

        [Header("Plaque — a finished Vision, read back out of its pane of the iris")]
        public GameObject plaque;
        public UiButton plaqueHover;
        public RectTransform plaqueShadow;
        public TMP_Text plaqueKind;
        public Image plaqueMarkGround;
        public Image plaqueMarkRule;
        public Image plaqueBorder;
        public Image plaqueGlyph;
        public TMP_Text plaqueName;
        public TMP_Text plaqueBlurb;
        public RectTransform plaqueEffects;

        [Header("Pointer — the field and the readout each report entering and leaving")]
        public UiButton fieldHover;
        public UiButton readoutHover;

        // The spec's raw values where it does not use a token.
        static Color RightGround => Theme.Mix(Tok.Veil, Tok.Mist, 0.15f);
        static Color RightGroundGreat => Theme.Mix(Tok.Veil, Tok.GoldL, 0.5f);
        static Color DisabledInk => Theme.Mix(Tok.Ink3, Tok.Ink4, 0.55f);

        // The iris window: which of its lit panes were golden Visions, in the order they lit.
        readonly List<bool> _greats = new List<bool>();
        int _lit = -1;
        int _plaqueIndex = -1;
        bool _plaqueTouched;
        float _plaqueCloseAt = -1f;

        // The Revelations rule exactly (spec section 8).
        bool _touchedReadout;
        float _closeAt = -1f;
        const float Grace = 1f / 6f;

        readonly List<VisionMarkView> _marks = new List<VisionMarkView>();
        VisionMarkView _open;
        float _orbit;
        bool _eyeRested;

        void Start()
        {
            Build();
            PaintIris();
            if (iris != null) iris.PaneHovered += OnPaneHover;
            if (fieldHover != null) fieldHover.Hovered += h => { if (!h) { Close(); ClosePlaque(); } };
            if (plaqueHover != null)
                plaqueHover.Hovered += h =>
                {
                    // Before the pointer has been on the plaque only leaving the field puts it
                    // away; once it has, stepping off does, after 150ms.
                    if (h) { _plaqueTouched = true; _plaqueCloseAt = -1f; }
                    else if (_plaqueTouched) _plaqueCloseAt = Time.unscaledTime + 0.15f;
                };
            ClosePlaque();
            if (readoutHover != null) readoutHover.Hovered += OnReadoutHover;
            if (GameState.I != null) GameState.I.Changed += OnChanged;
            Close();

            if (eyeHover != null)
                eyeHover.Hovered += h => _eyeRested = h;   // resting anywhere on the eye holds the marks still

            if (pour != null)
                pour.Clicked += () =>
                {
                    // A pour is immediate and unconfirmed, like every other purchase.
                    var d = _open?.Def;
                    if (d == null || d.of == null || d.of.Count == 0) return;
                    // The dream pours, and completes: a repeatable begins again a little
                    // dearer on its own, a one-off leaves the field.
                    if (!GameState.I.Dream.Pour(d)) return;
                    if (!GameState.I.visions.Contains(d)) { Retire(d); return; }
                    Paint();
                };

            if (channel != null)
                channel.Changed += on =>
                {
                    // Channelling continues with the readout closed and the menu closed.
                    if (_open?.Def != null) _open.Def.a = on && _open.Def.p < 100f;
                    Paint();
                    PaintCaption();
                };
        }

        string _built;

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var v in s.visions) if (s.Shown(v)) b.Append(v.k).Append(',');
            return b.ToString();
        }

        void Rebuild()
        {
            Close();
            foreach (var m in _marks) if (m != null) Destroy(m.gameObject);
            _marks.Clear();
            Build();
            PaintIris();
            PaintCaption();
        }

        void Build()
        {
            var s = GameState.I;
            if (s == null || markLayer == null || markPrefab == null) return;
            _built = Signature();

            foreach (var v in s.visions)
            {
                if (!s.Shown(v)) continue;
                var m = Instantiate(markPrefab, markLayer);
                m.Bind(v);
                m.HoverChanged += OnMarkHover;
                _marks.Add(m);
            }
        }

        void Update()
        {
            // One ellipse, 372 x 298, one direction, a revolution every nine minutes.
            if (!_eyeRested) _orbit += Time.deltaTime * (2f * Mathf.PI / 540f);

            foreach (var m in _marks)
            {
                if (m.Def == null) continue;
                float a = m.Def.angle + _orbit;
                var pos = new Vector2(Mathf.Cos(a) * 186f, Mathf.Sin(a) * 149f);

                // Nothing drifts inside 660 x 388 at the center.
                const float cx = 330f, cy = 194f;
                float k = Mathf.Max(Mathf.Abs(pos.x) / cx, Mathf.Abs(pos.y) / cy);
                if (k < 1f && k > 0f) pos /= k;

                ((RectTransform)m.transform).anchoredPosition = pos;
            }

            if (_plaqueCloseAt >= 0f && Time.unscaledTime >= _plaqueCloseAt) ClosePlaque();
            if (plaque != null && plaque.activeSelf && plaqueShadow != null)
            {
                // The shadow follows the plaque's height: 0 22px 48px -28px, as on the readouts.
                var pr = (RectTransform)plaque.transform;
                plaqueShadow.sizeDelta = new Vector2(pr.rect.width + 40f, pr.rect.height + 40f);
            }
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= OnChanged;
        }

        void OnChanged()
        {
            // A channelled one-off that has just filled leaves the field the same way.
            var s = GameState.I;
            if (s != null)
            {
                // A mark whose Vision has left the field (finished, channelled while the
                // menu was elsewhere) goes; a Vision newly shown gets its mark; one finished
                // for the first time lights the next pane of the iris.
                foreach (var m in _marks.ToArray())
                    if (m.Def == null || !s.visions.Contains(m.Def)) { Retire(m.Def); return; }
                if (Signature() != _built) { Rebuild(); return; }
                PaintIris();
            }
            PaintCaption();
            if (_open != null) Paint();              // the purse moved: affordability did too
        }

        void LateUpdate()
        {
            if (_closeAt >= 0f && Time.unscaledTime >= _closeAt) Close();
        }

        /// <summary>Lights the iris to match the dream: one pane per Vision, the first
        /// <c>visionsFinished.Count</c> of them lit in the order they finished, gold for golden.
        /// Cheap when nothing has changed.</summary>
        void PaintIris()
        {
            var s = GameState.I;
            if (s == null || iris == null) return;
            int total = s.VisionTotal;
            if (iris.Count == total && _lit == s.visionsFinished.Count) return;
            iris.Build(total);
            _greats.Clear();
            foreach (var k in s.visionsFinished)
            {
                var d = s.FindVision(k);
                _greats.Add(d != null && d.great);
            }
            _lit = s.visionsFinished.Count;
            iris.Show(_lit, _greats);
        }

        // Resting on a lit pane opens its plaque. Stepping off the pane does not close it: the
        // plaque closes on leaving the field, or on leaving the plaque once inside it, so the
        // walk down from the iris is free, and the pointer can step from pane to pane.
        void OnPaneHover(int i, bool entering)
        {
            if (entering) ShowPlaque(i);
        }

        // A finished one-off leaves the field; its pane of the iris has already lit.
        void Retire(VisionDef d)
        {
            var s = GameState.I;
            if (s == null) return;
            if (d != null) d.a = false;
            var m = _marks.Find(x => x.Def == d);
            if (m != null)
            {
                if (_open == m) Close();
                _marks.Remove(m);
                Destroy(m.gameObject);
            }
            // The dream has already taken it off the field and lit its pane.
            _built = Signature();
            // Finishing one Vision often shows the next in its chain on the same beat (The Duck
            // on the Wall reveals Saying It Out Loud). That Vision is in the signature just
            // recorded but has no mark yet, so nothing would ever draw it: build it now.
            foreach (var v in s.visions)
                if (s.Shown(v) && !_marks.Exists(x => x.Def == v)) { Rebuild(); break; }
            PaintIris();
            Close();
            PaintCaption();
            s.Dirty();
        }

        void ShowPlaque(int i)
        {
            var s = GameState.I;
            if (s == null || i < 0 || i >= s.visionsFinished.Count || plaque == null) return;
            if (_plaqueIndex == i && plaque.activeSelf) return;
            var d = s.FindVision(s.visionsFinished[i]);
            if (d == null) return;
            Close();                                  // a readout and a plaque are never both open
            _plaqueIndex = i;
            _plaqueTouched = false;
            _plaqueCloseAt = -1f;

            // The plaque takes the Vision's tier: gold for a golden one, iris for the rest. Each
            // part is rebound to its token rather than painted, so a palette swap keeps the tier.
            bool great = d.great;
            if (plaqueKind != null)
                plaqueKind.text = Strings.T(great ? "ui.visions.record.golden" : "ui.visions.record.plain").ToUpperInvariant();
            Tint(plaqueKind, great ? Tok.GoldD : Tok.IrisD);
            Tint(plaqueMarkGround, great ? Tok.GoldL : Tok.IrisL);
            Tint(plaqueMarkRule, great ? Tok.GoldB : Tok.IrisB);
            Tint(plaqueBorder, great ? Tok.GoldB : Tok.IrisB);
            if (plaqueShadow != null)
                Tint(plaqueShadow.GetComponent<Image>(), great ? Tok.GoldD : Tok.Ink, great ? 0.4f : 0.45f);
            if (plaqueGlyph != null) Art.Apply(plaqueGlyph, Art.Vision(d.k));
            Tint(plaqueGlyph, great ? Tok.GoldD : Tok.IrisD);
            if (plaqueName != null) plaqueName.text = d.n;
            if (plaqueBlurb != null) plaqueBlurb.text = d.bl;
            if (plaqueEffects != null && effectRowPrefab != null)
            {
                foreach (Transform c in plaqueEffects)
                    if (c.name != "Caption") Destroy(c.gameObject);
                string teal = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.TealD));
                foreach (var f in d.fx)
                {
                    var t = Instantiate(effectRowPrefab, plaqueEffects).GetComponentInChildren<TMP_Text>();
                    if (t == null) continue;
                    t.text = f.Replace("<b>", "<b><color=" + teal + ">").Replace("</b>", "</color></b>");
                    t.color = Theme.Get(Tok.Prose);
                }
            }
            plaque.SetActive(true);
            if (plaqueShadow != null) plaqueShadow.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)plaque.transform);

            if (fieldCaption != null) fieldCaption.alpha = 0f;
            foreach (var other in _marks)
                if (other.group != null) other.group.alpha = 0.4f;
        }

        static void Tint(Graphic g, Tok t, float a = 1f)
        {
            if (g == null) return;
            var bound = g.GetComponent<Ursine.Theming.ThemedGraphic>();
            if (bound != null) bound.Bind((int)t, a);
            else g.color = Theme.Get(t, a);
        }

        void ClosePlaque()
        {
            _plaqueIndex = -1;
            _plaqueTouched = false;
            _plaqueCloseAt = -1f;
            if (plaque != null) plaque.SetActive(false);
            if (plaqueShadow != null) plaqueShadow.gameObject.SetActive(false);
            if (_open == null)
            {
                if (fieldCaption != null) fieldCaption.alpha = 1f;
                foreach (var other in _marks)
                    if (other.group != null) other.group.alpha = 1f;
            }
        }

        // Before the pointer has touched the readout it closes only when the pointer leaves
        // the whole field, so crossing from a mark to it is free. Once it has been inside,
        // stepping off closes it unless a mark is reached within about a sixth of a second.
        void OnMarkHover(VisionMarkView m, bool entering)
        {
            if (entering)
            {
                _closeAt = -1f;
                if (_open != m) Open(m);
            }
            else if (_touchedReadout && _open == m)
            {
                _closeAt = Time.unscaledTime + Grace;
            }
        }

        void OnReadoutHover(bool entering)
        {
            if (entering) { _touchedReadout = true; _closeAt = -1f; }
            else if (_open != null) _closeAt = Time.unscaledTime + Grace;
        }

        // The eye drops to .5, the caption goes, and the other marks drop to .4.
        void Open(VisionMarkView m)
        {
            if (_plaqueIndex >= 0) ClosePlaque();
            _open = m;
            _builtFor = null;
            if (readout != null) readout.SetActive(true);
            Paint();
            if (eyeGroup != null) eyeGroup.alpha = 0.5f;
            if (fieldCaption != null) fieldCaption.alpha = 0f;
            foreach (var other in _marks)
                if (other.group != null) other.group.alpha = other == m ? 1f : 0.4f;
        }

        void Close()
        {
            _open = null;
            _touchedReadout = false;
            _closeAt = -1f;
            if (readout != null) readout.SetActive(false);
            if (eyeGroup != null) eyeGroup.alpha = 1f;
            if (fieldCaption != null) fieldCaption.alpha = 1f;
            foreach (var other in _marks)
                if (other.group != null) other.group.alpha = 1f;
            PaintCaption();
        }

        void PaintCaption()
        {
            var s = GameState.I;
            if (fieldCaption == null || s == null) return;
            int ch = 0;
            foreach (var v in s.visions) if (v.a) ch++;
            fieldCaption.text = Strings.T("ui.visions.caption", s.visions.Count(v => s.Shown(v)))
                              + (ch > 0 ? Strings.T("ui.visions.channelling", Strings.Number(ch)) : "");
        }

        VisionDef _builtFor;                    // which Vision the rows were built for
        readonly List<GameObject> _offerRows = new List<GameObject>();

        void Paint()
        {
            var d = _open?.Def;
            var s = GameState.I;
            if (d == null || s == null) return;
            bool great = d.great, full = d.p >= 100f;

            // --- the tier: iris, or gold for a golden Vision
            Tok accent = great ? Tok.GoldD : Tok.IrisD;
            if (readoutBorder != null) readoutBorder.color = Theme.Get(great ? Tok.GoldB : Tok.Haze);
            if (readoutShadow != null)
                readoutShadow.color = great ? Theme.Get(Tok.GoldD, 0.40f) : Theme.Get(Tok.Ink, 0.45f);
            if (rightGround != null) rightGround.color = great ? RightGroundGreat : RightGround;
            if (kindCaption != null)
            {
                kindCaption.text = Strings.T(great ? "ui.visions.kind.golden" : d.rep ? "ui.visions.kind.repeatable"
                                                   : "ui.visions.kind.plain").ToUpperInvariant();
                kindCaption.color = Theme.Get(accent);
            }
            if (mark != null) { Art.Apply(mark, Art.Vision(d.k)); mark.color = Theme.Get(accent); }

            // --- left: what it is and how far along
            if (visionName != null) visionName.text = d.n;
            if (blurb != null) blurb.text = d.bl;
            if (repeatLine != null)
            {
                repeatLine.gameObject.SetActive(d.done > 0);
                string nth = Strings.Ordinal(d.done + 1);
                int up = Mathf.RoundToInt((float)((System.Math.Pow(1.2, d.done) - 1.0) * 100.0));
                repeatLine.text = Strings.T("ui.visions.repeat", nth, up);
            }
            if (percentLabel != null)
            {
                percentLabel.text = Mathf.FloorToInt(d.p) + "%";
                percentLabel.color = Theme.Get(accent);
            }
            if (percentBar != null)
            {
                percentBar.Set(Mathf.Min(100f, d.p) / 100f);
                var fill = percentBar.fill != null ? percentBar.fill.GetComponent<Image>() : null;
                if (fill != null) fill.color = Theme.Get(great ? Tok.Gold : Tok.Iris);
            }

            if (_builtFor != d)
            {
                BuildRows(d);
                _builtFor = d;
            }

            // --- right: the offers, the pour, the channel
            if (rightCaption != null) rightCaption.text = full ? "FINISHED" : "POUR";
            for (int i = 0; i < _offerRows.Count && i < d.of.Count; i++) PaintOffer(_offerRows[i], d, i);

            var offer = d.of.Count > 0 ? d.of[Mathf.Clamp(d.sel, 0, d.of.Count - 1)] : null;
            double cost = offer == null ? 0 : d.OfferCost(offer);
            bool payable = offer != null && s.Held(offer.r) >= cost && !full;

            if (pour != null) pour.SetInteractable(full ? d.rep : payable);
            if (pourLabel != null) pourLabel.text = Strings.T(full ? (d.rep ? "ui.visions.again" : "ui.visions.complete") : "ui.visions.pour");
            if (pourGround != null && pourLabel != null)
            {
                if (full && !d.rep)
                {
                    // A finished one-off becomes a plaque in its own tier's color.
                    pourGround.color = Theme.Get(great ? Tok.GoldL : Tok.IrisL);
                    pourLabel.color = Theme.Get(accent);
                }
                else if (full || payable)
                {
                    pourGround.color = Theme.Get(great ? Tok.GoldD : Tok.Iris);
                    pourLabel.color = Theme.Get(Tok.OnIris);
                }
                else
                {
                    pourGround.color = Theme.Get(Tok.Track);
                    pourLabel.color = DisabledInk;
                }
            }

            if (channel != null)
            {
                channel.onTrackToken = (int)(great ? Tok.Gold : Tok.Iris);
                channel.Set(d.a, false);
                if (channel.button != null) channel.button.SetInteractable(!full);
            }
            if (channelHint != null)
            {
                bool warn = false;
                string hint;
                if (full) hint = Strings.T("ui.visions.hint.full");
                else if (!d.a) hint = Strings.T("ui.visions.hint.idle");
                else if (!payable)
                {
                    var res = offer != null ? s.Find(offer.r) : null;
                    hint = Strings.T("ui.visions.hint.waiting", res != null ? res.n : offer?.r);
                    warn = true;
                }
                else
                {
                    var res = s.Find(offer.r);
                    hint = Strings.T("ui.visions.hint.pouring", Fmt.Amount(cost), res != null ? res.n : offer.r);
                }
                channelHint.text = hint;
                channelHint.color = Theme.Get(warn ? Tok.RoseD : Tok.Ink3);
            }

            if (readout != null) FitReadout();
        }

        /// <summary>336 tall, or taller when the left column needs it: a long thought pushes the
        /// progress and the completion panel down rather than running underneath them. The
        /// readout is centered, so it grows from the middle out.</summary>
        void FitReadout()
        {
            var rt = (RectTransform)readout.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            if (readoutLeft == null) return;
            float h = Mathf.Max(ReadoutHeight, Mathf.Ceil(LayoutUtility.GetPreferredHeight(readoutLeft)));
            if (Mathf.Abs(rt.sizeDelta.y - h) < 0.5f) return;
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        void BuildRows(VisionDef d)
        {
            if (effects != null && effectRowPrefab != null)
            {
                // Switched off before they go: Destroy waits for the frame's end, and the readout
                // is measured before then.
                foreach (Transform c in effects)
                    if (c.name != "Caption") { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                string teal = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.TealD));
                foreach (var f in d.fx)
                {
                    var go = Instantiate(effectRowPrefab, effects);
                    var t = go.GetComponentInChildren<TMP_Text>();
                    if (t == null) continue;
                    t.text = f.Replace("<b>", "<b><color=" + teal + ">").Replace("</b>", "</color></b>");
                    t.color = Theme.Get(Tok.Prose);
                }
            }

            _offerRows.Clear();
            if (offers == null || offerRowPrefab == null) return;
            foreach (Transform c in offers) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
            for (int i = 0; i < d.of.Count; i++)
            {
                int captured = i;
                var go = Instantiate(offerRowPrefab, offers);
                ResourceHover.On(go, d.of[i].r);            // lights its row in the ledger
                var btn = go.GetComponent<UiButton>();
                if (btn != null) btn.Clicked += () => { if (_open?.Def == d) { d.sel = captured; Paint(); } };
                _offerRows.Add(go);
            }
        }

        // Selected is iris (gold on a golden Vision); an offer you cannot pay is rose, and a
        // golden selection keeps its gold even when short, as the spec's cascade does.
        void PaintOffer(GameObject row, VisionDef d, int i)
        {
            var s = GameState.I;
            var offer = d.of[i];
            double n = d.OfferCost(offer);
            bool on = d.sel == i, ok = s.Held(offer.r) >= n;
            var res = s.Find(offer.r);

            Tok? ground = null, dot = null;
            Tok border = Tok.Haze2, pip = Tok.Ink4;
            Tok ink = Tok.Prose;
            if (!ok) { ground = Tok.RoseL; border = Tok.RoseB; pip = Tok.RoseB; ink = Tok.RoseD; }
            if (on)
            {
                if (d.great) { ground = Tok.GoldL; border = Tok.GoldB; pip = Tok.Gold; dot = Tok.GoldD; }
                else if (ok) { ground = Tok.IrisL; border = Tok.IrisB; pip = Tok.Iris; dot = Tok.Iris; }
                else { pip = Tok.Rose; dot = Tok.RoseD; }
            }

            var groundImg = row.GetComponent<Image>();
            if (groundImg != null) groundImg.color = ground.HasValue ? Theme.Get(ground.Value) : Theme.Get(Tok.Lit);
            SetColor(row, "Border", Theme.Get(border));
            SetColor(row, "Pip", Theme.Get(pip));
            var dotT = row.transform.Find("Pip/Dot");
            if (dotT != null)
            {
                dotT.gameObject.SetActive(dot.HasValue);
                if (dot.HasValue) dotT.GetComponent<Image>().color = Theme.Get(dot.Value);
            }
            var glyphT = row.transform.Find("Glyph");
            if (glyphT != null)
            {
                var g = glyphT.GetComponent<Image>();
                Art.Apply(g, Art.Resource(offer.r));
                g.color = Theme.Get(ink, 0.8f);
            }
            SetText(row, "Amount", Fmt.Amount(n), Theme.Get(ink));
            SetText(row, "Resource", res != null ? res.n : offer.r, Theme.Get(ink));
            SetText(row, "Gain", "+" + Fmt.Count(offer.g) + "%", Theme.Get(Tok.TealD));
        }

        static void SetColor(GameObject row, string child, Color c)
        {
            var t = row.transform.Find(child);
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img != null) img.color = c;
        }

        static void SetText(GameObject row, string child, string text, Color c)
        {
            var t = row.transform.Find(child);
            var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
            if (tmp == null) return;
            tmp.text = text;
            tmp.color = c;
        }
    }
}
