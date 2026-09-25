// What Lies In The Depths — Options, the two questions, and the ending.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WhatLiesInTheDepths.EditorTools.UiFactory;
using Palette = Ursine.Theming.Palette;
using ThemedGraphic = Ursine.Theming.ThemedGraphic;
using Ursine.Text;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class BuildUtility
    {
        const float W = Layout.CenterColumnW;
        const float TrackH = Layout.PanelTrackH;
        const float PageW = 560f;                 // a settings page that fills the width
        const float PageX = (W - PageW) * 0.5f;   // reads as an argument, not a set of preferences

        public static void All()
        {
            AchievementMark();
            AchievementGroupHead();
            Achievements();
            Options();
            Question(QuestionView.Kind.Exit, "UI_ExitQuestion");
            Question(QuestionView.Kind.HardReset, "UI_HardResetQuestion");
            Ending();
        }

        static float GroupHead(RectTransform page, string title, float y)
        {
            Caps("Head_" + title, page, 0, y, 300f, 10f, title, 9f, Tok.Ink3);
            Rule("Rule_" + title, page, 0, y + 16f, PageW, Tok.Haze2);
            return y + 30f;
        }

        static (VolumeBar bar, TMP_Text percent) Volume(RectTransform page, string label, float y, float value)
        {
            Txt("Label_" + label, page, 0, y, 200f, 22f, label, TypeRole.Label500, 12.5f, Tok.Ink2,
                TextAlignmentOptions.MidlineLeft);

            var root = Node("Volume_" + label, page, 240f, y + 8f, 280f, 16f);
            var track = Node("Track", root, 0, 5f, 240f, 6f);
            Img(track, SpriteFactory.Round(999), Tok.Track, 1f, true);
            var fill = Node("Fill", track, 0, 0, 240f * value, 6f);
            Img(fill, SpriteFactory.Round(999), Tok.Iris);
            var knob = Node("Knob", root, 240f * value - 8f, 0, 16f, 16f);
            Img(knob, SpriteFactory.Load("Disc"), Tok.Veil);
            Img(Stretch(Node("Ring", knob)), SpriteFactory.Load("Ring_Halo"), Tok.Iris);

            var v = root.gameObject.AddComponent<VolumeBar>();
            v.track = track;
            v.fill = fill;
            v.knob = knob;
            v.width = 240f;
            v.level = value;        // what the fill and the knob above were drawn at

            var pct = Figures("Value", page, 500f, y, 60f, 22f, Fmt.Percent(value * 100), TypeRole.Mono400, 12f,
                              Tok.Ink3, TextAlignmentOptions.MidlineRight);
            return (v, pct);
        }

        static void Options()
        {
            var root = Node("UI_Options", null, 0, 0, W, TrackH);
            // Vertically centered in the center column's height.
            var page = Node("Page", root, PageX, 60f, PageW, 860f);

            float y = 0f;

            // --- Sound
            y = GroupHead(page, "SOUND", y);
            var rows = Node("SoundRows", page, 0, y, PageW, 96f);
            var soundGroup = rows.gameObject.AddComponent<CanvasGroup>();
            var master = Volume(rows, "Master", 0f, Layout.VolumeMaster);
            var music = Volume(rows, "Music", 32f, Layout.VolumeMusic);
            var effects = Volume(rows, "Effects", 64f, Layout.VolumeEffects);
            y += 100f;

            // A 22px checkbox inside the Sound group: a checkbox modifies what is above it.
            Txt("MuteLabel", page, 30f, y, 200f, 22f, "Mute", TypeRole.Label500, 12.5f, Tok.Ink2,
                TextAlignmentOptions.MidlineLeft);
            var muteRt = Node("Mute", page, 0, y, 22f, 22f);
            var muteBox = Img(muteRt, SpriteFactory.Round(7), Tok.Track, 1f, true);
            Img(Stretch(Node("Border", muteRt)), SpriteFactory.Outline(7), Tok.Haze);
            var mute = muteRt.gameObject.AddComponent<UiButton>();
            y += 44f;

            // --- Display
            y = GroupHead(page, "DISPLAY", y);
            Txt("FullScreenLabel", page, 0, y, 200f, 22f, "Full screen", TypeRole.Label500, 12.5f, Tok.Ink2,
                TextAlignmentOptions.MidlineLeft);
            var swRt = Node("FullScreen", page, PageW - 34f, y + 2f, 34f, 19f);
            var swTrack = Img(swRt, SpriteFactory.Round(999), Tok.Track, 1f, true);
            var knob = Node("Knob", swRt, 2f, 2f, 15f, 15f);
            Img(knob, SpriteFactory.Load("Disc"), Tok.Veil);
            var swBtn = swRt.gameObject.AddComponent<UiButton>();
            var fullScreen = swRt.gameObject.AddComponent<ToggleSwitch>();
            Dress(fullScreen);
            fullScreen.button = swBtn;
            fullScreen.track = swTrack;
            fullScreen.knob = knob;
            y += 40f;

            // A palette is shown rather than named, because nobody knows what "Parchment"
            // looks like and everybody can read a stripe.
            Txt("PaletteLabel", page, 0, y, 200f, 20f, "Palette", TypeRole.Label500, 12.5f, Tok.Ink2,
                TextAlignmentOptions.MidlineLeft);
            y += 26f;
            var swatches = new System.Collections.Generic.List<UiButton>();
            var borders = new System.Collections.Generic.List<Image>();
            string[] paletteNames = { "Dream", "Dusk", "Parchment" };
            for (int i = 0; i < 3; i++)
            {
                var card = Node("Swatch_" + paletteNames[i], page, i * 118f, y, 104f, 84f);
                Img(card, SpriteFactory.Round(11), Tok.Veil, 1f, true);
                var border = Img(Stretch(Node("Border", card)), SpriteFactory.Outline(11),
                                 i == 0 ? Tok.Iris : Tok.Haze);
                // A band of ground, agency and ink, with the name beneath.
                Img(Node("Ground", card, 10f, 12f, 84f, 14f), SpriteFactory.Round(999), Tok.Mist);
                Img(Node("Agency", card, 10f, 30f, 84f, 14f), SpriteFactory.Round(999), Tok.Iris);
                Img(Node("Ink", card, 10f, 48f, 84f, 6f), SpriteFactory.Round(999), Tok.Ink);
                Caps("Name", card, 0, 62f, 104f, 12f, paletteNames[i], 9f, Tok.Ink3, 0.16f,
                     TextAlignmentOptions.Center);
                swatches.Add(card.gameObject.AddComponent<UiButton>());
                borders.Add(border);
            }
            y += 100f;

            // Contrast is orthogonal to palette: it touches only the four ink steps and the
            // three rules, and at Hard the four semantic text colors as well.
            Txt("ContrastLabel", page, 0, y, 200f, 22f, "Contrast", TypeRole.Label500, 12.5f, Tok.Ink2,
                TextAlignmentOptions.MidlineLeft);
            var seg = Node("Contrast", page, PageW - 240f, y, 240f, 26f);
            Img(seg, SpriteFactory.Round(999), Tok.Block);
            var contrast = seg.gameObject.AddComponent<SegmentedToggle>();
            Dress(contrast);
            string[] steps = { "Soft", "Firm", "Hard" };
            for (int i = 0; i < 3; i++)
            {
                var segRt = Node(steps[i], seg, 2f + i * 78f, 2f, 78f, 22f);
                var segImg = Img(segRt, SpriteFactory.Round(999), Tok.IrisL, i == 0 ? 1f : 0f, true);
                var segLabel = Caps("Label", segRt, 0, 0, 78f, 22f, steps[i], 9f,
                                    i == 0 ? Tok.IrisD : Tok.Ink3, 0.16f, TextAlignmentOptions.Center);
                contrast.segments.Add(segRt.gameObject.AddComponent<UiButton>());
                contrast.grounds.Add(segImg);
                contrast.labels.Add(segLabel);
            }
            y += 46f;

            // --- The save
            y = GroupHead(page, "THE SAVE", y);
            var saveNote = Node("SaveNote", page, 0, y, PageW, 48f);
            Img(saveNote, SpriteFactory.Round(9), Tok.Block);
            var saveLine = Txt("Line", saveNote, 14f, 0, PageW - 28f, 48f, "", TypeRole.Label400, 12f, Tok.Ink2,
                               TextAlignmentOptions.MidlineLeft);
            y += 66f;

            // --- Beginning again. The only rose ground in any menu, with the consequence
            // written beside the button rather than hidden behind it.
            y = GroupHead(page, "BEGINNING AGAIN", y);
            var block = Node("HardResetBlock", page, 0, y, PageW, 76f);
            var blockImg = Img(block, SpriteFactory.Round(12), Tok.RoseT);
            Img(Stretch(Node("Border", block)), SpriteFactory.Outline(12), Tok.RoseB);
            Prose("Consequence", block, 16f, 16f, 340f, 48f,
                  "Everything the dream has made will be gone, and it does not come back.",
                  13f, Tok.RoseD, 1.4f, TypeRole.SerifItalic);
            var reset = OutlineButton("HardReset", block, 374f, 21f, 170f, 34f, "Hard reset", 15.5f, 9,
                                      Tok.RoseB, Tok.RoseD);

            var view = root.gameObject.AddComponent<OptionsView>();
            view.master = master.bar;
            view.music = music.bar;
            view.effects = effects.bar;
            view.masterValue = master.percent;
            view.musicValue = music.percent;
            view.effectsValue = effects.percent;
            view.mute = mute;
            view.muteBox = muteBox;
            view.soundRows = soundGroup;
            view.fullScreen = fullScreen;
            view.paletteCards = swatches;
            view.paletteBorders = borders;
            view.contrast = contrast;
            foreach (var n in paletteNames)
            {
                var p = UnityEditor.AssetDatabase.LoadAssetAtPath<Palette>(
                    $"{AssetFactory.ResourcesDir}/Palette_{n}.asset");
                view.palettes.Add(p);
            }
            view.saveLine = saveLine;
            view.hardReset = reset.button;
            view.hardResetBlock = blockImg;

            Save(root.gameObject, "UI_Options");
        }

        // ---- Achievements -------------------------------------------------------
        // The spec's Achievements page: a reading band 812 x 116 across the top, then the
        // marks, 84 square, 20 apart, eight to a row, under a head for each group.
        const float AchX = 34f, AchW = 812f, BandY = 24f, BandH = 116f, MarksY = 164f;

        static void AchievementMark()
        {
            var root = Node("UI_AchievementMark", null, 0, 0, 84f, 84f);
            var ground = Img(root, SpriteFactory.Round(16), Tok.Block, 1f, true);
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(16), Tok.Haze);
            var glyph = Img(Node("Glyph", root, 24f, 24f, 36f, 36f),
                            SpriteFactory.Glyph("Tab", "achievements") ?? SpriteFactory.Load("Disc"), Tok.IrisD);
            glyph.enabled = false;
            var btn = root.gameObject.AddComponent<UiButton>();

            var m = root.gameObject.AddComponent<WhatLiesInTheDepths.UI.AchievementMark>();
            m.button = btn;
            m.ground = ground;
            m.border = border;
            m.glyph = glyph;
            Save(root.gameObject, "UI_AchievementMark");
        }

        static void AchievementGroupHead()
        {
            var root = Node("UI_AchievementGroupHead", null, 0, 0, AchW, 20f);
            Caps("Name", root, 0, 0, 400f, 12f, "", 9f, Tok.Ink3);
            Figures("Count", root, AchW - 120f, 0, 120f, 12f, "", TypeRole.Mono400, 10.5f, Tok.Ink4,
                    TextAlignmentOptions.MidlineRight);
            Rule("Rule", root, 0, 18f, AchW, Tok.Haze2);
            Save(root.gameObject, "UI_AchievementGroupHead");
        }

        static void Achievements()
        {
            var root = Node("UI_Achievements", null, 0, 0, W, TrackH);

            // The band. Veil ground and haze edge at rest; iris-l and iris-b on an earned mark.
            var band = Node("Band", root, AchX, BandY, AchW, BandH);
            var bandGround = Img(band, SpriteFactory.Round(14), Tok.Veil);
            var bandBorder = Img(Stretch(Node("Border", band)), SpriteFactory.Outline(14), Tok.Haze);

            var box = Node("Mark", band, 29f, 29f, 58f, 58f);
            var markGround = Img(box, SpriteFactory.Round(13), Tok.Block);
            var markBorder = Img(Stretch(Node("Border", box)), SpriteFactory.Outline(13), Tok.Haze2);
            var markGlyph = Img(Node("Glyph", box, 17f, 17f, 24f, 24f),
                                SpriteFactory.Glyph("Tab", "achievements") ?? SpriteFactory.Load("Disc"), Tok.Ink3);

            const float TextX = 110f, TextW = AchW - 110f - 28f;
            var kicker = Caps("Kicker", band, TextX, 26f, TextW, 12f, "", 9f, Tok.Ink3);
            var title = Txt("Title", band, TextX, 40f, TextW, 30f, "", TypeRole.Serif, 22f, Tok.Ink);
            var body = Prose("Body", band, TextX, 74f, TextW, 24f, "", 13f, Tok.Ink2, 1.4f, TypeRole.Label400);
            var hint = Prose("Hint", band, TextX, 44f, TextW, 50f, "", 17f, Tok.Ink2, 1.4f, TypeRole.SerifItalic);

            // The marks scroll beneath the band; the band stays.
            var (scroll, content) = Scroll("Marks", root, 0, MarksY, W, TrackH - MarksY);
            var stack = content.GetComponent<VerticalLayoutGroup>();
            if (stack != null) Object.DestroyImmediate(stack);
            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null) Object.DestroyImmediate(fitter);
            content.sizeDelta = new Vector2(W, 0f);

            var view = root.gameObject.AddComponent<AchievementsView>();
            view.bandGround = bandGround;
            view.bandBorder = bandBorder;
            view.markGround = markGround;
            view.markBorder = markBorder;
            view.markGlyph = markGlyph;
            view.kicker = kicker;
            view.title = title;
            view.body = body;
            view.hint = hint;
            view.content = content;
            view.groupHeadPrefab = Load("UI_AchievementGroupHead");
            view.markPrefab = Load("UI_AchievementMark").GetComponent<WhatLiesInTheDepths.UI.AchievementMark>();
            view.left = AchX;
            view.top = 0f;
            view.width = AchW;

            Save(root.gameObject, "UI_Achievements");
        }

        // 560 wide, radius 14, a 44px mark, a Cormorant 27px title and a serif italic body,
        // with the save in a block panel beneath. Neither is a window.
        static void Question(QuestionView.Kind kind, string prefabName)
        {
            bool reset = kind == QuestionView.Kind.HardReset;
            var root = Node(prefabName, null, 0, 0, W, TrackH);

            var card = Panel("Question", root, PageX, 300f, PageW, 330f, 14);
            var frame = card.Find("Border").GetComponent<Image>();
            var mark = Img(Node("Mark", card, 28f, 28f, 44f, 44f),
                           (reset ? null : SpriteFactory.Glyph("Tab", "exit")) ?? SpriteFactory.Load("Disc"),
                           reset ? Tok.RoseD : Tok.IrisD);

            var title = Txt("Title", card, 88f, 30f, 440f, 34f, "", TypeRole.Serif, 27f, Tok.Ink);
            var body = Prose("Body", card, 28f, 88f, 504f, 60f, "", 15f, Tok.Ink2, 1.6f, TypeRole.SerifItalic);

            var note = Node("SaveNote", card, 28f, 158f, 504f, 46f);
            Img(note, SpriteFactory.Round(9), Tok.Block);
            var saveNote = Txt("Line", note, 14f, 0, 476f, 46f, "", TypeRole.Label400, 12f, Tok.Ink2,
                               TextAlignmentOptions.MidlineLeft);
            note.gameObject.SetActive(!reset);

            // No sits first, because it is the safe answer and the cheap one.
            var no = PrimaryButton("No", card, 28f, 226f, 240f, 40f, "No", 16f, 10);
            var yes = OutlineButton("Yes", card, 292f, 226f, 240f, 40f, "Yes", 16f, 10, Tok.RoseB, Tok.RoseD);

            var view = root.gameObject.AddComponent<QuestionView>();
            view.kind = kind;
            view.frame = frame;
            view.mark = mark;
            view.title = title;
            view.body = body;
            view.saveNote = saveNote;
            view.no = no.button;
            view.noGround = no.ground;
            view.noLabel = no.label;
            view.yes = yes.button;
            view.yesGround = yes.root.GetComponent<Image>();
            view.yesBorder = yes.border;
            view.yesLabel = yes.label;

            Save(root.gameObject, prefabName);
        }

        // The full 1920 x 1080, edge to edge, on the screen's own ground with the bloom
        // moved to the top center. No margin, no plate, no frame — the first time in the
        // game that anything ignores the three-column budget, and the last.
        static void Ending()
        {
            var root = Node("UI_Ending", null, 0, 0, Layout.ScreenW, Layout.ScreenH);
            Img(root, null, Tok.Mist);
            Img(Stretch(Node("Bloom", root)), SpriteFactory.Load("Bloom_Page"), Tok.Veil, 0.8f);

            var (scroll, content) = Scroll("Scroll", root, 0, 0, Layout.ScreenW, Layout.ScreenH);
            Stack(content, 0f, new RectOffset(0, 0, 0, 132));   // the scroll reserves 132

            // Art 1920 x 460, full bleed at the top, its last 150 dissolving into the ground.
            var artHolder = Node("Art", content, 0, 0, Layout.ScreenW, 460f);
            artHolder.gameObject.AddComponent<LayoutElement>().preferredHeight = 460f;
            var art = Img(Stretch(Node("Image", artHolder)), SpriteFactory.Load("Plate_Ending"), Tok.Veil);
            art.color = Color.white;
            var at = art.GetComponent<ThemedGraphic>();
            if (at != null) Object.DestroyImmediate(at);
            var dissolve = Node("Dissolve", artHolder, 0, 310f, Layout.ScreenW, 150f);
            Img(dissolve, SpriteFactory.Load("Scrim_Up"), Tok.Mist);

            var titleBlock = Node("TitleBlock", content, 0, 0, Layout.ScreenW, 120f);
            titleBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
            var smallCaps = Caps("SmallCaps", titleBlock, 0, 24f, Layout.ScreenW, 12f, "THE DREAM ENDS", 10f,
                                 Tok.Ink3, 0.20f, TextAlignmentOptions.Center);
            // The largest type anywhere in the game by a factor of two, and it is allowed to
            // be because nothing else is on this surface competing for the eye.
            var title = Txt("Title", titleBlock, 0, 44f, Layout.ScreenW, 64f, "", TypeRole.Serif, 52f, Tok.Ink,
                            TextAlignmentOptions.Center);

            var message = Node("Message", content, 0, 0, 760f, 300f);
            message.gameObject.AddComponent<LayoutElement>().preferredHeight = 300f;
            var messageStack = Node("Blocks", message, (Layout.ScreenW - 760f) * 0.5f, 0, 760f, 300f);
            Stack(messageStack, 22f);
            for (int i = 0; i < 3; i++)
                Prose("Paragraph" + i, messageStack, 0, 0, 760f, 80f,
                      "Placeholder ending prose. Rewrite freely: nothing depends on the words except their length.",
                      17.5f, Tok.Prose, 1.74f);

            var credits = Node("Credits", content, 0, 0, Layout.ScreenW, 120f);
            credits.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
            string[] roles = { "DESIGN", "CODE", "ART" };
            for (int i = 0; i < 3; i++)
            {
                float cy = 12f + i * 34f;
                Caps("Role" + i, credits, Layout.ScreenW * 0.5f - 180f, cy, 112f, 12f, roles[i], 9f, Tok.Ink3,
                     0.16f, TextAlignmentOptions.Right);
                // Placeholders sit in ink3 italic, which is how you can see at a glance
                // which are still open.
                var n = Txt("Name" + i, credits, Layout.ScreenW * 0.5f - 52f, cy - 6f, 220f, 24f,
                            "Placeholder", TypeRole.SerifItalic, 19f, Tok.Ink3, TextAlignmentOptions.MidlineLeft);
            }

            var plug = Node("Plug", content, 0, 0, 760f, 150f);
            plug.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
            var plugPanel = Node("Panel", plug, (Layout.ScreenW - 760f) * 0.5f, 0, 760f, 130f);
            Img(plugPanel, SpriteFactory.Round(12), Tok.Block);
            Img(Stretch(Node("Border", plugPanel)), SpriteFactory.Outline(12), Tok.Haze2);
            Txt("Title", plugPanel, 24f, 20f, 700f, 30f, "Placeholder", TypeRole.Serif, 23f, Tok.Ink);
            Prose("Body", plugPanel, 24f, 56f, 700f, 56f, "", 14f, Tok.Ink2, 1.6f, TypeRole.SerifItalic);

            // Answers pinned to the bottom over a gradient scrim — the DepthGauge plate
            // foot, reused at full width.
            var answers = Node("Answers", root, 0, Layout.ScreenH - 132f, Layout.ScreenW, 132f);
            Img(answers, SpriteFactory.Load("Scrim_Up"), Tok.Mist);
            float ax = Layout.ScreenW * 0.5f;
            var hardReset = OutlineButton("HardReset", answers, ax - 340f, 40f, 200f, 46f, "Hard reset", 18f, 11,
                                          Tok.RoseB, Tok.RoseD);
            var cont = PrimaryButton("Continue", answers, ax - 126f, 40f, 252f, 46f, "Continue", 18f, 11);
            var exit = OutlineButton("Exit", answers, ax + 140f, 40f, 200f, 46f, "Exit", 18f, 11,
                                     Tok.Haze, Tok.Ink2);

            var view = root.gameObject.AddComponent<EndingView>();
            view.art = art;
            view.smallCapsLine = smallCaps;
            view.title = title;
            view.message = messageStack;
            view.credits = credits;
            view.hardReset = hardReset.button;
            view.hardResetBorder = hardReset.border;
            view.hardResetLabel = hardReset.label;
            view.cont = cont.button;
            view.contGround = cont.ground;
            view.contLabel = cont.label;
            view.exit = exit.button;
            view.exitBorder = exit.border;
            view.exitLabel = exit.label;

            Save(root.gameObject, "UI_Ending");
        }
    }
}
