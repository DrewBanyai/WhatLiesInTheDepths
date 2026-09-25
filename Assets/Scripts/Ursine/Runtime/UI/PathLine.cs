// Ursine — a stroked path for uGUI: solid or dashed, round-capped, drawn only as far along
// as you ask, with an optional fade across the path's box. Enough for a road on a map.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ursine.Geometry;

namespace Ursine.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PathLine : MaskableGraphic
    {
        [Tooltip("Stroke width in canvas units.")]
        public float thickness = 3.4f;
        [Tooltip("Anti-aliasing: the width, in screen pixels, over which each edge fades out.")]
        public float feather = 1.25f;
        [Tooltip("Dash and gap lengths; a dash of 0 draws the stroke solid.")]
        public float dash, gap;
        [Range(0f, 1f)] public float from = 0f;
        [Range(0f, 1f)] public float to = 1f;

        [Header("Fade — alpha along a direction across the path's box (SVG gradient units)")]
        public bool fade;
        public Vector2 fadeStart = new Vector2(0f, 1f);   // bounding-box units, y-down
        public Vector2 fadeEnd = new Vector2(1f, 0f);
        [Range(0f, 1f)] public float fadeHold = 0.72f;    // full until here, gone at 1

        CubicPath _path;
        Vector2 _size;                                    // the space the path is in, y-down
        Vector2 _min, _max;                               // the path's box
        readonly List<Vector2> _run = new List<Vector2>();

        /// <summary>Draws <paramref name="path"/>, whose coordinates are y-down in a box of
        /// <paramref name="space"/>, stretched over this graphic's rect.</summary>
        public void SetPath(CubicPath path, Vector2 space)
        {
            _path = path;
            _size = space;
            _min = new Vector2(float.MaxValue, float.MaxValue);
            _max = new Vector2(float.MinValue, float.MinValue);
            foreach (var p in path.Points) { _min = Vector2.Min(_min, p); _max = Vector2.Max(_max, p); }
            SetVerticesDirty();
        }

        public void SetRange(float fromFraction, float toFraction)
        {
            from = fromFraction; to = toFraction;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_path == null || _path.Length <= 0f || _size.x <= 0f || _size.y <= 0f) return;

            float a = from * _path.Length, b = to * _path.Length;
            if (b <= a) return;

            if (dash <= 0f) Stroke(vh, a, b);
            else
                for (float s = a; s < b; s += dash + gap)
                    Stroke(vh, s, Mathf.Min(b, s + dash));
        }

        // One strip with shared vertices, so a translucent stroke never doubles up at its
        // joints. Each side carries a feather: the stroke is solid to within half a feather
        // of its edge and fades to nothing half a feather beyond it, which is the whole of
        // its anti-aliasing — uGUI has no MSAA on a Screen Space - Overlay canvas.
        void Stroke(VertexHelper vh, float a, float b)
        {
            _path.Between(a, b, _run);
            if (_run.Count < 2) return;
            float f = Mathf.Max(0f, feather) * CanvasUnitsPerPixel();
            float r = thickness * 0.5f;
            float inner = Mathf.Max(0f, r - f * 0.5f), outer = r + f * 0.5f;
            int n = _run.Count;
            int start = vh.currentVertCount;
            Vector2 firstDir = Vector2.right, lastDir = Vector2.right;
            for (int i = 0; i < n; i++)
            {
                var p = Local(_run[i]);
                var prev = Local(_run[Mathf.Max(0, i - 1)]);
                var next = Local(_run[Mathf.Min(n - 1, i + 1)]);
                var dir = next - prev;
                if (dir.sqrMagnitude < 1e-8f) dir = Vector2.right;
                dir.Normalize();
                if (i == 0) firstDir = dir;
                if (i == n - 1) lastDir = dir;
                var nrm = new Vector2(-dir.y, dir.x);
                var c = Tint(_run[i]);
                var clear = c; clear.a = 0;
                vh.AddVert(p + nrm * outer, clear, Vector2.zero);   // 0
                vh.AddVert(p + nrm * inner, c, Vector2.zero);       // 1
                vh.AddVert(p - nrm * inner, c, Vector2.zero);       // 2
                vh.AddVert(p - nrm * outer, clear, Vector2.zero);   // 3
            }
            for (int i = 0; i < n - 1; i++)
            {
                int k = start + i * 4, m = k + 4;
                for (int e = 0; e < 3; e++)
                {
                    vh.AddTriangle(k + e, m + e, m + e + 1);
                    vh.AddTriangle(m + e + 1, k + e + 1, k + e);
                }
            }
            Cap(vh, Local(_run[0]), -firstDir, inner, outer, Tint(_run[0]));
            Cap(vh, Local(_run[n - 1]), lastDir, inner, outer, Tint(_run[n - 1]));
        }

        // How many canvas units one screen pixel is, so the feather is a pixel whatever the
        // canvas scale.
        float CanvasUnitsPerPixel()
        {
            var c = canvas;
            float k = c != null ? c.scaleFactor : 1f;
            return k > 0f ? 1f / k : 1f;
        }

        // A y-down point in the path's space, onto this rect.
        Vector2 Local(Vector2 p)
        {
            var rect = rectTransform.rect;
            return new Vector2(rect.xMin + p.x / _size.x * rect.width,
                               rect.yMax - p.y / _size.y * rect.height);
        }

        Color32 Tint(Vector2 p)
        {
            Color c = color;
            if (!fade) return c;
            var box = _max - _min;
            var u = new Vector2(box.x > 0 ? (p.x - _min.x) / box.x : 0f, box.y > 0 ? (p.y - _min.y) / box.y : 0f);
            var dir = fadeEnd - fadeStart;
            float t = Vector2.Dot(u - fadeStart, dir) / Mathf.Max(1e-6f, dir.sqrMagnitude);
            c.a *= t <= fadeHold ? 1f : Mathf.Clamp01(1f - (t - fadeHold) / (1f - fadeHold));
            return c;
        }

        // A half-disc on the side the stroke is heading out of, with the same feather.
        static void Cap(VertexHelper vh, Vector2 center, Vector2 outward, float inner, float outer, Color32 c)
        {
            const int n = 12;
            var clear = c; clear.a = 0;
            float a0 = Mathf.Atan2(outward.y, outward.x) - Mathf.PI * 0.5f;
            int i0 = vh.currentVertCount;
            vh.AddVert(center, c, Vector2.zero);
            for (int k = 0; k <= n; k++)
            {
                float t = a0 + k / (float)n * Mathf.PI;
                var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                vh.AddVert(center + d * inner, c, Vector2.zero);
                vh.AddVert(center + d * outer, clear, Vector2.zero);
            }
            for (int k = 0; k < n; k++)
            {
                int a = i0 + 1 + k * 2, b = a + 2;
                vh.AddTriangle(i0, a, b);
                vh.AddTriangle(a, a + 1, b + 1);
                vh.AddTriangle(b + 1, b, a);
            }
        }
    }
}
