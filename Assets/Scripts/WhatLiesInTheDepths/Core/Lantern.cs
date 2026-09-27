// What Lies In The Depths — the lantern's shape. Revelations spec, section 1.
//
// Everything here is in lantern units: y down, the origin at the top of the hanging ring, the
// lantern 374 tall. The glass is a diamond lattice clipped to the body, with the lattice's
// size searched so the body holds exactly one pane per Revelation that can be realized: a
// change to the content changes the leading, never the rule that each realization lights one
// pane. A sliver of lattice too small to be a pane is joined to its biggest neighbour.
using System.Collections.Generic;
using UnityEngine;

namespace WhatLiesInTheDepths.Core
{
    public static class Lantern
    {
        public const float Height = 374f;
        public const float Width = 172f;

        /// <summary>The glass: an elongated octagon, widest across the middle.</summary>
        public static readonly Vector2[] Body =
        {
            new Vector2(-48, 100), new Vector2(48, 100), new Vector2(86, 166), new Vector2(86, 272),
            new Vector2(48, 338), new Vector2(-48, 338), new Vector2(-86, 272), new Vector2(-86, 166),
        };

        /// <summary>Where the flame burns. Panes light outward from here.</summary>
        public static readonly Vector2 Flame = new Vector2(0, 226);

        // The frame, drawn in the frame's own colors over and around the glass.
        public static readonly Vector2 RingCenter = new Vector2(0, 17);
        public const float RingRadius = 15f;
        public static readonly Vector2 FinialCenter = new Vector2(0, 40);
        public const float FinialRadius = 8f;
        public static readonly Vector2[] Roof =
            { new Vector2(-18, 48), new Vector2(18, 48), new Vector2(60, 88), new Vector2(-60, 88) };
        public static readonly Vector2[] Collar =
            { new Vector2(-64, 86), new Vector2(64, 86), new Vector2(64, 102), new Vector2(-64, 102) };
        public static readonly Vector2[] BaseCollar =
            { new Vector2(-64, 336), new Vector2(64, 336), new Vector2(64, 352), new Vector2(-64, 352) };
        public static readonly Vector2[] Foot =
            { new Vector2(-54, 352), new Vector2(54, 352), new Vector2(36, 374), new Vector2(-36, 374) };

        /// <summary>One pane: one or more convex pieces (a pane that took in a sliver has two).</summary>
        public sealed class Pane
        {
            public readonly List<Vector2[]> pieces = new List<Vector2[]>();
            public Vector2 centroid;
        }

        static readonly Dictionary<int, List<Pane>> Cache = new Dictionary<int, List<Pane>>();

        /// <summary>Exactly <paramref name="count"/> panes, ordered from the flame outward — the
        /// order they light in. Cached per count.</summary>
        public static List<Pane> Panes(int count)
        {
            if (count <= 0) return new List<Pane>();
            if (Cache.TryGetValue(count, out var hit)) return hit;

            // Find the lattice. Fewest slivers first, then the fattest smallest pane.
            float bestS = 0f, bestK = 0f, bestO = 0f;
            int bestSmall = int.MaxValue; float bestMin = 0f; int bestMiss = int.MaxValue;
            float[] ks = { 0.62f, 0.66f, 0.70f, 0.74f, 0.78f };
            float[] offs = { 0f, 0.25f, 0.5f };
            for (int si = 0; si < 250; si++)
            {
                float s = 18f + si * 0.16f;
                foreach (float k in ks)
                    foreach (float o in offs)
                    {
                        float full = s * s / (2f * k);
                        // A lattice whose panes are far too big or too small cannot be the one.
                        float estimate = BodyArea / full;
                        if (Mathf.Abs(estimate - count) > 0.2f * count + 4f) continue;
                        int big = 0, small = 0; float min = float.MaxValue;
                        CellAreas(s, k, s * o, _areas);
                        foreach (float a in _areas)
                        {
                            if (a >= 0.3f * full) { big++; min = Mathf.Min(min, a / full); }
                            else small++;
                        }
                        int miss = Mathf.Abs(big - count);
                        bool better = miss < bestMiss
                                   || (miss == bestMiss && (small < bestSmall || (small == bestSmall && min > bestMin)));
                        if (better) { bestMiss = miss; bestSmall = small; bestMin = min; bestS = s; bestK = k; bestO = s * o; }
                    }
            }

            var all = Cells(bestS, bestK, bestO);
            float fullArea = bestS * bestS / (2f * bestK);
            var panes = new Dictionary<Vector2Int, Pane>();
            foreach (var kv in all)
                if (Area(kv.Value) >= 0.3f * fullArea)
                {
                    var p = new Pane(); p.pieces.Add(kv.Value); panes[kv.Key] = p;
                }
            foreach (var kv in all)
            {
                if (panes.ContainsKey(kv.Key)) continue;
                // A sliver joins the biggest pane it shares an edge with, or the nearest.
                Pane target = null; float best = -1f;
                foreach (var n in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    if (panes.TryGetValue(kv.Key + n, out var p) && Area(p.pieces[0]) > best)
                    { best = Area(p.pieces[0]); target = p; }
                if (target == null)
                {
                    var c = Centroid(kv.Value); float d = float.MaxValue;
                    foreach (var p in panes.Values)
                    {
                        float dd = (Centroid(p.pieces[0]) - c).sqrMagnitude;
                        if (dd < d) { d = dd; target = p; }
                    }
                }
                target?.pieces.Add(kv.Value);
            }

            var list = new List<Pane>(panes.Values);
            foreach (var p in list) p.centroid = Centroid(p.pieces);
            list.Sort((a, b) => Reach(a.centroid).CompareTo(Reach(b.centroid)));
            // If the body cannot hold exactly that many (it always has so far), the extra panes
            // are dropped from the far edge, or the missing ones simply never light.
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            Cache[count] = list;
            return list;
        }

        /// <summary>How far a point is from the flame, the way the light reaches: a little
        /// further up and down than sideways.</summary>
        static float Reach(Vector2 p) => new Vector2(p.x - Flame.x, (p.y - Flame.y) / 0.85f).magnitude;

        static readonly float BodyArea = Area(Body);
        static readonly List<float> _areas = new List<float>();

        /// <summary>The areas of every lattice cell inside the body, without keeping the cells:
        /// the search asks this thousands of times.</summary>
        static void CellAreas(float s, float k, float ox, List<float> into)
        {
            into.Clear();
            float reach = 86f + k * 126f + Mathf.Abs(ox);
            int lo = Mathf.FloorToInt(-reach / s) - 1, hi = Mathf.CeilToInt(reach / s) + 1;
            for (int i = lo; i < hi; i++)
                for (int j = lo; j < hi; j++)
                {
                    _d[0] = XY(i * s, j * s, k, ox);
                    _d[1] = XY((i + 1) * s, j * s, k, ox);
                    _d[2] = XY((i + 1) * s, (j + 1) * s, k, ox);
                    _d[3] = XY(i * s, (j + 1) * s, k, ox);
                    Vector2[] result;
                    int n = ClipInto(_d, 4, Body, _a, _b, out result);
                    if (n < 3) continue;
                    float a = 0f;
                    for (int m = 0; m < n; m++)
                    {
                        var u = result[m]; var v = result[(m + 1) % n];
                        a += u.x * v.y - v.x * u.y;
                    }
                    a = Mathf.Abs(a) * 0.5f;
                    if (a > 0.01f) into.Add(a);
                }
        }

        static readonly Vector2[] _d = new Vector2[4], _a = new Vector2[16], _b = new Vector2[16];

        /// <summary>Sutherland–Hodgman into fixed buffers. Returns the vertex count; the result
        /// is whichever buffer the last pass wrote.</summary>
        static int ClipInto(Vector2[] poly, int count, Vector2[] clipper, Vector2[] bufA, Vector2[] bufB, out Vector2[] result)
        {
            var src = bufA; var dst = bufB;
            for (int i = 0; i < count; i++) src[i] = poly[i];
            int n = count;
            for (int c = 0; c < clipper.Length && n > 0; c++)
            {
                var a = clipper[c]; var b = clipper[(c + 1) % clipper.Length];
                int m = 0;
                for (int j = 0; j < n; j++)
                {
                    var p = src[j]; var q = src[(j + 1) % n];
                    bool pin = Inside(a, b, p), qin = Inside(a, b, q);
                    if (qin)
                    {
                        if (!pin) dst[m++] = Cross(p, q, a, b);
                        dst[m++] = q;
                    }
                    else if (pin) dst[m++] = Cross(p, q, a, b);
                }
                var t = src; src = dst; dst = t; n = m;
            }
            result = src;
            return n;
        }

        static Dictionary<Vector2Int, Vector2[]> Cells(float s, float k, float ox)
        {
            var cells = new Dictionary<Vector2Int, Vector2[]>();
            // Only the cells that can reach the body: |u|, |v| <= half its width + k * its reach
            // above or below the flame.
            float reach = 86f + k * 126f + Mathf.Abs(ox);
            int lo = Mathf.FloorToInt(-reach / s) - 1, hi = Mathf.CeilToInt(reach / s) + 1;
            var diamond = new Vector2[4];
            for (int i = lo; i < hi; i++)
                for (int j = lo; j < hi; j++)
                {
                    diamond[0] = XY(i * s, j * s, k, ox);
                    diamond[1] = XY((i + 1) * s, j * s, k, ox);
                    diamond[2] = XY((i + 1) * s, (j + 1) * s, k, ox);
                    diamond[3] = XY(i * s, (j + 1) * s, k, ox);
                    var c = Clip(diamond, Body);
                    if (c.Length >= 3 && Area(c) > 0.01f) cells[new Vector2Int(i, j)] = c;
                }
            return cells;
        }

        // The lattice's two families of lines: u = x + k·y and v = x − k·y, measured from a line
        // through the flame so the pane at the flame is centered on it.
        static Vector2 XY(float u, float v, float k, float ox)
            => new Vector2((u + v) * 0.5f + ox, (u - v) / (2f * k) + Flame.y);

        /// <summary>Sutherland–Hodgman: a convex polygon clipped by a convex clipper.</summary>
        static Vector2[] Clip(Vector2[] poly, Vector2[] clipper)
        {
            var output = new List<Vector2>(poly);
            for (int i = 0; i < clipper.Length && output.Count > 0; i++)
            {
                var a = clipper[i]; var b = clipper[(i + 1) % clipper.Length];
                var input = output; output = new List<Vector2>();
                for (int j = 0; j < input.Count; j++)
                {
                    var p = input[j]; var q = input[(j + 1) % input.Count];
                    bool pin = Inside(a, b, p), qin = Inside(a, b, q);
                    if (qin)
                    {
                        if (!pin) output.Add(Cross(p, q, a, b));
                        output.Add(q);
                    }
                    else if (pin) output.Add(Cross(p, q, a, b));
                }
            }
            return output.ToArray();
        }

        static bool Inside(Vector2 a, Vector2 b, Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) >= 0f;

        static Vector2 Cross(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float den = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x);
            float t = ((p1.x - p3.x) * (p3.y - p4.y) - (p1.y - p3.y) * (p3.x - p4.x)) / den;
            return p1 + t * (p2 - p1);
        }

        public static float Area(Vector2[] p)
        {
            float a = 0f;
            for (int i = 0; i < p.Length; i++)
            {
                var u = p[i]; var v = p[(i + 1) % p.Length];
                a += u.x * v.y - v.x * u.y;
            }
            return Mathf.Abs(a) * 0.5f;
        }

        static Vector2 Centroid(Vector2[] p)
        {
            var c = Vector2.zero;
            foreach (var v in p) c += v;
            return c / p.Length;
        }

        static Vector2 Centroid(List<Vector2[]> pieces)
        {
            float total = 0f; var c = Vector2.zero;
            foreach (var p in pieces) { float a = Area(p); c += Centroid(p) * a; total += a; }
            return total > 0f ? c / total : Vector2.zero;
        }
    }
}
