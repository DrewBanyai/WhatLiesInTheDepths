// What Lies In The Depths — the composite.
// "Whichever target: build the composite, not the components. Six of the twelve files were
// caught in error only because the panel was put in the real column at the real size next
// to the real ledger. A component that looks right alone is not evidence." — brief §8.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using WhatLiesInTheDepths.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static WhatLiesInTheDepths.EditorTools.UiFactory;
using Ursine;
using Ursine.Audio;
using Ursine.UI;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class BuildScreen
    {
        public static void Build()
        {
            var root = new GameObject("UI_Screen",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(FixedStage));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Canvas Scaler: Scale With Screen Size, reference 1920 x 1080, expanding to fit.
            // Every number in the specs is then a Unity UI unit, 1:1, and a screen that is not
            // 16:9 — a phone in landscape, a Free Aspect game view — gains margin rather than
            // losing the top and bottom of the design. FixedStage holds that against accident.
            var stageGuard = root.GetComponent<FixedStage>();
            stageGuard.referenceResolution = new Vector2(Layout.ScreenW, Layout.ScreenH);
            stageGuard.blendAxes = false;
            stageGuard.matchWidthOrHeight = 0.5f;
            stageGuard.Apply();

            var canvasRt = (RectTransform)root.transform;

            // Ground: mist with a soft veil bloom from the upper left. No panel behind the
            // columns and no divider between them.
            var ground = Stretch(Node("Ground", canvasRt));
            Img(ground, null, Tok.Mist);
            Img(Stretch(Node("Bloom", ground)), SpriteFactory.Load("Bloom_Page"), Tok.Veil, 0.85f);

            // The stage. Extra width goes to the outer margins and the columns stay put,
            // which is what anchoring the stage centrally and letting the ground stretch does.
            var stage = Node("Stage", canvasRt, 0, 0, Layout.ScreenW, Layout.ScreenH);
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.anchoredPosition = Vector2.zero;

            var columns = Stretch(Node("Columns", stage));
            var columnsGroup = columns.gameObject.AddComponent<CanvasGroup>();

            // Three columns, absolutely positioned, top-aligned, never stretched. All three:
            // y = 24, height 1032. Do not use layout groups for these.
            var left = Node("LeftColumn", columns, Layout.LeftX, Layout.ColumnY,
                            Layout.SideColumnW, Layout.ColumnH);
            var center = Node("CenterColumn", columns, Layout.CenterX, Layout.ColumnY,
                              Layout.CenterColumnW, Layout.ColumnH);
            var right = Node("RightColumn", columns, Layout.RightX, Layout.ColumnY,
                             Layout.SideColumnW, Layout.ColumnH);

            // The bars all sit on the same baseline, so their rules read as one line across
            // the whole screen at y = 55, broken only by the two gutters.
            var leftBar = Nest("UI_Bar_Left", left, 0, 0).GetComponent<BarView>();
            var centerBar = Nest("UI_Bar_Center", center, 0, 0).GetComponent<BarView>();
            var rightBar = Nest("UI_Bar_Right", right, 0, 0).GetComponent<BarView>();

            // Every panel starts at y = 71 with 985 available.
            Nest("UI_ResourceLedger", left, 0, Layout.PanelTopY - Layout.ColumnY);
            Nest("UI_DepthGauge", right, 0, Layout.PanelTopY - Layout.ColumnY);

            // On the column's own floor, so it is 24px clear of the bottom of the screen and
            // sits on the same baseline the columns end on rather than one of its own. Hung off
            // that floor rather than measured down from the head, because the column is not the
            // same height in both stages and a line placed at 998 would be below a column of 812.
            var nowPlaying = Nest("UI_NowPlaying", left, 0, 0).transform as RectTransform;
            nowPlaying.anchorMin = new Vector2(0f, 0f);
            nowPlaying.anchorMax = new Vector2(0f, 0f);
            nowPlaying.pivot = new Vector2(0f, 0f);
            nowPlaying.anchoredPosition = Vector2.zero;
            nowPlaying.sizeDelta = new Vector2(Layout.NowPlayingW, Layout.NowPlayingH);

            var centerSurfaces = Node("Surfaces", center, 0, Layout.PanelTopY - Layout.ColumnY,
                                      Layout.CenterColumnW, Layout.PanelTrackH);

            // The drawer sits on the stage ABOVE the columns, because when it is open it is
            // over them. It is built once and switched off in the shape where both side columns
            // stand in the row; which column it carries, and which edge it comes in from, is
            // settled at runtime.
            var drawer = Drawer(stage);

            var screen = root.AddComponent<ScreenRoot>();
            screen.stage = stage;
            screen.surfaces = centerSurfaces;
            screen.drawer = drawer;
            screen.leftColumn = left;
            screen.centerColumn = center;
            screen.rightColumn = right;
            screen.columnsGroup = columnsGroup;
            screen.leftBar = leftBar;
            screen.centerBar = centerBar;
            screen.rightBar = rightBar;

            // In Destination order: Journal, Focus, Constructs, Revelations, Visions, Assault.
            string[] destinations =
            {
                "UI_Journal", "UI_Focus", "UI_Constructs", "UI_Revelations", "UI_Visions", "UI_Assault"
            };
            foreach (var d in destinations)
            {
                var go = Nest(d, centerSurfaces, 0, 0);
                screen.destinations.Add(go);
                go.SetActive(false);
            }

            screen.achievementsPage = Nest("UI_Achievements", centerSurfaces, 0, 0);
            screen.achievementsPage.SetActive(false);
            screen.optionsPage = Nest("UI_Options", centerSurfaces, 0, 0);
            screen.exitQuestion = Nest("UI_ExitQuestion", centerSurfaces, 0, 0);
            screen.hardResetQuestion = Nest("UI_HardResetQuestion", centerSurfaces, 0, 0);
            screen.optionsPage.SetActive(false);
            screen.exitQuestion.SetActive(false);
            screen.hardResetQuestion.SetActive(false);

            // The ending is not an overlay: it comes up on the ground the columns were
            // sitting on, and they fade to zero beneath it.
            var ending = Nest("UI_Ending", stage, 0, 0);
            var endingGroup = ending.GetComponent<CanvasGroup>();
            if (endingGroup == null) endingGroup = ending.AddComponent<CanvasGroup>();
            endingGroup.alpha = 0f;
            ending.SetActive(false);
            screen.ending = ending;
            screen.endingGroup = endingGroup;

            // The services. One purse, one tick, one save clock, one router. The two
            // clocks are Ursine's, so their intervals come from this game's budget rather
            // than from a constant inside them.
            // Each one is switched on explicitly. A disabled clock still runs its Awake, so
            // GameClock.I and GameState.I are set and every readout looks right, while nothing
            // ticks and nothing drifts — a failure that says nothing in the console.
            // Which shape the stage is in. Added before the clocks only so that it reads
            // in the inspector in the order things happen: it is told the window, and it tells
            // the stage and the screen.
            root.AddComponent<StageDirector>().enabled = true;

            var clock = root.AddComponent<GameClock>();
            clock.tickSeconds = Layout.EconomyTick;
            clock.enabled = true;

            var save = root.AddComponent<SaveClock>();
            save.intervalSeconds = Layout.AutosaveInterval;
            save.enabled = true;
            root.AddComponent<GameState>().enabled = true;
            var router = root.AddComponent<Router>();
            router.enabled = true;

            Music(root);

            WireBars(leftBar, centerBar, rightBar);

            Save(root, "UI_Screen");
        }

        /// <summary>Where the left column goes when the row has no room for it. A scrim over
        /// the whole stage and a sheet that slides in from the left carrying the column itself —
        /// the ledger, the community bar and the now-playing line, exactly as they are drawn in
        /// the row, because it is the same column and not a second one.</summary>
        static ColumnDrawer Drawer(RectTransform stage)
        {
            var host = Node("ColumnDrawer", stage, 0, 0, Layout.ScreenW, Layout.ScreenH);
            var group = host.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // The dark over the rest of the screen. It is the hit area that shuts the drawer,
            // which is why it takes the pointer and the sheet's ground does not.
            var scrimRt = Stretch(Node("Scrim", host));
            Img(scrimRt, null, Tok.Ink, 0.28f, true);
            var scrim = scrimRt.gameObject.AddComponent<UiButton>();

            float sheetW = Layout.SideColumnW + Layout.Margin * 2f;
            var sheet = Node("Sheet", host, 0, 0, sheetW, Layout.ScreenH);
            Img(sheet, null, Tok.Mist, 1f, true);          // takes the pointer so a press inside is not a press outside
            Img(Stretch(Node("Bloom", sheet)), SpriteFactory.Load("Bloom_Page"), Tok.Veil, 0.85f);
            // A hairline where the sheet ends, so it reads as lifted off the page rather than
            // as the page having changed color. Which end that is depends on the shape.
            var edge = Node("Edge", sheet, sheetW - 1f, 0, 1f, Layout.ScreenH);
            Img(edge, null, Tok.Haze2);

            var slot = Node("Slot", sheet, Layout.Margin, 0, Layout.SideColumnW, Layout.ScreenH);

            // The handle, on the stage's own margin where the column used to begin. A sibling
            // of the host rather than a child of it, and created after it so it is drawn over
            // the scrim: it has to be seen while the drawer is shut, which is exactly when the
            // host is at zero. Which margin it hangs off is settled at runtime.
            var tab = Node("LedgerTab", stage, TabX, 0, TabW, TabH);
            tab.anchorMin = tab.anchorMax = new Vector2(0f, 0.5f);
            tab.pivot = new Vector2(0f, 0.5f);
            tab.anchoredPosition = new Vector2(TabX, 0f);
            var tabGround = Img(tab, SpriteFactory.Round(9), Tok.Veil, 1f, true);
            var tabBorder = Img(Stretch(Node("Border", tab)), SpriteFactory.Outline(9), Tok.Haze);
            var tabBtn = tab.gameObject.AddComponent<UiButton>();
            Feel(tab.gameObject, Tok.IrisD, 0.08f);
            // The arrow glyph points right as drawn, which is out of the screen — the way the
            // sheet is about to come. The view turns it over as the sheet arrives.
            var tabArrow = Img(Center(Node("Arrow", tab), 13f, 10f),
                               SpriteFactory.Glyph("Ui", "arrow"), Tok.Ink3);
            tabArrow.raycastTarget = false;
            tab.gameObject.SetActive(false);

            var view = host.gameObject.AddComponent<ColumnDrawer>();
            view.host = host;
            view.hostGroup = group;
            view.scrim = scrim;
            view.sheet = sheet;
            view.sheetEdge = edge;
            view.slot = slot;
            view.tab = tab;
            view.tabButton = tabBtn;
            view.tabArrow = tabArrow;
            view.tabGround = tabGround;
            view.tabBorder = tabBorder;
            host.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The tab: narrow enough to live on the 30 of margin the stage keeps to the
        /// left of the center column, and tall enough to be an easy thing to hit.</summary>
        const float TabW = 20f, TabH = 92f, TabX = 5f;

        /// <summary>The music, as a service beside the clocks. Two sources on the root: one
        /// is always the track being heard and the other is always the one arriving, and they
        /// trade places at every hand-over, which is the only way two pieces of music can
        /// overlap at all.
        ///
        /// The tracks are baked in here rather than loaded from Resources at runtime, so the
        /// folder is read once, by the build, and a player ships exactly what is in it.</summary>
        static void Music(GameObject root)
        {
            var near = root.AddComponent<AudioSource>();
            var far = root.AddComponent<AudioSource>();

            var box = root.AddComponent<Jukebox>();
            box.a = near;
            box.b = far;
            box.crossfadeSeconds = Layout.MusicCrossfade;
            box.avoidLast = 2;                  // never the one playing, never the one before
            box.tracks = MusicFactory.Tracks();
            // Where the Options bars start, so the music is at the level the page will show
            // long before anyone has opened the page. Options takes over the moment it does.
            box.volume = Layout.VolumeMaster * Layout.VolumeMusic;
            box.enabled = true;
        }

        /// <summary>The bars are wired here rather than in their own prefabs because what a
        /// bar item does is a fact about the screen, not about the bar.</summary>
        static void WireBars(BarView left, BarView center, BarView right)
        {
            for (int i = 0; i < center.items.Count; i++)
            {
                var d = (Destination)i;
                var item = center.items[i];
                if (item == null) continue;
                var binder = item.gameObject.AddComponent<DestinationBinder>();
                binder.destination = d;
                binder.item = item;
            }

            // The system bar, left to right: the two drawer handles, then Achievements,
            // Options and Exit. A handle is only ever shown in the shape that folds its own
            // column away, and no shape folds both.
            UtilityBinder.Which[] utilities =
            {
                UtilityBinder.Which.Resources, UtilityBinder.Which.Depths,
                UtilityBinder.Which.Achievements, UtilityBinder.Which.Options,
                UtilityBinder.Which.Exit
            };
            for (int i = 0; i < utilities.Length && i < right.items.Count; i++)
            {
                if (right.items[i] == null) continue;
                var b = right.items[i].gameObject.AddComponent<UtilityBinder>();
                b.utility = utilities[i];
                b.item = right.items[i];
            }

            foreach (var social in left.items)
            {
                if (social == null) continue;
                var b = social.gameObject.AddComponent<UtilityBinder>();
                b.utility = UtilityBinder.Which.Outward;
                b.item = social;
                b.url = Links.For(social.gameObject.name);
            }
        }
    }
}
