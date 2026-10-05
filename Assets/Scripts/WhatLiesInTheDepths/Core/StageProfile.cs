// What Lies In The Depths — the shapes the stage comes in.
//
// The design is drawn at 1920 x 1080 and the scaler fits the whole of it on screen, which
// means the scale is min(w/1920, h/1080) and the design is never cropped. That is the right
// behaviour and it is also why a laptop reads this interface at two thirds size: in a browser
// it is almost always the HEIGHT that binds, because the chrome takes it. Even a full-HD
// laptop in a window is at 87%.
//
// Making the type bigger therefore means making the design smaller, and it has to come down
// on BOTH axes — the scale is a min, so shortening alone is paid for by width and narrowing
// alone is paid for by height. Shortening alone is worth +23% on a 1366 laptop and +2% on a
// 1728 one; doing both is worth about a quarter everywhere:
//
//     viewport        binds     full      compact
//     1366 x  625     height     58%        73%
//     1536 x  720     height     67%        84%
//     1728 x  950     width      90%       110%
//     1920 x  935     height     87%       109%
//
// Both short shapes are the same size and differ only in which side column stands in the row
// and which is folded into a drawer. The center keeps its 880 and a side column keeps its 450
// in every shape: a panel that is narrowed is a panel that has to be redrawn, and nothing here
// is redrawn — this is a different size of the same drawing, not a smaller window onto it.
using UnityEngine;
using Ursine.UI;

namespace WhatLiesInTheDepths.Core
{
    /// <summary>What the player asked for, which is not the same as what they get: Auto is a
    /// request to be sensible and resolves to one of the others every time it is read.
    ///
    /// The numbers are saved, so they may not be renumbered. Compact1 is 2 because it was once
    /// the only short shape and was simply called Compact.</summary>
    public enum StageMode { Auto = 0, Full = 1, Compact1 = 2, Compact2 = 3 }

    /// <summary>Which side column is not in the row. The ledger is a readout you glance at and
    /// the gauge is where you dive, so which one you would rather keep in front of you is a
    /// matter of how you play rather than a matter of fact — hence two short shapes.</summary>
    public enum Folded { None, Ledger, Gauge }

    public sealed class StageProfile
    {
        /// <summary>Full, Compact1 or Compact2. Never Auto: a profile is an answer.</summary>
        public readonly StageMode Mode;
        public readonly string Name;

        public readonly float StageW, StageH;

        public readonly Folded Fold;

        /// <summary>Where each column starts when it is standing in the row. The one that is
        /// folded has no place in the row, and its number here means nothing.</summary>
        public readonly float LedgerX, CenterX, GaugeX;

        /// <summary>Both derived, so the one vertical rule — a panel starts at 71 and the
        /// columns end 24 clear of the floor — holds in every shape without being restated.</summary>
        public readonly float ColumnH, TrackH;

        public Vector2 Size => new Vector2(StageW, StageH);

        public bool LedgerStands => Fold != Folded.Ledger;
        public bool GaugeStands => Fold != Folded.Gauge;
        public bool IsShort => Fold != Folded.None;

        /// <summary>The drawer comes in from the edge its column was nearest: the ledger from
        /// the left, the gauge from the right.</summary>
        public bool DrawerOnRight => Fold == Folded.Gauge;

        /// <summary>The standing side column carries the system bar, because Achievements,
        /// Options and Exit have to be reachable and a folded column is not. In Full and in
        /// Compact 1 that is already where they are; in Compact 2 the two bars change places,
        /// and the community bar rides into the drawer with the gauge.</summary>
        public bool SwapBars => Fold == Folded.Gauge;

        StageProfile(StageMode mode, string name, float w, float h, Folded fold,
                     float ledgerX, float centerX, float gaugeX)
        {
            Mode = mode;
            Name = name;
            StageW = w;
            StageH = h;
            Fold = fold;
            LedgerX = ledgerX;
            CenterX = centerX;
            GaugeX = gaugeX;
            ColumnH = h - Layout.ColumnY * 2f;
            TrackH = ColumnH - (Layout.PanelTopY - Layout.ColumnY);
        }

        /// <summary>30 · 450 · 40 · 880 · 40 · 450 · 30 = 1920, and 24 · 1032 · 24 = 1080.</summary>
        public static readonly StageProfile Full = new StageProfile(
            StageMode.Full, "Full", Layout.ScreenW, Layout.ScreenH, Folded.None,
            Layout.LeftX, Layout.CenterX, Layout.RightX);

        /// <summary>The gauge stands, the ledger is in the drawer. 30 · 880 · 40 · 450 · 30.</summary>
        public static readonly StageProfile Compact1 = new StageProfile(
            StageMode.Compact1, "Compact 1", ShortW, ShortH, Folded.Ledger,
            Layout.Margin, Layout.Margin,
            Layout.Margin + Layout.CenterColumnW + Layout.Gutter);

        /// <summary>The ledger stands, the gauge is in the drawer. 30 · 450 · 40 · 880 · 30 —
        /// the same width, read the other way round.</summary>
        public static readonly StageProfile Compact2 = new StageProfile(
            StageMode.Compact2, "Compact 2", ShortW, ShortH, Folded.Gauge,
            Layout.Margin, Layout.Margin + Layout.SideColumnW + Layout.Gutter,
            Layout.Margin + Layout.SideColumnW + Layout.Gutter);

        /// <summary>One side column and one gutter less than the full stage, and 220 shorter.</summary>
        const float ShortW = Layout.ScreenW - Layout.SideColumnW - Layout.Gutter;   // 1430
        const float ShortH = 860f;

        /// <summary>The shape being drawn right now. Anything that needs to know how tall its
        /// track is reads it from here rather than from the constant it was drawn against — the
        /// gauge already crops itself to the track every frame, so making this live is most of
        /// what the right column needs to change shape at all.</summary>
        public static StageProfile Current { get; private set; } = Full;

        /// <summary>Set by whatever is directing the stage, and by nothing else.</summary>
        public static void MarkCurrent(StageProfile p)
        {
            if (p != null) Current = p;
        }

        // ---- choosing ----------------------------------------------------------------

        /// <summary>Stay in Full while the full design still draws at least this big. Below it
        /// the type is small enough that losing a column is the better trade.</summary>
        public const float FullKeep = 0.85f;

        /// <summary>And only go back to Full once it draws this big. The gap between the two is
        /// the hysteresis: a window dragged slowly across the breakpoint crosses one number on
        /// the way down and a different one on the way back, so it cannot sit on the boundary
        /// rebuilding the screen every frame.</summary>
        public const float FullEnter = 0.92f;

        /// <summary>Which shape to be in. Auto asks the screen and answers Full or Compact 1;
        /// it never answers Compact 2, because which panel you would rather keep is a preference
        /// and not something a window size can tell us. The other three are told.</summary>
        public static StageProfile Choose(StageMode mode, StageProfile current)
        {
            if (mode == StageMode.Full) return Full;
            if (mode == StageMode.Compact1) return Compact1;
            if (mode == StageMode.Compact2) return Compact2;

            float fit = FixedStage.FitFor(Full.Size);
            bool wasFull = current == null || current.Mode == StageMode.Full;
            return fit >= (wasFull ? FullKeep : FullEnter) ? Full : Compact1;
        }

        public static StageProfile Of(StageMode mode)
        {
            switch (mode)
            {
                case StageMode.Compact1: return Compact1;
                case StageMode.Compact2: return Compact2;
                default: return Full;
            }
        }
    }
}
