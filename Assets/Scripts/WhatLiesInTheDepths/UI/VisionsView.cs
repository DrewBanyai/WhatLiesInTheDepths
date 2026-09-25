// What Lies In The Depths — center destination. Spec section 8.
// In this menu the question is never what is this but how far along is it, so every mark
// carries its own progress around its rim. Gold means the tier here, not the finish:
// completion is marked by the ring closing, which needs no color.
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
        public RectTransform irisRing;
        public UiButton eyeHover;

        [Header("Readout — 620 x 336, dead center, two columns with a rule between")]
        public GameObject readout;
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

        [Header("Plaque — a finished golden Vision, read back out of the iris")]
        public GameObject plaque;
        public UiButton plaqueHover;
        public RectTransform plaqueShadow;
        public Image plaqueGlyph;
        public TMP_Text plaqueName;
        public TMP_Text plaqueBlurb;
        public RectTransform plaqueEffects;

        [Header("Pointer — the field and the readout each report entering and leaving")]
        public UiButton fieldHover;
        public UiButton readoutHover;

        // The spec's raw values where it does not use a token.
        static readonly Color RightGround      = new Color32(0xFB, 0xF9, 0xFD, 0xFF);
        static readonly Color RightGroundGreat = new Color32(0xFC, 0xFA, 0xF4, 0xFF);
        static readonly Color DisabledInk      = new Color32(0xB6, 0xAE, 0xCB, 0xFF);

        // The iris: golden Visions already finished travel clockwise on a ring of 52 inside
        // it, upright, once every 175 seconds, and now and then one warms to gold.
        sealed class Kept { public VisionDef def; public RectTransform rt; public Image glyph; public float next; }
        readonly List<Kept> _kept = new List<Kept>();
        static readonly Color KeptInk   = new Color32(0x8D, 0x74, 0xB4, 0xFF);
        static readonly Color KeptWarm  = new Color32(0xD6, 0xAC, 0x6E, 0xFF);
        static readonly Color KeptRead  = new Color32(0x8A, 0x6A, 0x2F, 0xFF);
        const float KeptAlpha = 0.66f, KeptWarmAlpha = 0.96f, KeptDur = 2.0f, IrisRadius = 52f;
        float _clock;
        int _plaqueIndex = -1;
        bool _plaqueTouched;
        float _plaqueCloseAt = -1f;

        // The Revelations rule exactly (spec section 8).
        bool _touchedReadout;
        float _closeAt = -1f;
        const float Grace = 1f / 6f;

        readonly List<VisionMarkView> _marks = new List<VisionMarkView>();
        VisionMarkView _open;
        float _orbit, _irisOrbit;
        bool _eyeRested;

        void Start()
        {
            Build();
            BuildIris();
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
                eyeHover.Hovered += h => _eyeRested = h;   // resting anywhere on the eye stops the ring dead

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
            BuildIris();
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

            // The iris ring stops dead while the pointer is anywhere on the eye or on an
            // open plaque.
            _clock += Time.unscaledDeltaTime;
            if (!_eyeRested && _plaqueIndex < 0)
                _irisOrbit -= Time.unscaledDeltaTime * 0.036f;      // clockwise, y up
            for (int i = 0; i < _kept.Count; i++)
            {
                var k = _kept[i];
                float a = _irisOrbit + i * Mathf.PI * 2f / _kept.Count;
                k.rt.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * IrisRadius;
                k.rt.rotation = Quaternion.identity;               // staying upright
                Remember(k, i);
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
                // menu was elsewhere) goes; a Vision newly shown gets its mark; a golden one
                // just kept joins the iris.
                foreach (var m in _marks.ToArray())
                    if (m.Def == null || !s.visions.Contains(m.Def)) { Retire(m.Def); return; }
                if (Signature() != _built) { Rebuild(); return; }
                if (_kept.Count != s.visionsAbsorbed.Count) BuildIris();
            }
            PaintCaption();
            if (_open != null) Paint();              // the purse moved: affordability did too
        }

        void LateUpdate()
        {
            if (_closeAt >= 0f && Time.unscaledTime >= _closeAt) Close();
        }

        // Now and then one remembers: it warms to gold and cools back over two seconds.
        void Remember(Kept k, int i)
        {
            if (k.glyph == null) return;
            if (_plaqueIndex >= 0)
            {
                bool read = i == _plaqueIndex;
                k.glyph.color = read ? KeptRead : new Color(KeptInk.r, KeptInk.g, KeptInk.b, 0.28f);
                return;
            }
            float d = _clock - k.next;
            if (d > 0f && d < KeptDur)
            {
                float u = Mathf.Sin(d / KeptDur * Mathf.PI);
                var c = Color.Lerp(KeptInk, KeptWarm, u);
                c.a = Mathf.Lerp(KeptAlpha, KeptWarmAlpha, u);
                k.glyph.color = c;
            }
            else
            {
                if (d >= KeptDur) k.next = _clock + 8f + Random.value * 14f;
                k.glyph.color = new Color(KeptInk.r, KeptInk.g, KeptInk.b, KeptAlpha);
            }
        }

        void BuildIris()
        {
            var s = GameState.I;
            if (s == null || irisRing == null) return;
            foreach (Transform c in irisRing) Destroy(c.gameObject);
            _kept.Clear();
            for (int i = 0; i < s.visionsAbsorbed.Count; i++)
            {
                var d = s.visionsAbsorbed[i];
                // A 34 hit circle holding the Vision's glyph at .82 of its 28 grid.
                var go = new GameObject("Kept " + d.k, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(irisRing, false);
                rt.sizeDelta = new Vector2(34f, 34f);
                var hit = go.GetComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                var btn = go.AddComponent<UiButton>();
                int captured = i;
                btn.Hovered += h => { if (h) ShowPlaque(captured); };

                var g = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
                var grt = (RectTransform)g.transform;
                grt.SetParent(rt, false);
                grt.sizeDelta = new Vector2(23f, 23f);
                var img = g.GetComponent<Image>();
                img.raycastTarget = false;
                Art.Apply(img, Art.Vision(d.k));
                img.color = new Color(KeptInk.r, KeptInk.g, KeptInk.b, KeptAlpha);

                _kept.Add(new Kept { def = d, rt = rt, glyph = img, next = _clock + 3f + i * 4.5f + Random.value * 6f });
            }
        }

        // A finished one-off leaves the field. A golden one is taken into the eye.
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
            // The dream has already taken it off the field and, if golden, into the eye.
            _built = Signature();
            if (_kept.Count != s.visionsAbsorbed.Count) BuildIris();
            Close();
            PaintCaption();
            s.Dirty();
        }

        void ShowPlaque(int i)
        {
            var s = GameState.I;
            if (s == null || i < 0 || i >= s.visionsAbsorbed.Count || plaque == null) return;
            if (_plaqueIndex == i && plaque.activeSelf) return;
            Close();                                  // a readout and a plaque are never both open
            _plaqueIndex = i;
            _plaqueTouched = false;
            _plaqueCloseAt = -1f;
            var d = s.visionsAbsorbed[i];

            if (plaqueGlyph != null) Art.Apply(plaqueGlyph, Art.Vision(d.k));
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
                    pourLabel.color = Theme.Get(Tok.Veil);
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
                    hint = Strings.T("ui.visions.hint.pouring", Fmt.Count(cost), res != null ? res.n : offer.r);
                }
                channelHint.text = hint;
                channelHint.color = Theme.Get(warn ? Tok.RoseD : Tok.Ink3);
            }

            if (readout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)readout.transform);
        }

        void BuildRows(VisionDef d)
        {
            if (effects != null && effectRowPrefab != null)
            {
                foreach (Transform c in effects)
                    if (c.name != "Caption") Destroy(c.gameObject);
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
            foreach (Transform c in offers) Destroy(c.gameObject);
            for (int i = 0; i < d.of.Count; i++)
            {
                int captured = i;
                var go = Instantiate(offerRowPrefab, offers);
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
            if (groundImg != null) groundImg.color = ground.HasValue ? Theme.Get(ground.Value) : Color.white;
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
            SetText(row, "Amount", Fmt.Count(n), Theme.Get(ink));
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
