// What Lies In The Depths — the leaf prefabs every surface reuses.
// Namespace per component from the start, whatever the target: the brief names this as the
// one trap worth naming, because generic names collided silently in the composite.
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
    public static class BuildWidgets
    {
        public static void All()
        {
            BuildResourceRow();
            GroupCaption();
            SectionHead();
            BuildCostPill();
            EffectRow();
            LedgerLine();
            Paragraph();
            JournalParagraph();
            RuledBlock();
            BuildStepper();
            BarItemDestination();
            BarItemUtility();
            ChapterRailItem();
            EndMark();
            OfferRow();
        }

        // ---- the ledger row ----------------------------------------------------
        // 450 wide, height 28 — the floor, below which the mono numerals crowd their
        // baseline. Padding 0 / 12, no separators: the fill is what separates rows.
        static void BuildResourceRow()
        {
            var root = Node("UI_ResourceRow", null, 0, 0, Layout.SideColumnW, 28f);

            var fillHolder = Node("Fill", root, 0, 0, Layout.SideColumnW, 28f);
            var fill = Node("Bar", fillHolder, 0, 0, 0, 28f);
            var fillImg = Img(fill, null, Tok.IrisL);
            var fb = root.gameObject.AddComponent<FillBar>();
            Dress(fb);
            fb.fill = fill;
            fb.fillImage = fillImg;

            var icon = Node("Icon", root, 12f, 6.5f, 15f, 15f);
            var iconImg = Img(icon, SpriteFactory.Glyph("Resource", "reverie") ?? SpriteFactory.Load("Disc"), Tok.IrisD);

            var name = Txt("Name", root, 35f, 0, 174f, 28f, "Resource", TypeRole.Label500, 12.5f, Tok.Ink,
                           TextAlignmentOptions.MidlineLeft);
            name.overflowMode = TextOverflowModes.Ellipsis;

            var held = Figures("Held", root, 219f, 0, 70f, 28f, "0", TypeRole.Mono500, 12.5f, Tok.Ink,
                               TextAlignmentOptions.MidlineRight);
            var div = Figures("Divider", root, 289f, 0, 9f, 28f, "/", TypeRole.Mono400, 11f, Tok.Ink4,
                              TextAlignmentOptions.Midline);
            var max = Figures("Maximum", root, 298f, 0, 62f, 28f, "0", TypeRole.Mono400, 11.5f, Tok.Ink3,
                              TextAlignmentOptions.MidlineLeft);
            var rate = Figures("Rate", root, 360f, 0, 76f, 28f, "0.0 /s", TypeRole.Mono500, 11.5f, Tok.Ink3,
                               TextAlignmentOptions.MidlineRight);

            var row = root.gameObject.AddComponent<ResourceRow>();
            row.fill = fb;
            row.icon = iconImg;
            row.nameLabel = name;
            row.held = held;
            row.divider = div;
            row.maximum = max;
            row.rate = rate;

            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.minHeight = 28f;

            Save(root.gameObject, "UI_ResourceRow");
        }

        // Karla 700, 9px, .16em, upper, ink3, with a 1px trailing rule. Padding 9 / 12 / 4.
        static void GroupCaption()
        {
            var root = Node("UI_GroupCaption", null, 0, 0, Layout.SideColumnW, 22f);
            Caps("Label", root, 12f, 9f, 200f, 10f, "GATHERED", 9f, Tok.Ink3);
            Rule("Rule", root, 12f, 21f, Layout.SideColumnW - 24f, Tok.Haze2);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;
            Save(root.gameObject, "UI_GroupCaption");
        }

        // The same head a menu section gets: 16 above, 8 below.
        static void SectionHead()
        {
            var root = Node("UI_SectionHead", null, 0, 0, Layout.CenterColumnW, 33f);
            Caps("Label", root, 0, 16f, 300f, 10f, "SECTION", 9f, Tok.Ink3);
            Rule("Rule", root, 0, 32f, Layout.CenterColumnW, Tok.Haze2);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 33f;
            Save(root.gameObject, "UI_SectionHead");
        }

        // A 12px resource glyph, a mono amount, a resource name. Transparent ground when
        // payable; rose when short; gold when above a ceiling.
        // Padding 3 / 8, radius 7, gap 5, 11.5px; a 1px border that is only there when the
        // pill refuses. It sizes to what it says: a glyph (where the surface wants one), the
        // amount in mono, the resource name.
        static void BuildCostPill()
        {
            var root = Node("UI_CostPill", null, 0, 0, 100f, 22f);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 3, 3);
            row.spacing = 5f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var fit = root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var ground = Img(Stretch(Node("Ground", root)), SpriteFactory.Round(7), Tok.Block, 0f);
            ground.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(7), Tok.Haze, 0f);
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var glyph = Img(Node("Glyph", root, 0, 0, 12f, 12f),
                            SpriteFactory.Glyph("Resource", "reverie") ?? SpriteFactory.Load("Disc"), Tok.Ink2);
            var gle = glyph.gameObject.AddComponent<LayoutElement>();
            gle.preferredWidth = gle.preferredHeight = 12f;

            var amount = Figures("Amount", root, 0, 0, 40f, 16f, "0", TypeRole.Mono400, 11.5f, Tok.Ink2,
                                 TextAlignmentOptions.MidlineLeft);
            var label = Txt("Label", root, 0, 0, 56f, 16f, "Resource", TypeRole.Label400, 11.5f, Tok.Ink2,
                            TextAlignmentOptions.MidlineLeft);
            foreach (var t in new[] { amount, label })
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
            }

            var pill = root.gameObject.AddComponent<CostPill>();
            Dress(pill);
            pill.ground = ground;
            pill.border = border;
            pill.glyph = glyph;
            pill.amount = amount;
            pill.label = label;
            Save(root.gameObject, "UI_CostPill");
        }

        // Rows Karla 12px prose with the changed value in mono teal.
        static void EffectRow()
        {
            var root = Node("UI_EffectRow", null, 0, 0, 402f, 17f);
            Txt("Text", root, 0, 0, 402f, 17f, "+0 effect", TypeRole.Label400, 12f, Tok.Prose,
                TextAlignmentOptions.MidlineLeft);
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 17f;
            Save(root.gameObject, "UI_EffectRow");
        }

        // One line of the double entry: rose spent on the left, teal brought up on the right.
        // One line of a double-entry ledger, inline: an optional 12px glyph, the signed amount
        // in mono 500, the resource name in prose; 6 between. The glyph is off by default —
        // the gauge names its resources in words alone; Focus turns it on.
        static void LedgerLine()
        {
            // Padding 2 / 7 and a 1px border, radius 7: a line that refuses (short of the
            // resource, or a gain above its ceiling) wears a rose or gold ground. Payable
            // lines keep the padding with no ground, so nothing shifts when one turns.
            var root = Node("UI_LedgerLine", null, 0, 0, 190f, 21f);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(7, 7, 2, 2);
            row.spacing = 6f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var ground = Img(Stretch(Node("Ground", root)), SpriteFactory.Round(7), Tok.RoseL, 0f);
            ground.raycastTarget = false;
            ground.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(7), Tok.RoseB, 0f);
            border.raycastTarget = false;
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var glyph = Img(Node("Glyph", root, 0, 0, 12f, 12f),
                            SpriteFactory.Glyph("Resource", "reverie") ?? SpriteFactory.Load("Disc"), Tok.Prose, 0.8f);
            glyph.raycastTarget = false;
            var gle = glyph.gameObject.AddComponent<LayoutElement>();
            gle.preferredWidth = gle.preferredHeight = 12f;
            glyph.gameObject.SetActive(false);

            var amount = Figures("Amount", root, 0, 0, 40f, 17f, "0", TypeRole.Mono500, 12f, Tok.Ink2,
                                 TextAlignmentOptions.MidlineLeft);
            var label = Txt("Label", root, 0, 0, 100f, 17f, "Resource", TypeRole.Label400, 12f, Tok.Prose,
                            TextAlignmentOptions.MidlineLeft);
            foreach (var t in new[] { amount, label })
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
            }
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 21f;
            Save(root.gameObject, "UI_LedgerLine");
        }

        // Cormorant 15.5 / 1.62 on prose; the newest one a step darker. 22 between paragraphs.
        static void Paragraph()
        {
            // The text sits on the root: a wrapper with the text stretched inside it reports
            // no height of its own, and every paragraph came out 20 tall and overlapping.
            var root = Node("UI_Paragraph", null, 0, 0, 400f, 60f);
            var t = root.gameObject.AddComponent<TextMeshProUGUI>();
            Typeset.Set(t, TypeRole.Serif, 15.5f, (int)Tok.Prose);
            Typeset.Wrap(t);
            Typeset.Leading(t, 1.62f);
            t.alignment = TextAlignmentOptions.TopLeft;
            Save(root.gameObject, "UI_Paragraph");
        }

        // Ruled line-blocks, 1px wait, 25 apart, the last line short. One block per
        // unrevealed paragraph, so the page reads as having room left.
        static void RuledBlock()
        {
            var root = Node("UI_RuledBlock", null, 0, 0, 400f, 97f);
            for (int i = 0; i < 4; i++)
            {
                float w = i == 3 ? 240f : 400f;
                Rule("Line" + i, root, 0, 22f + i * 25f, w, Tok.Wait);
            }
            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 97f;
            Save(root.gameObject, "UI_RuledBlock");
        }

        // 130 x 40, 38px buttons, count mono 500 20px, cap mono 12px behind a slash.
        static void BuildStepper()
        {
            var root = Node("UI_Stepper", null, 0, 0, 130f, 40f);
            // The spec's #fff between iris-l buttons: artwork-white, not the veil token.
            Artwork(root, SpriteFactory.Round(10));
            Img(Stretch(Node("Border", root)), SpriteFactory.Outline(10), Tok.Haze);

            var minusRt = Node("Minus", root, 1f, 1f, 38f, 38f);
            var minusGround = Img(minusRt, SpriteFactory.Round(9), Tok.IrisL, 1f, true);
            Txt("Sign", minusRt, 0, 0, 38f, 38f, "−", TypeRole.Mono500, 16f, Tok.IrisD, TextAlignmentOptions.Center);
            var minus = minusRt.gameObject.AddComponent<UiButton>();

            var plusRt = Node("Plus", root, 91f, 1f, 38f, 38f);
            var plusGround = Img(plusRt, SpriteFactory.Round(9), Tok.IrisL, 1f, true);
            Txt("Sign", plusRt, 0, 0, 38f, 38f, "+", TypeRole.Mono500, 16f, Tok.IrisD, TextAlignmentOptions.Center);
            var plus = plusRt.gameObject.AddComponent<UiButton>();

            // The count and its cap sit together, centered between the two buttons, 3 apart
            // (min-width 52, justify-content: center) — the pair moves as one, so "3/4" and
            // "12/15" are both centered rather than pinned to one side.
            var number = Node("Number", root, 39f, 0, 52f, 40f);
            var row = number.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 3f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            var count = Figures("Count", number, 0, 0, 20f, 40f, "0", TypeRole.Mono500, 20f, Tok.Ink,
                                TextAlignmentOptions.Midline);
            var cap = Figures("Cap", number, 0, 0, 18f, 40f, "/0", TypeRole.Mono400, 12f, Tok.Ink3,
                              TextAlignmentOptions.Midline);
            foreach (var t in new[] { count, cap })
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
            }

            var st = root.gameObject.AddComponent<Ursine.UI.Stepper>();
            Dress(st);
            st.minus = minus;
            st.plus = plus;
            st.minusGround = minusGround;
            st.plusGround = plusGround;
            st.count = count;
            st.cap = cap;

            Save(root.gameObject, "UI_Stepper");
        }

        // Cormorant Garamond 600, 19px, with a 16px glyph before it, 11 above the rule.
        // The open marker is 2px iris at -1, so it covers the rule rather than resting on it.
        static void BarItemDestination()
        {
            var root = Node("UI_BarItem_Destination", null, 0, 0, 120f, Layout.BarH);
            var hit = Stretch(Node("Hit", root));
            var btn = Button(hit);

            var glyph = Img(Node("Glyph", root, 0, 5f, 16f, 16f), SpriteFactory.Glyph("Tab", "focus") ?? SpriteFactory.Load("Disc"), Tok.Ink3);
            var label = Txt("Label", root, 24f, 0, 90f, 20f, "Destination", TypeRole.Serif, 19f, Tok.Ink3,
                            TextAlignmentOptions.BottomLeft);
            ((RectTransform)label.transform).anchoredPosition = new Vector2(24f, -1f);
            ((RectTransform)label.transform).sizeDelta = new Vector2(90f, 19f);

            var underline = Node("Underline", root, 0, Layout.BarH, 114f, 2f);
            Img(underline, SpriteFactory.Round(999), Tok.Iris);
            underline.anchoredPosition = new Vector2(0f, -(Layout.BarH - 1f));

            var dot = Dot("Dot", root, 118f, 8f, 7f);

            var item = root.gameObject.AddComponent<BarItem>();
            item.kind = BarItemKind.Destination;
            item.button = btn;
            item.label = label;
            item.glyph = glyph;
            item.underline = underline;
            item.dot = dot;

            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 120f;
            Save(root.gameObject, "UI_BarItem_Destination");
        }

        // Karla 700, 11px, .16em, upper, with a 14px glyph, 12 above the rule.
        static void BarItemUtility()
        {
            var root = Node("UI_BarItem_Utility", null, 0, 0, 96f, Layout.BarH);
            var hit = Stretch(Node("Hit", root));
            var btn = Button(hit);

            var glyph = Img(Node("Glyph", root, 0, 6f, 14f, 14f), SpriteFactory.Glyph("Tab", "options") ?? SpriteFactory.Load("Disc"), Tok.Ink3);
            var label = Caps("Label", root, 20f, 7f, 62f, 12f, "UTILITY", 11f, Tok.Ink3);
            // The 8px outward arrow — the only promise the player gets that they are leaving,
            // and there are no tooltips to make it with.
            var arrow = Img(Node("Outward", root, 84f, 9f, 8f, 8f),
                            SpriteFactory.Glyph("Tab", "outward") ?? SpriteFactory.Load("Disc"), Tok.Ink4);

            var underline = Node("Underline", root, 0, 0, 88f, 2f);
            Img(underline, SpriteFactory.Round(999), Tok.Iris);
            underline.anchoredPosition = new Vector2(0f, -(Layout.BarH - 1f));

            var item = root.gameObject.AddComponent<BarItem>();
            item.kind = BarItemKind.Utility;
            item.button = btn;
            item.label = label;
            item.glyph = glyph;
            item.underline = underline;
            item.outwardArrow = arrow;

            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 96f;
            Save(root.gameObject, "UI_BarItem_Utility");
        }

        // Journal spec, .jchaps button: the Roman numeral first (mono 500 10, .08em, ink-4,
        // iris-d when open), 8 on, then the name (Cormorant 600 17, line-height 1), baseline to
        // baseline; 1 of side padding, 10 under the words, a 2px iris underline across the
        // whole button's foot, and a 6px unread dot centered on the words when there is one.
        // The rail is 32 tall with its items sitting on its floor, so the words' baseline
        // lands at 19 and the underline at 30.
        static void ChapterRailItem()
        {
            const float H = 32f, Baseline = 19f;
            var root = Node("UI_ChapterRailItem", null, 0, 0, 120f, H);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.padding = new RectOffset(1, 1, 0, 0);
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var fit = root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hit = Stretch(Node("Hit", root));
            hit.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var btn = Button(hit);

            // Both texts are 2 x Baseline tall and set on their baseline, so TMP puts each
            // one's baseline at the same height whatever the size.
            var numeral = Figures("Numeral", root, 0, 0, 20f, Baseline * 2f, "I", TypeRole.Mono500, 10f, Tok.Ink4,
                                  TextAlignmentOptions.BaselineLeft);
            numeral.characterSpacing = 8f;
            var label = Txt("Label", root, 0, 0, 100f, Baseline * 2f, "Chapter", TypeRole.Serif, 17f, Tok.Ink3,
                            TextAlignmentOptions.BaselineLeft);

            // align-self:center on a button whose content box is the 17px line from 5 to 22.
            var dotRoot = Node("Dot", root, 0, 0, 6f, H);
            Img(Node("Disc", dotRoot, 0, 10.5f, 6f, 6f), SpriteFactory.Load("Disc"), Tok.Iris);
            Img(Node("Halo", dotRoot, -3f, 7.5f, 12f, 12f), SpriteFactory.Load("Disc"), Tok.Iris, 0.16f)
                .transform.SetAsFirstSibling();
            var dot = dotRoot.gameObject.AddComponent<AttentionDot>();
            dot.root = dotRoot.gameObject;

            var underline = Node("Underline", root);
            underline.anchorMin = new Vector2(0f, 0f);
            underline.anchorMax = new Vector2(1f, 0f);
            underline.pivot = new Vector2(0.5f, 0f);
            underline.offsetMin = new Vector2(0f, 0f);
            underline.offsetMax = new Vector2(0f, 2f);
            underline.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Img(underline, SpriteFactory.Round(999), Tok.Iris);

            var item = root.gameObject.AddComponent<BarItem>();
            item.kind = BarItemKind.Destination;
            item.button = btn;
            item.label = label;
            item.numeral = numeral;
            item.underline = underline;
            item.dot = dot;

            Save(root.gameObject, "UI_ChapterRailItem");
        }

        // Journal spec, .jend: a haze-2 hairline either side of one italic line, 14 between,
        // 26 above and 34 below. Under the first chapter only.
        static void EndMark()
        {
            var root = Node("UI_EndMark", null, 0, 0, Layout.CenterColumnW, 77f);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 14f;
            row.padding = new RectOffset(0, 0, 26, 34);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var l = Rule("RuleLeft", root, 0, 0, 10f, Tok.Haze2);
            var le = l.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 1f;
            var t = Txt("Line", root, 0, 0, 140f, 17f, "The dream begins", TypeRole.SerifItalic, 14f, Tok.Ink3,
                        TextAlignmentOptions.Center);
            Typeset.Leading(t, 1.2f);
            var r = Rule("RuleRight", root, 0, 0, 10f, Tok.Haze2);
            le = r.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 1f;

            Save(root.gameObject, "UI_EndMark");
        }

        // Journal spec, .jbody p: Cormorant 16.5 / 1.74 in prose. The text sits on the root,
        // so the entry's layout reads its wrapped height directly.
        static void JournalParagraph()
        {
            var root = Node("UI_JournalParagraph", null, 0, 0, 660f, 58f);
            var t = root.gameObject.AddComponent<TextMeshProUGUI>();
            Typeset.Set(t, TypeRole.Serif, 16.5f, (int)Tok.Prose);
            Typeset.Wrap(t);
            Typeset.Leading(t, 1.74f);
            t.alignment = TextAlignmentOptions.TopLeft;
            Save(root.gameObject, "UI_JournalParagraph");
        }

        // One of two or three named resources, each buying a percentage.
        // One offer in a Vision's readout: a radio pip, the amount (glyph, mono figure, name)
        // and what it buys in teal mono, pushed right. Padding 7 / 10, radius 9, a 1px border,
        // a white ground. Selected, short and golden are painted by the view.
        static void OfferRow()
        {
            var root = Node("UI_OfferRow", null, 0, 0, 235f, 32f);
            Img(root, SpriteFactory.Round(9), Tok.Veil, 1f, true);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 10, 7, 7);
            row.spacing = 9f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            root.gameObject.AddComponent<UiButton>();

            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(9), Tok.Haze2);
            border.raycastTarget = false;
            border.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var pip = Node("Pip", root, 0, 0, 12f, 12f);
            Img(pip, SpriteFactory.Load("Ring_Pip"), Tok.Ink4).raycastTarget = false;
            var ple = pip.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = ple.preferredHeight = 12f;
            var dot = Node("Dot", pip, 3.5f, 3.5f, 5f, 5f);
            Img(dot, SpriteFactory.Load("Disc"), Tok.Iris).raycastTarget = false;

            var glyph = Img(Node("Glyph", root, 0, 0, 12f, 12f),
                            SpriteFactory.Glyph("Resource", "silt") ?? SpriteFactory.Load("Disc"), Tok.Prose, 0.8f);
            glyph.raycastTarget = false;
            var gle = glyph.gameObject.AddComponent<LayoutElement>();
            gle.preferredWidth = gle.preferredHeight = 12f;

            var amount = Figures("Amount", root, 0, 0, 40f, 18f, "0", TypeRole.Mono500, 12.5f, Tok.Prose,
                                 TextAlignmentOptions.MidlineLeft);
            var resource = Txt("Resource", root, 0, 0, 90f, 18f, "Resource", TypeRole.Label400, 12.5f, Tok.Prose,
                               TextAlignmentOptions.MidlineLeft);
            var gain = Figures("Gain", root, 0, 0, 40f, 18f, "+1%", TypeRole.Mono500, 12f, Tok.TealD,
                               TextAlignmentOptions.MidlineRight);
            foreach (var t in new[] { amount, resource, gain })
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.raycastTarget = false;
            }
            // margin-left: auto on the gain
            resource.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var le = root.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;
            Save(root.gameObject, "UI_OfferRow");
        }
    }
}
