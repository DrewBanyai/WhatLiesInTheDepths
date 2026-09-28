// What Lies In The Depths — the lantern in the Revelations field. Revelations spec, section 1.
//
// One pane for every Revelation that can be realized, dark until it is. Each realization lights
// the next pane out from the flame, in the order they were realized; a greater one burns gold,
// and one side of a choice burns blue.
// The lantern's own light grows with what is lit. Resting on a lit pane reports it, so the
// readout can read that realization back; a dark pane reports nothing.
//
// The frame, the glass and the flame are drawn here from Core/Lantern's shapes, in the palette's
// own tokens, so a palette change reaches them. The light itself is white: it is light, not a
// surface, the same exception the sigils' halos make.
using System;
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class LanternView : MonoBehaviour
    {
        [Tooltip("Lantern units to canvas units. The lantern is 374 units tall.")]
        public float scale = 0.95f;
        [Tooltip("The lantern's light: a radial bloom behind it, grown and brightened by what is lit.")]
        public Image glow;

        /// <summary>A lit pane gained or lost the pointer: its index in the order panes light.</summary>
        public event Action<int, bool> PaneHovered;

        public int Count => _panes.Count;

        readonly List<PolygonGraphic> _panes = new List<PolygonGraphic>();
        readonly List<bool> _great = new List<bool>();
        readonly List<bool> _choice = new List<bool>();
        PolygonGraphic _frame, _ring, _outline, _flame;
        int _lit, _hover = -1;
        float _t;

        static readonly Color Light = Color.white;

        void OnEnable() => Theme.Changed += Paint;
        void OnDisable() => Theme.Changed -= Paint;

        Vector2 P(Vector2 lantern) => new Vector2(lantern.x * scale, -lantern.y * scale);

        Vector2[] P(Vector2[] shape)
        {
            var o = new Vector2[shape.Length];
            for (int i = 0; i < shape.Length; i++) o[i] = P(shape[i]);
            return o;
        }

        PolygonGraphic Layer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(Lantern.Width * scale * 2f, Lantern.Height * scale);
            var g = go.AddComponent<PolygonGraphic>();
            g.raycastTarget = false;
            return g;
        }

        /// <summary>Draws the lantern with <paramref name="count"/> panes. Does nothing if it
        /// already has that many.</summary>
        public void Build(int count)
        {
            if (count == _panes.Count && _frame != null) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (glow != null && c == glow.transform) continue;
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
            _panes.Clear();
            _great.Clear();
            _choice.Clear();
            _hover = -1;

            // The ring it hangs from, then the roof, collars and foot, all behind the glass.
            _ring = Layer("Ring");
            _ring.fill = false;
            _ring.strokeWidth = 4f;
            _ring.SetPiece(PolygonGraphic.Circle(P(Lantern.RingCenter), Lantern.RingRadius * scale));

            _frame = Layer("Frame");
            _frame.strokeWidth = 2.2f;
            _frame.SetPieces(new[]
            {
                P(Lantern.Roof), P(Lantern.Collar), P(Lantern.BaseCollar), P(Lantern.Foot),
                PolygonGraphic.Circle(P(Lantern.FinialCenter), Lantern.FinialRadius * scale, 24),
            });

            var shapes = Lantern.Panes(count);
            for (int i = 0; i < shapes.Count; i++)
            {
                var g = Layer("Pane" + i);
                var pieces = new List<Vector2[]>();
                foreach (var piece in shapes[i].pieces) pieces.Add(P(piece));
                g.SetPieces(pieces);
                g.radial = true;
                g.center = P(Lantern.Flame);
                g.radius = 118f * scale;
                g.strokeWidth = 1.6f;          // its share of the leading
                g.raycastTarget = true;
                var button = g.gameObject.AddComponent<UiButton>();
                int index = i;
                button.Hovered += h => OnPane(index, h);
                _panes.Add(g);
                _great.Add(false);
                _choice.Add(false);
            }

            _flame = Layer("Flame");
            _flame.radial = true;
            _flame.center = P(Lantern.Flame + new Vector2(0f, 6f));
            _flame.radius = 46f * scale;
            _flame.SetPiece(Teardrop());

            _outline = Layer("Outline");
            _outline.fill = false;
            _outline.strokeWidth = 3f;
            _outline.SetPiece(P(Lantern.Body));

            Paint();
        }

        Vector2[] Teardrop()
        {
            var pts = new List<Vector2>();
            for (int i = 0; i < 48; i++)
            {
                float t = i / 48f * Mathf.PI * 2f;
                float x = Mathf.Sin(t) * Mathf.Pow(Mathf.Abs(Mathf.Sin(t * 0.5f)), 1.3f) * 22f;
                float y = -Mathf.Cos(t) * 46f;
                pts.Add(P(Lantern.Flame + new Vector2(x, y - 8f)));
            }
            return pts.ToArray();
        }

        /// <summary>Lights the first <paramref name="lit"/> panes, in order; a pane whose
        /// realization was a greater one burns gold, and one that was a side of a choice burns
        /// blue.</summary>
        public void Show(int lit, IList<bool> great, IList<bool> choice = null)
        {
            _lit = Mathf.Clamp(lit, 0, _panes.Count);
            for (int i = 0; i < _great.Count; i++) _great[i] = great != null && i < great.Count && great[i];
            for (int i = 0; i < _choice.Count; i++) _choice[i] = choice != null && i < choice.Count && choice[i] && !_great[i];
            if (_hover >= _lit) _hover = -1;
            Paint();
        }

        void OnPane(int i, bool entering)
        {
            if (i >= _lit) return;                // a dark pane has nothing to say
            if (entering) _hover = i;
            else if (_hover == i) _hover = -1;
            PaintPane(i);
            if (entering) _panes[i].transform.SetAsLastSibling();
            if (_flame != null) _flame.transform.SetAsLastSibling();
            if (_outline != null) _outline.transform.SetAsLastSibling();
            PaneHovered?.Invoke(i, entering);
        }

        public void Paint()
        {
            if (_frame == null) return;
            var iris = Theme.Get(Tok.Iris);
            _ring.strokeColor = Theme.Get(Tok.Iris, 0.85f);
            _ring.SetVerticesDirty();
            _frame.color = Theme.Get(Tok.Wait);
            _frame.strokeColor = Theme.Get(Tok.Iris, 0.85f);
            _frame.SetVerticesDirty();
            _outline.strokeColor = Theme.Get(Tok.Iris, 0.9f);
            _outline.SetVerticesDirty();
            for (int i = 0; i < _panes.Count; i++) PaintPane(i);

            float frac = _panes.Count > 0 ? (float)_lit / _panes.Count : 0f;
            _flame.inner = new Color(1f, 1f, 1f, _lit > 0 ? 0.35f + 0.5f * frac : 0f);
            _flame.outer = new Color(1f, 1f, 1f, 0f);
            _flame.SetVerticesDirty();

            if (glow != null)
            {
                float size = (300f + 360f * frac) * scale;
                glow.rectTransform.sizeDelta = new Vector2(size, size);
                glow.color = new Color(1f, 1f, 1f, _lit > 0 ? 0.25f + 0.65f * frac : 0f);
            }
        }

        void PaintPane(int i)
        {
            var g = _panes[i];
            bool lit = i < _lit, hover = i == _hover;
            if (lit)
            {
                // Gold for a greater realization, blue for a side of a choice, lilac for the rest.
                bool strong = _great[i] || _choice[i];
                var tint = Theme.Get(_great[i] ? Tok.Gold : _choice[i] ? Tok.Blue : Tok.Iris);
                g.inner = strong ? Color.Lerp(Light, tint, 0.12f) : Light;
                g.outer = Color.Lerp(Light, tint, hover ? 0.35f : strong ? 0.9f : 0.55f);
            }
            else
            {
                var dark = Color.Lerp(Theme.Get(Tok.Ink3), Theme.Get(Tok.Haze), 0.2f);
                g.inner = dark;
                g.outer = Color.Lerp(dark, Theme.Get(Tok.Ink2), 0.18f);
            }
            g.strokeColor = hover ? Theme.Get(Tok.IrisD) : Theme.Get(Tok.IrisD, 0.55f);
            g.strokeWidth = hover ? 3f : 1.6f;
            g.SetVerticesDirty();
        }

        void Update()
        {
            if (_flame == null || _lit == 0) return;
            // The flame is alive, barely: a slow breath with a little flutter in it.
            _t += Time.unscaledDeltaTime;
            float frac = (float)_lit / Mathf.Max(1, _panes.Count);
            float breath = 0.92f + 0.05f * Mathf.Sin(_t * 1.3f) + 0.03f * Mathf.PerlinNoise(_t * 2.1f, 0.5f);
            var c = _flame.inner;
            c.a = (0.35f + 0.5f * frac) * breath;
            _flame.inner = c;
            _flame.SetVerticesDirty();
        }
    }
}
