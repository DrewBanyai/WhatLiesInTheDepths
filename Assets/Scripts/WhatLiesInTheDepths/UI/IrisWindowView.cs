// What Lies In The Depths — the iris window at the center of the Visions field. Visions spec,
// section 1.
//
// The eye stays line art; its iris is a rose window of glass, one pane for every Vision the
// dream holds, dark until that Vision is finished for the first time. Each first finish lights
// the next pane clockwise from the top, in the order they were finished; a golden Vision's pane
// burns gold. The eye's light grows with what is lit. Resting on a lit pane reports it, so the
// plaque can read that Vision back; a dark pane reports nothing.
//
// The glass is drawn over the eye art's iris, and the iris's two rings and the pupil's catch-
// light are drawn again on top of it, so the leading reads as part of the eye. Colors come from
// the palette's tokens, so a palette change reaches them; the light itself is white, as the
// lantern's is.
using System;
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class IrisWindowView : MonoBehaviour
    {
        /// <summary>The iris, in eye units (the eye art is 480 x 250, 1:1 on the canvas).</summary>
        public const float Inner = 34f, Outer = 70f;

        [Tooltip("The eye's light: a bloom behind it, grown and brightened by what is lit.")]
        public Image glow;
        [Tooltip("The glow's size with nothing lit and with everything lit.")]
        public Vector2 glowSizeDark = new Vector2(460f, 300f), glowSizeLit = new Vector2(720f, 460f);

        /// <summary>A lit pane gained or lost the pointer: its index in the order panes light.</summary>
        public event Action<int, bool> PaneHovered;

        public int Count => _panes.Count;

        readonly List<PolygonGraphic> _panes = new List<PolygonGraphic>();
        readonly List<bool> _great = new List<bool>();
        PolygonGraphic _rings, _catch;
        int _lit, _hover = -1;

        static readonly Color Light = Color.white;
        // The eye art is drawn in fixed colors rather than the palette's (it is art, not UI);
        // what redraws parts of it uses the same ones.
        static readonly Color EyeLine = new Color32(0x8A, 0x72, 0xA8, 0xFF);

        void OnEnable() => Theme.Changed += Paint;
        void OnDisable() => Theme.Changed -= Paint;

        PolygonGraphic Layer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Outer * 2f, Outer * 2f);
            var g = go.AddComponent<PolygonGraphic>();
            g.raycastTarget = false;
            return g;
        }

        /// <summary>One pane: the part of the ring between two angles, as convex pieces (a wedge
        /// of a ring is not convex, a thin slice of one is). Angles in radians, y up.</summary>
        public static List<Vector2[]> Wedge(float a0, float a1)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) / (Mathf.PI / 36f)));
            var pieces = new List<Vector2[]>(n);
            for (int i = 0; i < n; i++)
            {
                float u = Mathf.Lerp(a0, a1, (float)i / n), v = Mathf.Lerp(a0, a1, (float)(i + 1) / n);
                var du = new Vector2(Mathf.Cos(u), Mathf.Sin(u));
                var dv = new Vector2(Mathf.Cos(v), Mathf.Sin(v));
                pieces.Add(new[] { du * Inner, du * Outer, dv * Outer, dv * Inner });
            }
            return pieces;
        }

        /// <summary>Draws the window with <paramref name="count"/> panes. Does nothing if it
        /// already has that many.</summary>
        public void Build(int count)
        {
            if (count == _panes.Count && _rings != null) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.GetComponent<PolygonGraphic>() == null) continue;   // only what this drew
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
            _panes.Clear();
            _great.Clear();
            _hover = -1;

            // The first pane starts at twelve o'clock; they run clockwise.
            float step = count > 0 ? Mathf.PI * 2f / count : 0f;
            for (int i = 0; i < count; i++)
            {
                float a0 = Mathf.PI * 0.5f - i * step, a1 = a0 - step;
                var g = Layer("Pane" + i);
                g.SetPieces(Wedge(a0, a1));
                g.radial = true;
                g.center = Vector2.zero;
                g.radius = Outer + 6f;
                g.strokeWidth = 1.3f;          // its share of the leading
                g.raycastTarget = true;
                var button = g.gameObject.AddComponent<UiButton>();
                int index = i;
                button.Hovered += h => OnPane(index, h);
                _panes.Add(g);
                _great.Add(false);
            }

            // The iris's own rings, back on top of the glass, and the pupil's catchlight, which
            // the art lets spill a little past the pupil.
            _rings = Layer("Rings");
            _rings.fill = false;
            _rings.strokeWidth = 2.2f;
            _rings.strokeShared = true;
            _rings.SetPieces(new[] { PolygonGraphic.Circle(Vector2.zero, Outer, 96), PolygonGraphic.Circle(Vector2.zero, Inner, 64) });

            _catch = Layer("Catchlight");
            _catch.SetPiece(PolygonGraphic.Circle(new Vector2(22f, 19f), 8.5f, 28));

            Paint();
        }

        /// <summary>Lights the first <paramref name="lit"/> panes, in order; a pane whose Vision
        /// was a golden one burns gold.</summary>
        public void Show(int lit, IList<bool> great)
        {
            _lit = Mathf.Clamp(lit, 0, _panes.Count);
            for (int i = 0; i < _great.Count; i++) _great[i] = great != null && i < great.Count && great[i];
            if (_hover >= _lit) _hover = -1;
            Paint();
        }

        void OnPane(int i, bool entering)
        {
            if (i >= _lit) return;                // a dark pane has nothing to say
            if (entering) _hover = i;
            else if (_hover == i) _hover = -1;
            PaintPane(i);
            // The hovered pane's leading is drawn over its neighbours'; the rings stay on top.
            if (entering) _panes[i].transform.SetAsLastSibling();
            if (_rings != null) _rings.transform.SetAsLastSibling();
            if (_catch != null) _catch.transform.SetAsLastSibling();
            PaneHovered?.Invoke(i, entering);
        }

        public void Paint()
        {
            if (_rings == null) return;
            _rings.strokeColor = EyeLine;
            _rings.SetVerticesDirty();
            _catch.color = new Color(0.984f, 0.973f, 1f, 0.9f);
            _catch.SetVerticesDirty();
            for (int i = 0; i < _panes.Count; i++) PaintPane(i);

            if (glow != null)
            {
                float frac = _panes.Count > 0 ? (float)_lit / _panes.Count : 0f;
                glow.rectTransform.sizeDelta = Vector2.Lerp(glowSizeDark, glowSizeLit, frac);
                glow.color = new Color(1f, 1f, 1f, _lit > 0 ? 0.25f + 0.6f * frac : 0f);
            }
        }

        void PaintPane(int i)
        {
            var g = _panes[i];
            bool lit = i < _lit, hover = i == _hover;
            if (lit)
            {
                var tint = _great[i] ? Theme.Get(Tok.Gold) : Theme.Get(Tok.Iris);
                g.inner = _great[i] ? Color.Lerp(Light, Theme.Get(Tok.Gold), 0.12f) : Light;
                g.outer = Color.Lerp(Light, tint, hover ? 0.35f : _great[i] ? 0.9f : 0.55f);
            }
            else
            {
                var dark = Color.Lerp(Theme.Get(Tok.Ink3), Theme.Get(Tok.Haze), 0.2f);
                g.inner = dark;
                g.outer = Color.Lerp(dark, Theme.Get(Tok.Ink2), 0.18f);
            }
            g.strokeColor = hover ? Theme.Get(Tok.IrisD) : Theme.Get(Tok.IrisD, 0.55f);
            g.strokeWidth = hover ? 3f : 1.3f;
            g.SetVerticesDirty();
        }
    }
}
