// What Lies In The Depths — the road the Assault map is drawn on. Spec section 9.
// The spec's own path, in its 880 x 430 map space (y down). Places sit on it by fraction
// of its length, as the spec's getPointAtLength(len * t) does.
using UnityEngine;
using Ursine.Geometry;

namespace WhatLiesInTheDepths.Core
{
    public static class Road
    {
        public static readonly Vector2 Space = new Vector2(880f, 430f);

        static CubicPath _path;

        /// <summary>M 54 366 C 118 344, 146 306, 196 302 S 272 348, 320 332 S 400 250, 448 254
        /// S 524 300, 570 278 S 646 194, 694 180 S 800 132, 866 86</summary>
        public static CubicPath Path => _path ??= new CubicPath(new Vector2(54, 366))
            .Curve(new Vector2(118, 344), new Vector2(146, 306), new Vector2(196, 302))
            .Smooth(new Vector2(272, 348), new Vector2(320, 332))
            .Smooth(new Vector2(400, 250), new Vector2(448, 254))
            .Smooth(new Vector2(524, 300), new Vector2(570, 278))
            .Smooth(new Vector2(646, 194), new Vector2(694, 180))
            .Smooth(new Vector2(800, 132), new Vector2(866, 86));

        /// <summary>A point on the road, as an anchored position in a map whose anchor is its
        /// top-left corner (y up, so negative downward).</summary>
        public static Vector2 Anchored(float fraction)
        {
            var p = Path.PointAt(fraction);
            return new Vector2(p.x, -p.y);
        }
    }
}
