// What Lies In The Depths — one Revelation sigil, drifting on its ring. Spec section 6.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine.Economy;

namespace WhatLiesInTheDepths.UI
{
    public sealed class SigilView : MonoBehaviour
    {
        public UiButton button;
        public Image glyph;
        public Image halo;
        public Image greaterRing;
        public CanvasGroup group;

        public RevelationDef Def { get; private set; }

        // The spec's own drift, per sigil (_Whole Screen.html, the Revelations build()).
        bool _outer;                       // the 388 ring rather than the 306
        float _a0, _r;                     // starting angle, ring radius
        float _w1, _w2, _p1, _p2, _amp;    // the slow figure each sigil wanders on
        float _calm;                       // 0 drifting, 1 held
        Vector2 _held;                     // where it was when the pointer arrived
        Vector2 _shown;                    // where it is drawn now

        public System.Action<SigilView, bool> HoverChanged;

        bool _hover;
        float _ringShow;                   // 0 hidden at .88, 1 shown at full size; .18s

        // The halo is light, not a surface: the spec's raw rgba, not a token.
        static readonly Color LilacLight = new Color32(156, 134, 206, 255);
        static readonly Color GoldLight  = new Color32(214, 172, 110, 255);
        static readonly Color GreatHover = new Color32(0x6E, 0x53, 0x16, 255);
        static readonly Color ShortInk   = new Color32(0x8E, 0x86, 0xA3, 255);

        /// <summary>Above a ceiling the whole sigil sits at .45; the field multiplies its own
        /// yielding on top of this.</summary>
        public float BaseAlpha => Def != null && Def.state == Refusal.AboveCeiling ? 0.45f : 1f;

        public void Bind(RevelationDef d, int index)
        {
            Def = d;
            Art.Apply(glyph, Art.Sigil(string.IsNullOrEmpty(d.g) ? d.k : d.g));

            _outer = d.ring > 350f;
            _r = d.ring;                                   // a radius, not a diameter
            _a0 = d.angle;
            _w1 = 0.19f + index * 0.031f;
            _w2 = 0.13f + index * 0.024f;
            _p1 = index * 1.7f;
            _p2 = index * 2.3f;
            _amp = 13f + (index % 3) * 5f;                 // the spec's ±13–23px

            if (button != null)
                button.Hovered += h =>
                {
                    // Hold where it is — under the pointer — before anything else reacts.
                    if (h) _held = _shown;
                    _hover = h;
                    Paint();
                    HoverChanged?.Invoke(this, h);
                };
            Paint();
        }

        public void Paint()
        {
            if (Def == null) return;
            bool great = Def.great, isShort = Def.state == Refusal.Short;

            if (glyph != null)
                glyph.color = _hover ? (great ? GreatHover : Theme.Get(Tok.Ink))
                            : isShort ? ShortInk
                            : Theme.Get(great ? Tok.GoldD : Tok.IrisD);

            if (halo != null)
            {
                var c = great ? GoldLight : LilacLight;
                c.a = great ? (_hover ? 0.40f : 0.22f) : (_hover ? 0.30f : 0.16f);
                if (isShort && !_hover) c.a *= 0.35f;
                halo.color = c;
            }

            if (greaterRing != null)
                greaterRing.color = great ? Theme.Get(_hover ? Tok.Gold : Tok.GoldB) : Theme.Get(Tok.IrisB);
            ApplyRing();
        }

        void ApplyRing()
        {
            if (greaterRing == null || Def == null) return;
            float shown = Def.great ? Mathf.Lerp(0.55f, 1f, _ringShow) : _ringShow;
            var c = greaterRing.color; c.a = shown; greaterRing.color = c;
            float scale = Def.great ? 1f : Mathf.Lerp(0.88f, 1f, _ringShow);
            greaterRing.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>One frame of drift. <paramref name="t"/> is the field's clock in seconds.
        ///
        /// Resting on a sigil brings it almost to rest: it eases to a near-stop over about
        /// a third of a second and holds WHERE IT WAS when the pointer arrived, with a
        /// whisper of residual motion so it does not look frozen. Letting go, it eases back
        /// onto its path. This is the spec's own arithmetic, line for line.</summary>
        public void Tick(float t, float dt, bool open)
        {
            // n.calm += (n.want - n.calm) * 0.09 per 60fps frame, made frame-rate independent.
            float ringWant = _hover ? 1f : 0f;
            if (!Mathf.Approximately(_ringShow, ringWant))
            {
                _ringShow = Mathf.MoveTowards(_ringShow, ringWant, dt / 0.18f);
                ApplyRing();
            }

            float want = open ? 1f : 0f;
            _calm += (want - _calm) * (1f - Mathf.Pow(1f - 0.09f, dt * 60f));

            // Two rings turning in opposite directions, about one revolution every six
            // minutes; each ring a slightly flattened ellipse so it fits the 720 field.
            float a = _a0 + (_outer ? -t * 0.012f : t * 0.017f);
            float x = Mathf.Cos(a) * _r * (_outer ? 1.02f : 1f) + Mathf.Sin(t * _w1 + _p1) * _amp;
            float y = Mathf.Sin(a) * _r * (_outer ? 0.84f : 0.90f) + Mathf.Sin(t * _w2 + _p2) * _amp * 0.9f;

            // Nothing drifts through the readout's footprint (668 x 368 at the center): a
            // sigil that would is pushed out along its own ray, so it appears to skirt it.
            const float EW = 334f, EH = 184f;
            if (Mathf.Abs(x) < EW && Mathf.Abs(y) < EH)
            {
                float push = Mathf.Min(EW / Mathf.Max(Mathf.Abs(x), 0.001f),
                                       EH / Mathf.Max(Mathf.Abs(y), 0.001f));
                x *= push; y *= push;
            }

            // The spec measures y downward; a RectTransform measures it up.
            var drift = new Vector2(x, -y);

            // Held, but never quite dead.
            float k = _calm * 0.94f;
            _shown = drift + (_held - drift) * k;
            ((RectTransform)transform).anchoredPosition = _shown;
        }
    }
}
