// Ursine — a chain of cubic Béziers, sampled once and then read by arc length.
//
// An SVG path such as "M a C b, c, d S e, f ..." is enough to lay a road, a river or a
// progress trail across a map; what a game needs from it is a point at a fraction of the
// way along, which the browser gives as getPointAtLength. This is that, precomputed.
using System.Collections.Generic;
using UnityEngine;

namespace Ursine.Geometry
{
    public sealed class CubicPath
    {
        readonly List<Vector2> _points = new List<Vector2>();
        readonly List<float> _lengths = new List<float>();     // cumulative, same count as points

        public IReadOnlyList<Vector2> Points => _points;
        public float Length => _lengths.Count == 0 ? 0f : _lengths[_lengths.Count - 1];

        /// <summary>Starts at <paramref name="start"/>. Coordinates are whatever space the
        /// caller draws in; an SVG's are y-down.</summary>
        public CubicPath(Vector2 start) { _start = start; _last = start; _lastControl = start; Add(start); }

        readonly Vector2 _start;
        Vector2 _last, _lastControl;

        /// <summary>SVG "C c1, c2, p".</summary>
        public CubicPath Curve(Vector2 c1, Vector2 c2, Vector2 p, int samples = 48)
        {
            var p0 = _last;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples, u = 1f - t;
                Add(u * u * u * p0 + 3f * u * u * t * c1 + 3f * u * t * t * c2 + t * t * t * p);
            }
            _last = p;
            _lastControl = c2;
            return this;
        }

        /// <summary>SVG "S c2, p": the first control is the previous one reflected.</summary>
        public CubicPath Smooth(Vector2 c2, Vector2 p, int samples = 48)
            => Curve(2f * _last - _lastControl, c2, p, samples);

        void Add(Vector2 p)
        {
            float d = _points.Count == 0 ? 0f : _lengths[_lengths.Count - 1] + Vector2.Distance(_points[_points.Count - 1], p);
            _points.Add(p);
            _lengths.Add(d);
        }

        /// <summary>The point <paramref name="distance"/> along the path.</summary>
        public Vector2 PointAtLength(float distance)
        {
            if (_points.Count == 0) return _start;
            distance = Mathf.Clamp(distance, 0f, Length);
            int lo = 0, hi = _lengths.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (_lengths[mid] < distance) lo = mid; else hi = mid;
            }
            float seg = _lengths[hi] - _lengths[lo];
            float k = seg <= 0f ? 0f : (distance - _lengths[lo]) / seg;
            return Vector2.Lerp(_points[lo], _points[hi], k);
        }

        /// <summary>The point a fraction 0-1 of the way along.</summary>
        public Vector2 PointAt(float fraction) => PointAtLength(fraction * Length);

        /// <summary>The samples between two distances, ends included — for drawing a stretch.</summary>
        public void Between(float from, float to, List<Vector2> into)
        {
            into.Clear();
            if (to <= from) return;
            into.Add(PointAtLength(from));
            for (int i = 0; i < _points.Count; i++)
                if (_lengths[i] > from && _lengths[i] < to) into.Add(_points[i]);
            into.Add(PointAtLength(to));
        }
    }
}
