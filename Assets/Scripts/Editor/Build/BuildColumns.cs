// What Lies In The Depths — the three bars, the left column and the right column.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WhatLiesInTheDepths.EditorTools.UiFactory;
using ThemedGraphic = Ursine.Theming.ThemedGraphic;
using Ursine.Text;
using Ursine.UI;
using Ursine.Economy;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class BuildColumns
    {
        public static void All()
        {
            Bars();
            Ledger();
            NowPlaying();
            Gauge();
        }

        // ---- the bars ----------------------------------------------------------
        // Height 31 in all three: 19 of label, 11 of clearance, 1 of rule. Spacing 30
        // between items, 2 side padding. The rule is drawn whether there is one item or
        // seven, and no bar scrolls or wraps.
        static RectTransform BarShell(string name, float width, TextAnchor align)
        {
            var root = Node(name, null, 0, 0, width, Layout.BarH);
            Img(root, null, Tok.Mist, 0f);                       // no ground, no border but the rule

            var row = Node("Items", root, 2f, 0, width - 4f, Layout.BarH);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30f;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var rule = Rule("Rule", root, 0, Layout.BarH - 1f, width);

            var bar = root.gameObject.AddComponent<BarView>();
            bar.itemsRow = row;
            bar.rule = rule;
            return root;
        }

        static BarItem AddItem(RectTransform bar, string prefab, string text, string glyphName = null)
        {
            var row = bar.GetComponent<BarView>().itemsRow;
            var go = Nest(prefab, row, 0, 0);
            var item = go.GetComponent<BarItem>();
            if (item != null && item.label != null)
            {
                item.label.text = item.kind == BarItemKind.Utility ? text.ToUpperInvariant() : text;
                Localize(item.label, item.label.text);
                go.name = prefab.Contains("Utility") ? "Utility " + text : text;
            }
            // The spec's own tab glyph, drawn white and tinted with the label. The set draws
            // nine of the eleven section 16 asks for — Journal and Discord were never drawn —
            // so a missing glyph hides rather than showing a stand-in shape beside real ones.
            if (item != null && item.glyph != null)
            {
                var sprite = SpriteFactory.Glyph("Tab", text.ToLowerInvariant());
                item.glyph.sprite = sprite;
                item.glyph.enabled = sprite != null;
            }

            bar.GetComponent<BarView>().items.Add(item);
            return item;
        }

        /// <summary>Widens a utility item for a longer word: the hit area, the label and the
        /// underline all follow the item's width.</summary>
        static void FitUtility(BarItem item, float w)
        {
            var rt = (RectTransform)item.transform;
            rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            if (item.label != null)
            {
                var lr = (RectTransform)item.label.transform;
                lr.sizeDelta = new Vector2(w - 20f - 12f, lr.sizeDelta.y);
            }
            if (item.underline != null) item.underline.sizeDelta = new Vector2(w - 8f, item.underline.sizeDelta.y);
        }

        static void Bars()
        {
            // Left — 450. The community, pinned to the far left, which is the left edge of
            // the screen. No destination: the ResourceLedger is simply what is under this
            // bar, always, and naming it would be a label rather than a choice.
            var left = BarShell("UI_Bar_Left", Layout.SideColumnW, TextAnchor.LowerLeft);
            foreach (var n in new[] { "Discord", "Reddit", "Twitter" })
            {
                var item = AddItem(left, "UI_BarItem_Utility", n);
                // Outward links carry a small arrow and open in the browser; they never take
                // the underline and never carry a dot.
                if (item != null && item.underline != null) item.underline.gameObject.SetActive(false);
            }
            Save(left.gameObject, "UI_Bar_Left");

            // Center — 880. Six destinations, nothing else, left-aligned. Six names measure
            // 748 of the center's 880, so there is room for one more of ordinary length.
            var center = BarShell("UI_Bar_Center", Layout.CenterColumnW, TextAnchor.LowerLeft);
            foreach (var n in new[] { "Journal", "Focus", "Constructs", "Revelations", "Visions", "Assault" })
                AddItem(center, "UI_BarItem_Destination", n);
            Save(center.gameObject, "UI_Bar_Center");

            // Right — 450. The system, pinned to the far right, which is the top-right
            // corner of the screen, where Exit belongs. The rule still runs the full 450.
            var right = BarShell("UI_Bar_Right", Layout.SideColumnW, TextAnchor.LowerRight);
            // Achievements first: it is absent until the first mark is earned, and arriving
            // on the left of the pair leaves Options and Exit exactly where they always were.
            var achievements = AddItem(right, "UI_BarItem_Utility", "Achievements");
            if (achievements != null)
            {
                if (achievements.outwardArrow != null) achievements.outwardArrow.gameObject.SetActive(false);
                achievements.GetComponent<LayoutElement>().preferredWidth = 140f;
                FitUtility(achievements, 140f);
            }
            var options = AddItem(right, "UI_BarItem_Utility", "Options");
            if (options != null && options.outwardArrow != null)
                options.outwardArrow.gameObject.SetActive(false);   // Options does not leave the game
            var exit = AddItem(right, "UI_BarItem_Utility", "Exit");
            if (exit != null)
            {
                exit.hoversRose = true;                             // Exit alone hovers to rose
                if (exit.underline != null) exit.underline.gameObject.SetActive(false);
                if (exit.outwardArrow != null) exit.outwardArrow.gameObject.SetActive(false);
            }
            Save(right.gameObject, "UI_Bar_Right");
        }

        // ---- the left column ---------------------------------------------------
        // 450 x 501 at fifteen resources, radius 14, 5 vertical padding. It does not
        // scroll, and it is not interactive: a readout, and it behaves like one.
        static void Ledger()
        {
            var root = Panel("UI_ResourceLedger", null, 0, 0, Layout.SideColumnW, 501f, 14);

            var clip = Stretch(Node("Clip", root, 1, 5, 1, 5));
            clip.gameObject.AddComponent<RectMask2D>();

            var content = Node("Content", clip, 0, 0, Layout.SideColumnW - 2f, 0f);
            Stretch(content);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            Stack(content, 0f);

            var view = root.gameObject.AddComponent<LedgerView>();
            view.content = content;
            view.rowPrefab = Load("UI_ResourceRow").GetComponent<ResourceRow>();
            view.groupCaptionPrefab = Load("UI_GroupCaption");

            Save(root.gameObject, "UI_ResourceLedger");
        }

        // ---- the foot of the left column ---------------------------------------
        // The reserved foot stays empty but for this: a caption and a line, on the mist
        // rather than on a panel, because a panel down there would read as a fourth thing
        // the dream contains. It is the only readout on the screen that is not about the
        // dream, which is why it is the quietest — Ink4, the faintest step there is.
        //
        // The line is not a scroller by default. It is measured against its window and only
        // moves if it cannot be read standing still, which for these titles is almost never.
        static void NowPlaying()
        {
            var root = Node("UI_NowPlaying", null, 0, 0, Layout.NowPlayingW, Layout.NowPlayingH);

            // Silent until a track starts: with no music in the project there is nothing to
            // say, and a caption over an empty line would be a promise the build cannot keep.
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var caption = Caps("Caption", root, 0, 0, 200f, 10f, "NOW PLAYING", 9f, Tok.Ink4);

            // The window the line is read through. Anything past its edges is cut here, which
            // is what lets a long title move without running into the ledger above it.
            var window = Node("Line", root, 0, 13f, Layout.NowPlayingW, 21f);
            window.gameObject.AddComponent<RectMask2D>();
            var lineGroup = window.gameObject.AddComponent<CanvasGroup>();

            var label = Line("Label", window);
            var echo = Line("Echo", window);
            echo.gameObject.SetActive(false);

            var marquee = window.gameObject.AddComponent<Marquee>();
            marquee.viewport = window;
            marquee.label = label;
            marquee.echo = echo;
            marquee.group = lineGroup;
            marquee.pixelsPerSecond = 20f;
            marquee.gap = 64f;
            marquee.holdSeconds = 2.5f;
            marquee.fadeSeconds = 0.5f;

            var view = root.gameObject.AddComponent<NowPlayingView>();
            view.marquee = marquee;
            view.caption = caption;
            view.group = group;

            Save(root.gameObject, "UI_NowPlaying");
        }

        /// <summary>One copy of the title: set to run off the end of its rect rather than
        /// wrap or trail off, because the marquee moves the rect rather than the words.</summary>
        static TMP_Text Line(string name, RectTransform window)
        {
            var t = Txt(name, window, 0, 0, Layout.NowPlayingW, 21f, string.Empty,
                        TypeRole.Serif, 13.5f, Tok.Ink3, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // ---- the right column --------------------------------------------------
        // 450 wide, radius 16, height varies with how much of the entry is written.
        static void Gauge()
        {
            const float W = Layout.SideColumnW;
            var root = Panel("UI_DepthGauge", null, 0, 0, W, Layout.PanelTrackH, 16);

            // Plate 448 x 266 — the panel's 450 less its 1px border. Full bleed, clipped.
            var plateHolder = Node("Plate", root, 1f, 1f, 448f, 266f);
            plateHolder.gameObject.AddComponent<RectMask2D>();
            var plate = Img(Stretch(Node("Art", plateHolder)), SpriteFactory.Load("Plate_Veil"), Tok.Veil);
            plate.color = Color.white;
            var themed = plate.GetComponent<ThemedGraphic>();
            if (themed != null) Object.DestroyImmediate(themed);   // artwork does not follow the palette

            var scrimTop = Node("ScrimTop", plateHolder, 0, 0, 448f, 70f);
            Img(scrimTop, SpriteFactory.Load("Scrim_Down"), Tok.Veil, 0.9f);
            var ordinal = Caps("Ordinal", plateHolder, 18f, 14f, 200f, 12f, "SECOND VEIL", 9f, Tok.IrisD);

            var scrimFoot = Node("ScrimFoot", plateHolder, 0, 176f, 448f, 90f);
            Img(scrimFoot, SpriteFactory.Load("Scrim_Up"), Tok.Veil);
            var veilName = Txt("VeilName", plateHolder, 18f, 222f, 412f, 34f, "The Silt Shore",
                               TypeRole.Serif, 26f, Tok.Ink, TextAlignmentOptions.BottomLeft);

            // Dive block: ground block, border haze2, radius 12, padding 15, gap 12.
            var dive = Block("DiveBlock", root, 14f, 282f, 422f, 260f, 12);

            // Depth Gauge spec, fathoms: "FATHOMS SUNK" in 9px caps on the left; on the right,
            // baseline to baseline, the count and the total in mono 17 with "of" in Cormorant 15
            // between them, a word-space either side. The whole readout turns teal at full.
            var fathomCap = Caps("FathomCaption", dive, 15f, 15f, 160f, 22f, "FATHOMS SUNK", 9f, Tok.Ink3, 0.16f,
                                 TextAlignmentOptions.BaselineLeft);
            var fathomRow = Node("Fathoms", dive, 15f, 15f, 392f, 22f);
            var frow = fathomRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            frow.spacing = 4.5f;
            frow.childAlignment = TextAnchor.UpperRight;
            frow.childControlWidth = true;
            frow.childControlHeight = false;
            frow.childForceExpandWidth = false;
            frow.childForceExpandHeight = false;
            var sunk = Figures("FathomSunk", fathomRow, 0, 0, 60f, 22f, "412", TypeRole.Mono500, 17f, Tok.Ink,
                               TextAlignmentOptions.BaselineRight);
            var of = Txt("FathomOf", fathomRow, 0, 0, 20f, 22f, "of", TypeRole.Serif, 15f, Tok.Ink3,
                         TextAlignmentOptions.Baseline);
            var need = Figures("FathomNeed", fathomRow, 0, 0, 60f, 22f, "1,000", TypeRole.Mono500, 17f, Tok.Ink,
                               TextAlignmentOptions.BaselineRight);
            var progress = Track("Progress", dive, 15f, 44f, 392f, 4f);

            // Ledger lines carry 7 of padding for a refusal's ground; the column hangs 7 left
            // of its caption so the words still line up (margin-left: -7).
            var spent = Node("Spent", dive, 8f, 62f, 193f, 60f);
            // Each line only as wide as it says, so a refusal's ground hugs its words.
            var spentStack = Stack(spent, 3f);
            spentStack.childForceExpandWidth = false;
            spentStack.childAlignment = TextAnchor.UpperLeft;
            Caps("Caption", spent, 0, 0, 160f, 10f, "SPENT, EACH DIVE", 8f, Tok.RoseD).margin = new Vector4(7f, 0f, 0f, 0f);
            var mid = Rule("Rule", dive, 210f, 62f, 1f, Tok.Haze);
            ((RectTransform)mid.transform).sizeDelta = new Vector2(1f, 60f);
            var brought = Node("Brought", dive, 214f, 62f, 193f, 60f);
            var broughtStack = Stack(brought, 3f);
            broughtStack.childForceExpandWidth = false;
            broughtStack.childAlignment = TextAnchor.UpperLeft;
            Caps("Caption", brought, 0, 0, 160f, 10f, "BROUGHT UP", 8f, Tok.TealD).margin = new Vector4(7f, 0f, 0f, 0f);

            var rateLine = Txt("RateLine", dive, 15f, 130f, 392f, 20f, "a dive every 6.0s",
                               TypeRole.SerifItalic, 13.5f, Tok.Ink2, TextAlignmentOptions.MidlineLeft);

            Txt("OneiriLabel", dive, 15f, 156f, 180f, 24f, "Oneiri bound", TypeRole.Serif, 18f, Tok.Ink,
                TextAlignmentOptions.MidlineLeft);
            var stepperGo = Nest("UI_Stepper", dive, 277f, 154f);
            var stepper = stepperGo.GetComponent<Ursine.UI.Stepper>();

            // Dive control: the Focus action button in the gauge's own place, because diving
            // is a Focus. 44 tall, radius 10: outlined with a faint fill and an arrow when
            // your attention is elsewhere; filled iris, the progress a darker iris and the
            // attention mark when it is here. The fill is clipped to the corners.
            var diveCtl = Node("DiveControl", dive, 15f, 196f, 392f, 44f);
            var diveGround = Img(diveCtl, SpriteFactory.Round(10), Tok.Iris, 0f, true);
            var diveFillClip = Stretch(Node("FillClip", diveCtl));
            var clip = Img(diveFillClip, SpriteFactory.Round(10), Tok.Veil);
            clip.raycastTarget = false;
            diveFillClip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var diveFill = Node("Fill", diveFillClip, 0, 0, 392f, 44f);
            diveFill.anchorMin = new Vector2(0f, 0f);
            diveFill.anchorMax = new Vector2(0f, 1f);
            diveFill.offsetMin = Vector2.zero;
            diveFill.offsetMax = Vector2.zero;
            var diveFillImg = Img(diveFill, null, Tok.Iris, 0.17f);
            diveFillImg.raycastTarget = false;
            var diveBorder = Img(Stretch(Node("Border", diveCtl)), SpriteFactory.Outline(10), Tok.IrisB);
            diveBorder.raycastTarget = false;
            var diveLabel = Txt("Label", diveCtl, 14f, 0, 320f, 44f, "Sink a fathom", TypeRole.Serif, 18f, Tok.IrisD,
                                TextAlignmentOptions.MidlineLeft);
            diveLabel.raycastTarget = false;
            var diveArrow = Img(Node("Arrow", diveCtl, 392f - 14f - 17f, 16f, 17f, 12f),
                                SpriteFactory.Glyph("Ui", "arrow"), Tok.IrisD);
            diveArrow.raycastTarget = false;
            var diveMark = Img(Node("Mark", diveCtl, 392f - 14f - 14f, 15f, 14f, 14f),
                               SpriteFactory.Glyph("Ui", "mark"), Tok.Veil);
            diveMark.raycastTarget = false;
            var diveBtn = diveCtl.gameObject.AddComponent<UiButton>();
            var diveSub = Txt("SubLine", dive, 15f, 242f, 392f, 14f, "", TypeRole.Label400, 11f, Tok.Ink3,
                              TextAlignmentOptions.MidlineLeft);

            // The entry never scrolls — a veil's story is short by design, so the whole
            // panel, including the control that parts the veil, always fits its track.
            // The entry sits in a clip. If a veil's text is ever longer than the track allows,
            // its foot is cropped rather than the part control being pushed off the screen.
            var entryClip = Node("EntryClip", root, 24f, 558f, 402f, 300f);
            entryClip.gameObject.AddComponent<RectMask2D>();
            var entry = Node("Entry", entryClip, 0f, 0f, 402f, 300f);
            Stack(entry, 22f);

            // Depth Gauge spec, .turn: the gauge's one filled control. Iris, 52 tall, radius 11,
            // Cormorant 600 19 in white with a down arrow 10 after it, an iris-d shadow under
            // it; then the .turnsub line centered, italic 11 in ink-3, 8 below. It follows the
            // last paragraph by the body's 17 gap, and the panel ends 18 under it — GaugeView
            // places both, because the entry above is as tall as what has been written.
            var part = Node("PartControl", root, 24f, 866f, 402f, 52f);
            var partShadow = Img(Stretch(Node("Shadow", part), -6f, 4f, -6f, -16f),
                                 SpriteFactory.Load("Shadow_Primary"), Tok.IrisD, 0.55f);
            partShadow.raycastTarget = false;
            var partGround = Img(Stretch(Node("Ground", part)), SpriteFactory.Round(11), Tok.Iris, 1f, true);
            var partBtn = partGround.gameObject.AddComponent<UiButton>();
            var partRow = Stretch(Node("Row", part));
            var prow = partRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            prow.spacing = 10f;
            prow.childAlignment = TextAnchor.MiddleCenter;
            prow.childControlWidth = true;
            prow.childControlHeight = false;
            prow.childForceExpandWidth = false;
            prow.childForceExpandHeight = false;
            var partLabel = Txt("Label", partRow, 0, 0, 120f, 52f, "Part the veil", TypeRole.Serif, 19f, Tok.Veil,
                                TextAlignmentOptions.Center);
            partLabel.color = Color.white;
            var tg = partLabel.GetComponent<ThemedGraphic>();
            if (tg != null) Object.DestroyImmediate(tg);   // the spec's #fff, not the veil token
            var arrowHolder = Node("Arrow", partRow, 0, 0, 12f, 18f);
            var arrowLe = arrowHolder.gameObject.AddComponent<LayoutElement>();
            arrowLe.preferredWidth = 12f;
            var partArrow = Img(Center(Node("Glyph", arrowHolder), 16f, 12f), SpriteFactory.Glyph("Ui", "arrow"), Tok.Veil);
            partArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            partArrow.color = Color.white;
            var ag = partArrow.GetComponent<ThemedGraphic>();
            if (ag != null) Object.DestroyImmediate(ag);

            var partSub = Txt("PartSubLine", root, 24f, 926f, 402f, 14f, "the way back closes behind you",
                              TypeRole.Label400, 11f, Tok.Ink3, TextAlignmentOptions.Center);
            partSub.fontStyle = FontStyles.Italic;

            var view = root.gameObject.AddComponent<GaugeView>();
            view.plate = plate;
            view.ordinal = ordinal;
            view.veilName = veilName;
            view.fathomSunk = sunk;
            view.fathomCaption = fathomCap;
            view.fathomOf = of;
            view.fathomNeed = need;
            view.progress = progress;
            view.spentColumn = spent;
            view.broughtColumn = brought;
            view.rateLine = rateLine;
            view.stepper = stepper;
            view.diveControl = diveBtn;
            view.diveLabel = diveLabel;
            view.diveFill = diveFill;
            view.diveGround = diveGround;
            view.diveFillImage = diveFillImg;
            view.diveBorder = diveBorder;
            view.diveArrow = diveArrow;
            view.diveMark = diveMark;
            view.diveSubLine = diveSub;
            view.diveBlock = (RectTransform)dive;
            view.entryColumn = entry;
            view.entryClip = entryClip;
            view.paragraphPrefab = Load("UI_Paragraph");
            view.ruledBlockPrefab = Load("UI_RuledBlock");
            view.partControl = partBtn;
            view.partRoot = part;
            view.partGround = partGround;
            view.partSubLine = partSub;
            view.panel = root;
            view.ledgerLinePrefab = Load("UI_LedgerLine");

            Save(root.gameObject, "UI_DepthGauge");
        }
    }
}
