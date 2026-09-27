// Ursine — a filled shape for uGUI, made of convex pieces: a pane of glass, a roof, a ring.
//
// A shape is a list of convex polygons in this graphic's local space. Each is filled as a fan
// around its own centroid, so a radial gradient has a vertex to bend at, and the outline of
// the whole shape — every edge that is not shared by two of its own pieces — can be stroked,
// anti-aliased the way PathLine is. The pointer only counts as over the graphic inside one of
// its pieces, not anywhere in its rect, so shapes that sit edge to edge each get their own.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ursine.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PolygonGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        [Header("Fill")]
        public bool fill = true;
        [Tooltip("Colors the fill from inner at the center to outer at the radius. Off: the graphic's color.")]
        public bool radial;
        public Vector2 center;
        public float radius = 100f;
        public Color inner = Color.white, outer = Color.white;

        [Header("Stroke — the shape's outline")]
        public float strokeWidth;
        public Color strokeColor = Color.black;
        [Tooltip("Anti-aliasing: the width, in screen pixels, over which a stroke edge fades.")]
        public float feather = 1.25f;
        [Tooltip("Stroke every edge, shared or not. For drawing leading between pieces of one shape.")]
        public bool strokeShared;

        readonly List<Vector2[]> _pieces = new List<Vector2[]>();
        public IReadOnlyList<Vector2[]> Pieces => _pieces;

        public void SetPieces(IEnumerable<Vector2[]> pieces)
        {
            _pieces.Clear();
            foreach (var p in pieces) if (p != null && p.Length >= 3) _pieces.Add(p);
            SetVerticesDirty();
        }

        public void SetPiece(Vector2[] piece)
        {
            _pieces.Clear();
            if (piece != null && piece.Length >= 3) _pieces.Add(piece);
            SetVerticesDirty();
        }

        /// <summary>A circle as one convex piece, for a ring or a finial.</summary>
        public static Vector2[] Circle(Vector2 c, float r, int segments = 40)
        {
            var p = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                p[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return p;
        }

        /// <summary>Recolors without rebuilding anything but the mesh's colors.</summary>
        public void SetGradient(Color innerColor, Color outerColor)
        {
            inner = innerColor; outer = outerColor;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (fill)
                foreach (var p in _pieces) Fan(vh, p);
            if (strokeWidth > 0f) Stroke(vh);
        }

        Color FillAt(Vector2 v)
        {
            if (!radial) return color;
            float t = radius > 0f ? Mathf.Clamp01((v - center).magnitude / radius) : 1f;
            var c = Color.Lerp(inner, outer, t);
            c.a *= color.a;
            return c;
        }

        void Fan(VertexHelper vh, Vector2[] p)
        {
            var c = Vector2.zero;
            foreach (var v in p) c += v;
            c /= p.Length;
            int start = vh.currentVertCount;
            vh.AddVert(c, FillAt(c), Vector2.zero);
            foreach (var v in p) vh.AddVert(v, FillAt(v), Vector2.zero);
            for (int i = 0; i < p.Length; i++)
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % p.Length);
        }

        // ---- the outline ----------------------------------------------------------------

        static bool Same(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 0.01f;

        void Stroke(VertexHelper vh)
        {
            var edges = new List<(Vector2 a, Vector2 b)>();
            foreach (var p in _pieces)
                for (int i = 0; i < p.Length; i++)
                {
                    var a = p[i]; var b = p[(i + 1) % p.Length];
                    if (Same(a, b)) continue;
                    int twin = edges.FindIndex(e => (Same(e.a, b) && Same(e.b, a)) || (Same(e.a, a) && Same(e.b, b)));
                    if (twin >= 0) { if (!strokeShared) edges.RemoveAt(twin); continue; }
                    edges.Add((a, b));
                }

            float f = Mathf.Max(0f, feather) * CanvasUnitsPerPixel();
            float r = strokeWidth * 0.5f;
            float solid = Mathf.Max(0f, r - f * 0.5f), edge = r + f * 0.5f;
            var clear = strokeColor; clear.a = 0f;
            foreach (var (a, b) in edges)
            {
                var d = (b - a).normalized;
                var n = new Vector2(-d.y, d.x);
                // Run each stroke a little past its ends so neighbours meet without a notch.
                var a2 = a - d * r; var b2 = b + d * r;
                int s = vh.currentVertCount;
                vh.AddVert(a2 + n * edge, clear, Vector2.zero);
                vh.AddVert(a2 + n * solid, strokeColor, Vector2.zero);
                vh.AddVert(a2 - n * solid, strokeColor, Vector2.zero);
                vh.AddVert(a2 - n * edge, clear, Vector2.zero);
                vh.AddVert(b2 + n * edge, clear, Vector2.zero);
                vh.AddVert(b2 + n * solid, strokeColor, Vector2.zero);
                vh.AddVert(b2 - n * solid, strokeColor, Vector2.zero);
                vh.AddVert(b2 - n * edge, clear, Vector2.zero);
                for (int k = 0; k < 3; k++)
                {
                    vh.AddTriangle(s + k, s + k + 1, s + 4 + k + 1);
                    vh.AddTriangle(s + k, s + 4 + k + 1, s + 4 + k);
                }
            }
        }

        float CanvasUnitsPerPixel()
        {
            var c = canvas;
            float k = c != null ? c.scaleFactor : 1f;
            return k > 0f ? 1f / k : 1f;
        }

        // ---- the pointer ----------------------------------------------------------------

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return false;
            return Contains(local);
        }

        /// <summary>Whether a point in local space lies inside any piece.</summary>
        public bool Contains(Vector2 local)
        {
            foreach (var p in _pieces)
                if (InConvex(p, local)) return true;
            return false;
        }

        static bool InConvex(Vector2[] p, Vector2 q)
        {
            int sign = 0;
            for (int i = 0; i < p.Length; i++)
            {
                var a = p[i]; var b = p[(i + 1) % p.Length];
                float cross = (b.x - a.x) * (q.y - a.y) - (b.y - a.y) * (q.x - a.x);
                if (Mathf.Abs(cross) < 1e-5f) continue;
                int s = cross > 0 ? 1 : -1;
                if (sign == 0) sign = s;
                else if (s != sign) return false;
            }
            return true;
        }
    }
}
