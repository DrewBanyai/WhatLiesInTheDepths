// What Lies In The Depths — the layout budget. Spec section 3 of the brief and section 1 of the
// authority. These are the only numbers in the project that may be treated as fixed:
// every other figure in the specs is a placeholder.
namespace WhatLiesInTheDepths.Core
{
    public static class Layout
    {
        public const float ScreenW = 1920f;
        public const float ScreenH = 1080f;

        // Horizontal: 30 margin · 450 left · 40 gutter · 880 center · 40 gutter · 450 right · 30 margin
        public const float Margin = 30f;
        public const float SideColumnW = 450f;
        public const float Gutter = 40f;
        public const float CenterColumnW = 880f;

        public const float LeftX = 30f;
        public const float CenterX = 520f;
        public const float RightX = 1440f;

        // Vertical: 24 top, 24 bottom, 1032 track.
        public const float ColumnY = 24f;
        public const float ColumnH = 1032f;

        // The bar takes 31 and clears 16, so every panel starts at y = 71 with 985 available.
        public const float BarH = 31f;
        public const float BarLabelH = 19f;
        public const float BarClearance = 11f;
        public const float BarRule = 1f;
        public const float GapBarToContent = 16f;
        public const float PanelTopY = 71f;
        public const float PanelTrackH = 985f;

        /// <summary>The one strong line — all three bar rules on y = 55, broken only by the
        /// two gutters. Nothing else in the composition may be a horizontal this strong.</summary>
        public const float RuleY = 55f;

        // Radii. 14 is the default panel, 9 the default control, 999 the pill.
        public const float RadiusPill = 999f;
        public const float RadiusControl = 9f;
        public const float RadiusPanel = 14f;

        // Motion, in seconds. Spec section 4 of the brief.
        public const float MotionControl = 0.155f;
        public const float MotionRevealIn = 0.16f;
        public const float MotionRevealOut = 0.13f;
        public const float MotionFill = 0.22f;
        public const float MotionColumnsOut = 0.5f;
        public const float MotionEndingIn = 0.6f;

        // Clocks.
        public const float EconomyTick = 1f;       // everything in this game happens on the second
        public const float AutosaveInterval = 120f;
        public const float BattleMs = 10000f;
        public const float BattleResultHoldMs = 4500f;

        /// <summary>The foot of the left column is deliberately reserved and deliberately
        /// empty — roughly 480px under the ledger. Nothing may drift into it.</summary>
        public const float LeftColumnReservedFoot = 480f;

        /// <summary>The one thing placed in that foot, and the reason it is allowed there: it
        /// is not about the dream. 300 of the column's 450, sitting on the column's own floor
        /// so it is 24px clear of the bottom of the screen, and drawn on the mist.</summary>
        public const float NowPlayingW = 300f;
        public const float NowPlayingH = 34f;

        /// <summary>Seconds one track takes to dissolve into the next, and the span the first
        /// track of a session fades up over.</summary>
        public const float MusicCrossfade = 6f;

        /// <summary>Where the three sound bars start. They live here rather than in the
        /// Options builder because the music has to be at the right level before anyone has
        /// opened Options — the page takes over the moment it is first shown.</summary>
        public const float VolumeMaster = 0.8f;
        public const float VolumeMusic = 0.6f;
        public const float VolumeEffects = 0.7f;
    }
}
