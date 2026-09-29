// What Lies In The Depths — the road the Assault map is drawn on. Spec section 9.
//
// The road snakes: the Mind Palace at the top left, then five places to a row, left to right
// along the first row, down, right to left along the second, down, and so on — five rows of
// five for the twenty-five places. In the map's 880 x 430 space (y down).
//
// At the end of a row the road runs straight on past the last place before it turns, far
// enough that the turn clears the place's name hanging under it, then comes back in the
// same way to the first place of the next row.
using System.Collections.Generic;
using UnityEngine;
using Ursine.Geometry;

namespace WhatLiesInTheDepths.Core
{
    public static class Road
    {
        public static readonly Vector2 Space = new Vector2(880f, 430f);

        public const int PerRow = 5;
        static readonly float[] Cols = { 190f, 325f, 460f, 595f, 730f };
        static readonly Vector2 Home = new Vector2(76f, 52f);
        const float Top = 52f, Bottom = 364f, RowStepMax = 78f;
        /// <summary>How far the road runs on past a row's last place before it turns: half a
        /// place's name (the names are 128 wide) and a little air.</summary>
        const float RunOn = 70f;

        static CubicPath _path;
        static int _count = 25;
        static readonly List<float> _at = new List<float>();   // distance along at each place

        /// <summary>Lays the road out for <paramref name="count"/> places. Cheap if unchanged.</summary>
        public static void Configure(int count)
        {
            count = Mathf.Max(1, count);
            if (_path != null && count == _count) return;
            _count = count;
            _path = null;
        }

        public static CubicPath Path { get { if (_path == null) Build(); return _path; } }

        static float RowStep
        {
            get
            {
                int rows = (_count + PerRow - 1) / PerRow;
                return rows <= 1 ? 0f : Mathf.Min(RowStepMax, (Bottom - Top) / (rows - 1));
            }
        }

        /// <summary>Where the place at <paramref name="index"/> sits, y down.</summary>
        public static Vector2 PlacePoint(int index)
        {
            int row = index / PerRow, col = index % PerRow;
            float x = row % 2 == 0 ? Cols[col] : Cols[PerRow - 1 - col];
            return new Vector2(x, Top + row * RowStep);
        }

        static void Build()
        {
            var p = new CubicPath(Home);
            _at.Clear();
            var prev = Home;
            for (int i = 0; i < _count; i++)
            {
                var next = PlacePoint(i);
                if (i > 0 && i % PerRow == 0)
                {
                    // Round the end of the row: straight on, a half-turn down, straight back in.
                    float dir = prev.x > Space.x * 0.5f ? 1f : -1f;
                    float ex = prev.x + dir * RunOn;
                    float r = (next.y - prev.y) * 0.5f;
                    p.Line(new Vector2(ex, prev.y));
                    p.Curve(new Vector2(ex + dir * r * 1.3333f, prev.y), new Vector2(ex + dir * r * 1.3333f, next.y),
                            new Vector2(ex, next.y), 32);
                }
                p.Line(next);
                _at.Add(p.Length);
                prev = next;
            }
            _path = p;
        }

        /// <summary>How far along the road the place at <paramref name="index"/> is, 0-1.
        /// -1 is the Mind Palace.</summary>
        public static float Fraction(int index)
        {
            var path = Path;
            if (index < 0 || _at.Count == 0 || path.Length <= 0f) return 0f;
            return _at[Mathf.Min(index, _at.Count - 1)] / path.Length;
        }

        /// <summary>A point on the road as an anchored position in a map whose anchor is its
        /// top-left corner (y up, so negative downward).</summary>
        public static Vector2 Anchored(float fraction)
        {
            var q = Path.PointAt(fraction);
            return new Vector2(q.x, -q.y);
        }

        /// <summary>The place at <paramref name="index"/>, as an anchored position.</summary>
        public static Vector2 AnchoredPlace(int index)
        {
            var q = PlacePoint(index);
            return new Vector2(q.x, -q.y);
        }
    }
}
