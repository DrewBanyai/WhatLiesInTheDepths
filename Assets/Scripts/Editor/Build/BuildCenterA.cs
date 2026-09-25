// What Lies In The Depths — center destinations: Journal and Focus.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WhatLiesInTheDepths.EditorTools.UiFactory;
using Ursine.Text;
using Ursine.UI;
using Ursine.Economy;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class BuildCenterA
    {
        const float W = Layout.CenterColumnW;     // 880
        const float TrackH = Layout.PanelTrackH;   // 985

        public static void All()
        {
            BuildJournalEntry();
            Journal();
            FocusCard();
            Focus();
        }

        // ---- a journal entry ---------------------------------------------------
        // Journal spec, .jent: a row, 28 above and below, 26 between a 92 day rail and a body
        // capped at 660 — the same measure the veil entries use one column over, which is not
        // a coincidence: it is the same person writing. A 1px wait rule closes each entry.
        // Everything is laid out rather than placed, so an entry is as tall as what it says.
        static LayoutElement Le(Component c) =>
            c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();

        static void BuildJournalEntry()
        {
            var root = Node("UI_JournalEntry", null, 0, 0, W - 54f, 200f);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 26f;
            row.padding = new RectOffset(0, 0, 28, 28);
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            // .jrail: a right-aligned column, 1 between, 2 down. Right-aligned so that 96 and
            // 412 share an edge rather than a starting point.
            var rail = Node("DayRail", root, 0, 0, 92f, 40f);
            var rv = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            rv.spacing = 1f;
            rv.padding = new RectOffset(0, 0, 2, 0);
            rv.childAlignment = TextAnchor.UpperRight;
            rv.childControlWidth = true;
            rv.childControlHeight = true;
            rv.childForceExpandWidth = true;
            rv.childForceExpandHeight = false;
            var rle = Le(rail);
            rle.minWidth = 92f;
            rle.preferredWidth = 92f;
            rle.flexibleWidth = 0f;

            var dayLabel = Caps("DayLabel", rail, 0, 0, 92f, 10f, "DAY", 8.5f, Tok.Ink4, 0.18f,
                                TextAlignmentOptions.Right);
            var dayNumber = Figures("DayNumber", rail, 0, 0, 92f, 26f, "0", TypeRole.Mono500, 25f, Tok.Ink,
                                    TextAlignmentOptions.TopRight);
            dayNumber.characterSpacing = -3f;
            Typeset.Leading(dayNumber, 1.05f);

            // .jbody: a column, 9 between: source line, name, then each paragraph.
            var body = Node("Body", root, 0, 0, 660f, 160f);
            var bv = body.gameObject.AddComponent<VerticalLayoutGroup>();
            bv.spacing = 9f;
            bv.childAlignment = TextAnchor.UpperLeft;
            bv.childControlWidth = true;
            bv.childControlHeight = true;
            bv.childForceExpandWidth = true;
            bv.childForceExpandHeight = false;
            var ble = Le(body);
            ble.preferredWidth = 660f;
            ble.flexibleWidth = 0f;

            // .jsrc, 14 tall, 7 between: the 6px unread dot (plain — no halo — and its place
            // kept once read), the 13px source glyph, the word in 8.5 caps at .18em.
            var srcRow = Node("Source", body, 0, 0, 660f, 14f);
            Le(srcRow).preferredHeight = 14f;
            var dotRoot = Node("Unread", srcRow, 0, 4f, 6f, 6f);
            Img(dotRoot, SpriteFactory.Load("Disc"), Tok.Iris);
            var dot = dotRoot.gameObject.AddComponent<AttentionDot>();
            dot.root = dotRoot.gameObject;
            var srcGlyph = Img(Node("Glyph", srcRow, 14f, 0.5f, 13f, 13f), SpriteFactory.Glyph("Source", "veil") ?? SpriteFactory.Load("Disc"), Tok.Ink3);
            var srcWord = Caps("Word", srcRow, 34f, 0f, 400f, 14f, "A VEIL", 8.5f, Tok.Ink3, 0.18f);

            // .ename: Cormorant 600 25 / 1.1.
            var entryName = Txt("Name", body, 0, 0, 660f, 28f, "Entry", TypeRole.Serif, 25f, Tok.Ink,
                                TextAlignmentOptions.TopLeft);
            entryName.characterSpacing = -0.5f;
            Typeset.Wrap(entryName);
            Typeset.Leading(entryName, 1.1f);

            // The closing rule sits on the entry's foot, outside the layout.
            var ruleRt = Node("Rule", root);
            ruleRt.anchorMin = new Vector2(0f, 0f);
            ruleRt.anchorMax = new Vector2(1f, 0f);
            ruleRt.pivot = new Vector2(0.5f, 0f);
            ruleRt.offsetMin = Vector2.zero;
            ruleRt.offsetMax = new Vector2(0f, 1f);
            Le(ruleRt).ignoreLayout = true;
            var rule = Img(ruleRt, null, Tok.Wait);

            var view = root.gameObject.AddComponent<JournalEntryView>();
            view.dot = dot;
            view.sourceGlyph = srcGlyph;
            view.sourceWord = srcWord;
            view.dayLabel = dayLabel;
            view.dayNumber = dayNumber;
            view.entryName = entryName;
            view.paragraphs = body;
            view.paragraphPrefab = Load("UI_JournalParagraph");
            view.rule = rule;

            Save(root.gameObject, "UI_JournalEntry");
        }

        // ---- the Journal -------------------------------------------------------
        // Journal spec, .jr: a 32 chapter rail, 4 in, items on its floor, 30 apart, and no rule
        // under it — the bar's rules are the screen's one strong horizontal. 14 below it the
        // 939 scroll panel, whose entries sit 26 in from either side and 4 down.
        static void Journal()
        {
            var root = Node("UI_Journal", null, 0, 0, W, TrackH);

            var rail = Node("ChapterRail", root, 0, 0, W, 32f);
            var railRow = Node("Items", rail, 4f, 0, W - 8f, 32f);
            var h = railRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30f;
            h.childAlignment = TextAnchor.LowerLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var panel = Panel("Pane", root, 0, 46f, W, 939f, 14);
            var (scroll, content) = Scroll("Scroll", panel, 1f, 1f, W - 2f, 937f);
            Stack(content, 0f, new RectOffset(26, 26, 4, 0));

            var view = root.gameObject.AddComponent<JournalView>();
            view.rail = railRow;
            view.railItemPrefab = Load("UI_ChapterRailItem");
            view.scroll = scroll;
            view.content = content;
            view.entryPrefab = Load("UI_JournalEntry").GetComponent<JournalEntryView>();
            view.endMarkPrefab = Load("UI_EndMark");

            Save(root.gameObject, "UI_Journal");
        }

        // ---- a Focus card ------------------------------------------------------
        // 432 wide in a two-column grid, 16 gutter, padding 14, gap 11. A Focus is an act
        // rather than a place, so it carries a glyph on a tile and not a painted plate —
        // the one structural difference between this menu and Constructs.
        static void LedgerColumn(VerticalLayoutGroup v)
        {
            v.childForceExpandWidth = false;
            v.childAlignment = TextAnchor.UpperLeft;
        }

        static void FocusCard()
        {
            var root = Node("UI_FocusCard", null, 0, 0, 432f, 250f);
            var ground = Img(root, SpriteFactory.Round(13), Tok.Veil, 1f, true);
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(13), Tok.Haze);
            var hover = root.gameObject.AddComponent<UiButton>();

            var tile = Node("IconTile", root, 14f, 14f, 52f, 52f);
            Img(tile, SpriteFactory.Round(11), Tok.IrisL);
            var tileBorder = Img(Stretch(Node("Border", tile)), SpriteFactory.Outline(11), Tok.Haze2);
            var glyph = Img(Node("Glyph", tile, 12f, 12f, 28f, 28f), SpriteFactory.Glyph("Focus", "sift") ?? SpriteFactory.Load("Disc"), Tok.IrisD);

            var name = Txt("Name", root, 77f, 16f, 341f, 26f, "Task", TypeRole.Serif, 20f, Tok.Ink,
                           TextAlignmentOptions.TopLeft);
            var blurb = Prose("Blurb", root, 77f, 42f, 341f, 40f, "", 13.5f, Tok.Ink2, 1.5f, TypeRole.SerifItalic);

            var dot = Dot("Dot", root, 408f, 12f, 10f);

            // Spent & gained: the DepthGauge's double entry, reduced to 402 wide.
            // On a block ground, padding 9 / 11; spent in rose, gained in teal.
            var ledger = Node("Ledger", root, 15f, 92f, 402f, 60f);
            Img(ledger, SpriteFactory.Round(9), Tok.Block);
            // Lines hang 7 to the left of the caption (margin-left: -7) so a refusing line's
            // ground has room without its words moving; each line is only as wide as it says.
            var spent = Node("Spent", ledger, 4f, 9f, 187f, 42f);
            LedgerColumn(Stack(spent, 5f));
            Caps("Caption", spent, 0, 0, 120f, 10f, "SPENT", 8f, Tok.RoseD).margin = new Vector4(7f, 0f, 0f, 0f);
            var rule = Rule("Rule", ledger, 201f, 9f, 1f, Tok.Haze);
            ((RectTransform)rule.transform).sizeDelta = new Vector2(1f, 42f);
            var gained = Node("Gained", ledger, 206f, 9f, 187f, 42f);
            LedgerColumn(Stack(gained, 5f));
            Caps("Caption", gained, 0, 0, 120f, 10f, "GAINED", 8f, Tok.TealD).margin = new Vector4(7f, 0f, 0f, 0f);

            var rate = Txt("Rate", root, 15f, 156f, 402f, 18f, "", TypeRole.SerifItalic, 13.5f, Tok.Ink2,
                           TextAlignmentOptions.MidlineLeft);

            var stepperGo = Nest("UI_Stepper", root, 287f, 178f);
            var stepper = stepperGo.GetComponent<Ursine.UI.Stepper>();
            // A teal Oneiri glyph and "Oneiri bound" in the display serif, level with the stepper.
            Img(Node("OneiriGlyph", root, 15f, 189.5f, 17f, 17f),
                SpriteFactory.Glyph("Resource", "oneiri") ?? SpriteFactory.Load("Disc"), Tok.TealD).raycastTarget = false;
            Txt("OneiriLabel", root, 40f, 186f, 200f, 24f, "Oneiri bound", TypeRole.Serif, 17f, Tok.Ink,
                TextAlignmentOptions.MidlineLeft).raycastTarget = false;

            // The gauge's dive control, full width: 402 x 44. Outlined when the task is not
            // yours; filled iris when it is, with the progress a darker iris so the
            // advancing edge is the darker side on both variants.
            // The button clips its fill to its own rounded corners (overflow: hidden), so the
            // fill's advancing edge is straight and only its left corners are round.
            var action = Node("Action", root, 15f, 224f, 402f, 44f);
            var actionGround = Img(action, SpriteFactory.Round(10), Tok.Iris, 0f, true);
            var progClip = Stretch(Node("ProgressClip", action));
            var clipImg = Img(progClip, SpriteFactory.Round(10), Tok.Veil);
            clipImg.raycastTarget = false;
            progClip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var progress = Node("Progress", progClip, 0, 0, 402f, 44f);
            progress.anchorMin = new Vector2(0f, 0f);
            progress.anchorMax = new Vector2(0f, 1f);
            progress.offsetMin = Vector2.zero;
            progress.offsetMax = Vector2.zero;
            var progressImg = Img(progress, null, Tok.Iris, 0.13f);
            progressImg.raycastTarget = false;
            var actionBorder = Img(Stretch(Node("Border", action)), SpriteFactory.Outline(10), Tok.IrisB);
            actionBorder.raycastTarget = false;
            // Padding 0 / 14, the label in display serif at 18, the arrow (or, when the task
            // is yours, the attention mark) pushed to the right.
            var actionLabel = Txt("Label", action, 14f, 0, 330f, 44f, "Task", TypeRole.Serif, 18f, Tok.IrisD,
                                  TextAlignmentOptions.MidlineLeft);
            actionLabel.raycastTarget = false;
            var arrow = Img(Node("Arrow", action, 402f - 14f - 17f, 16f, 17f, 12f),
                            SpriteFactory.Glyph("Ui", "arrow"), Tok.IrisD);
            arrow.raycastTarget = false;
            var mark = Img(Node("Mark", action, 402f - 14f - 14f, 15f, 14f, 14f),
                           SpriteFactory.Glyph("Ui", "mark"), Tok.Veil);
            mark.raycastTarget = false;
            var actionBtn = action.gameObject.AddComponent<UiButton>();

            var limit = Txt("LimitLine", root, 15f, 270f, 402f, 14f, "", TypeRole.Label400, 11f, Tok.Ink3,
                            TextAlignmentOptions.MidlineLeft);

            // The card is as tall as its content: the limit line ends at 284, plus the
            // card's own 14 of padding. The grid reads this off the prefab.
            At(root, 0, 0, 432f, 298f);

            var view = root.gameObject.AddComponent<FocusCardView>();
            view.cardGround = ground;
            view.cardBorder = border;
            view.iconTile = tile.GetComponent<Image>();
            view.glyph = glyph;
            view.taskName = name;
            view.blurb = blurb;
            view.spentColumn = spent;
            view.gainedColumn = gained;
            view.ledgerLinePrefab = Load("UI_LedgerLine");
            view.rateLine = rate;
            view.stepper = stepper;
            view.action = actionBtn;
            view.actionGround = actionGround;
            view.actionLabel = actionLabel;
            view.actionProgress = progress;
            view.actionFill = progressImg;
            view.actionBorder = actionBorder;
            view.actionArrow = arrow;
            view.actionMark = mark;
            view.tileBorder = tileBorder;
            view.ledgerRule = rule;
            view.ledger = ledger;
            view.dot = dot;
            view.limitLine = limit;

            Save(root.gameObject, "UI_FocusCard");
        }

        // ---- Focus -------------------------------------------------------------
        // The first menu long enough to overrun the column's 985. It scrolls within the
        // center track; the bar above it does not move.
        static void Focus()
        {
            var root = Node("UI_Focus", null, 0, 0, W, TrackH);

            // The pool line states the Oneiri total once for the whole menu, so no card has
            // to explain why its plus is gray.
            var pool = Node("PoolLine", root, 0, 0, W, 44f);
            Img(pool, SpriteFactory.Round(12), Tok.Block);
            var poolText = Txt("Text", pool, 16f, 0, W - 32f, 44f, "", TypeRole.Label500, 13f, Tok.Ink2,
                               TextAlignmentOptions.MidlineLeft);

            var (scroll, content) = Scroll("Scroll", root, 0, 52f, W, TrackH - 52f);
            Stack(content, 0f, new RectOffset(0, 0, 0, 40));

            var view = root.gameObject.AddComponent<FocusView>();
            view.poolLine = poolText;
            view.content = content;
            view.sectionHeadPrefab = Load("UI_SectionHead");
            view.cardPrefab = Load("UI_FocusCard").GetComponent<FocusCardView>();

            Save(root.gameObject, "UI_Focus");
        }
    }
}
