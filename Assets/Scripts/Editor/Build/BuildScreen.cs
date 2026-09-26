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
            // sits on the same baseline the columns end on rather than one of its own.
            Nest("UI_NowPlaying", left, 0, Layout.ColumnH - Layout.NowPlayingH);

            var centerSurfaces = Node("Surfaces", center, 0, Layout.PanelTopY - Layout.ColumnY,
                                      Layout.CenterColumnW, Layout.PanelTrackH);

            var screen = root.AddComponent<ScreenRoot>();
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

            // Right bar, left to right: Achievements, Options, Exit.
            UtilityBinder.Which[] utilities =
            {
                UtilityBinder.Which.Achievements, UtilityBinder.Which.Options, UtilityBinder.Which.Exit
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
