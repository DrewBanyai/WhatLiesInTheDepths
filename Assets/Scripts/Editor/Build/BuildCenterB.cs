// What Lies In The Depths — center destinations: Revelations, Constructs, Visions, Assault.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static WhatLiesInTheDepths.EditorTools.UiFactory;
using ThemedGraphic = Ursine.Theming.ThemedGraphic;
using Ursine.Text;
using Ursine.UI;
using Ursine.Combat;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class BuildCenterB
    {
        const float W = Layout.CenterColumnW;
        const float TrackH = Layout.PanelTrackH;

        public static void All()
        {
            Sigil();
            Revelations();
            ConstructCard();
            PalaceBuilding();
            Constructs();
            VisionMark();
            Visions();
            UnitCard();
            RoadPin();
            Assault();
        }

        /// <summary>A field's light: radial-gradient(#F7F2FC → 0), which is veil at .5 over
        /// the field's ground, reaching (rx, ry) from (cx, cy) in top-left coordinates.</summary>
        static void FieldBloom(RectTransform field, float cx, float cy, float rx, float ry)
        {
            var bloom = Node("Bloom", field, cx - rx, cy - ry, rx * 2f, ry * 2f);
            var img = Img(bloom, SpriteFactory.Load("Bloom_Field"), Tok.Veil, 0.5f);
            img.raycastTarget = false;
        }

        // ---- Revelations -------------------------------------------------------
        // A 36px glyph in a 62px hit circle, 1.45 stroke on a 24x24 grid, with a soft halo.
        static void Sigil()
        {
            var root = Node("UI_Sigil", null, 0, 0, 62f, 62f);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);

            var group = root.gameObject.AddComponent<CanvasGroup>();
            // The halo is a lilac (or gold) light, not a surface, so it carries no token.
            var halo = Artwork(Stretch(Node("Halo", root)), SpriteFactory.Load("Halo_Sigil"));
            halo.raycastTarget = false;
            // 1px, inset 6: hidden and at .88 until hovered; a greater one's always shows at .55.
            var ring = Img(Stretch(Node("Ring", root), 6f, 6f, 6f, 6f), SpriteFactory.Load("Ring_Sigil"), Tok.IrisB);
            ring.raycastTarget = false;
            var glyphRt = Node("Glyph", root, 0, 0, 36f, 36f);
            glyphRt.anchorMin = glyphRt.anchorMax = new Vector2(0.5f, 0.5f);
            glyphRt.pivot = new Vector2(0.5f, 0.5f);
            glyphRt.anchoredPosition = Vector2.zero;
            var glyph = Img(glyphRt, SpriteFactory.Glyph("Sigil", "lantern") ?? SpriteFactory.Load("Disc"), Tok.IrisD);

            var hit = Stretch(Node("Hit", root));
            var btn = Button(hit);

            var view = root.gameObject.AddComponent<SigilView>();
            view.button = btn;
            view.glyph = glyph;
            view.halo = halo;
            view.greaterRing = ring;
            view.group = group;

            Save(root.gameObject, "UI_Sigil");
        }

        // Field 880 x 720, radius 16, 1px haze2, on a soft radial bloom. Sits 16 below the
        // bar, leaving 249 of the column's track spare. It does not scroll; its field is fixed.
        static void Revelations()
        {
            var root = Node("UI_Revelations", null, 0, 0, W, TrackH);

            var field = Node("Field", root, 0, 0, W, 720f);
            // The field's own graphic is the hit area, so empty field counts as inside it,
            // and everything that drifts over it is a descendant: uGUI sends the field its
            // exit only when the pointer leaves the whole subtree.
            // #F4F0F9 on the page's mist is veil at .2 over it — so it follows the palette.
            Img(field, SpriteFactory.Round(16), Tok.Veil, 0.2f, true);
            var fieldHover = field.gameObject.AddComponent<UiButton>();
            FieldBloom(field, 440f, 720f * 0.46f, 880f * 0.58f * 0.72f, 720f * 0.58f * 0.72f);
            Img(Stretch(Node("Border", field)), SpriteFactory.Outline(16), Tok.Haze2);

            var center = Node("Center", field, W * 0.5f, 360f, 0f, 0f);
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.anchoredPosition = Vector2.zero;

            var massRt = Node("Mass", center, 0, 0, 480f, 480f);
            massRt.anchorMin = massRt.anchorMax = new Vector2(0.5f, 0.5f);
            massRt.pivot = new Vector2(0.5f, 0.5f);
            massRt.anchoredPosition = Vector2.zero;
            var massGroup = massRt.gameObject.AddComponent<CanvasGroup>();
            var massImg = Img(massRt, SpriteFactory.Load("Art_Mass"), Tok.Veil);
            massImg.color = Color.white;
            var massThemed = massImg.GetComponent<ThemedGraphic>();
            if (massThemed != null) Object.DestroyImmediate(massThemed);

            // Five places inside the mass, in the spec's 360 viewBox (the mass draws 380px
            // scaled 1.26, so 480/360 px a unit); each a 24-grid sigil at .92.
            const float MassUnit = 480f / 360f;
            float[,] spots = { { 142, 150 }, { 218, 150 }, { 180, 198 }, { 150, 106 }, { 210, 106 } };
            var absorbed = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                float size = 24f * 0.92f * MassUnit;
                var spot = Node("Absorbed" + i, massRt, 0, 0, size, size);
                spot.anchorMin = spot.anchorMax = new Vector2(0.5f, 0.5f);
                spot.pivot = new Vector2(0.5f, 0.5f);
                spot.anchoredPosition = new Vector2((spots[i, 0] - 180f) * MassUnit,
                                                    -(spots[i, 1] - 180f) * MassUnit);
                absorbed[i] = Artwork(spot, SpriteFactory.Glyph("Sigil", "lantern"));
                absorbed[i].raycastTarget = false;
            }

            // Italic 11px ink4, 12 above the field's foot; the field is the only thing it names.
            var caption = Txt("Caption", field, 0f, 720f - 12f - 16f, W, 16f, "nine realizations within reach",
                              TypeRole.Label400, 11f, Tok.Ink4, TextAlignmentOptions.Bottom);
            caption.fontStyle = FontStyles.Italic;
            caption.raycastTarget = false;
            caption.transform.SetSiblingIndex(center.GetSiblingIndex());   // under what drifts

            var sigilLayer = Node("Sigils", center, 0, 0, 0f, 0f);
            sigilLayer.anchorMin = sigilLayer.anchorMax = new Vector2(0.5f, 0.5f);
            sigilLayer.pivot = new Vector2(0.5f, 0.5f);
            sigilLayer.anchoredPosition = Vector2.zero;

            // Readout: 600 x 300, dead center, one fixed position. It never moves.
            var readout = Node("Readout", center, 0, 0, 600f, 300f);
            readout.anchorMin = readout.anchorMax = new Vector2(0.5f, 0.5f);
            readout.pivot = new Vector2(0.5f, 0.5f);
            readout.anchoredPosition = Vector2.zero;
            // The root is only a hit area; its children draw it, shadow under ground.
            Img(readout, null, Tok.Veil, 0f, true);
            var readoutHover = readout.gameObject.AddComponent<UiButton>();
            // box-shadow 0 22px 48px -28px: the core is the panel shrunk by 28 and dropped
            // 22, and the falloff reaches 48 past that core.
            var readoutShadow = Img(Stretch(Node("Shadow", readout), -20f, 2f, -20f, -42f),
                                    SpriteFactory.Load("Shadow_Readout"), Tok.Ink, 0.45f);
            readoutShadow.raycastTarget = false;
            // The ground clips what it holds (overflow: hidden), so the mark panel takes the
            // panel's rounded left corners and none of its own on the right.
            var readoutGround = Img(Stretch(Node("Ground", readout)), SpriteFactory.Round(14), Tok.Veil);
            readoutGround.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var readoutBorder = Img(Stretch(Node("Border", readout)), SpriteFactory.Outline(14), Tok.Haze);
            readoutBorder.raycastTarget = false;

            var markPanel = Node("MarkPanel", readoutGround.transform, 0, 0, 132f, 300f);
            var markPanelImg = Img(markPanel, null, Tok.IrisL);
            var markRule = Img(Node("Rule", markPanel, 131f, 0f, 1f, 300f), null, Tok.Haze2);
            var markGlyph = Img(Node("Glyph", markPanel, 35f, 119f, 62f, 62f),
                                SpriteFactory.Glyph("Sigil", "lantern") ?? SpriteFactory.Load("Disc"), Tok.IrisD);

            // The body flows: padding 20 / 22 / 18, gap 11, the foot pushed to the bottom.
            var contents = Node("Contents", readoutGround.transform, 132f, 0, 468f, 300f);
            var contentsGroup = contents.gameObject.AddComponent<CanvasGroup>();
            var body = contents.gameObject.AddComponent<VerticalLayoutGroup>();
            body.padding = new RectOffset(22, 22, 20, 18);
            body.spacing = 11f;
            body.childControlWidth = body.childControlHeight = true;
            body.childForceExpandWidth = true;
            body.childForceExpandHeight = false;

            var head = Node("Head", contents, 0, 0, 424f, 40f);
            var headStack = head.gameObject.AddComponent<VerticalLayoutGroup>();
            headStack.spacing = 5f;
            headStack.childControlWidth = headStack.childControlHeight = true;
            headStack.childForceExpandWidth = true;
            headStack.childForceExpandHeight = false;
            var kind = Caps("Kind", head, 0, 0, 424f, 10f, "KIND", 8.5f, Tok.IrisD);
            var name = Txt("Name", head, 0, 0, 424f, 29f, "Revelation", TypeRole.Serif, 27f, Tok.Ink);
            name.lineSpacing = 0f;

            var realization = Prose("Realization", contents, 0, 0, 424f, 23f, "", 14.5f, Tok.Ink2, 1.55f,
                                    TypeRole.SerifItalic);

            var effects = Node("Effects", contents, 0, 0, 424f, 40f);
            Img(effects, SpriteFactory.Round(9), Tok.Block);
            var effectsStack = effects;
            var fxStack = effects.gameObject.AddComponent<VerticalLayoutGroup>();
            fxStack.padding = new RectOffset(12, 12, 10, 10);
            fxStack.spacing = 4f;
            fxStack.childControlWidth = fxStack.childControlHeight = true;
            fxStack.childForceExpandWidth = true;
            fxStack.childForceExpandHeight = false;

            // margin-top: auto
            Node("Spacer", contents, 0, 0, 424f, 0f).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var foot = Node("Foot", contents, 0, 0, 424f, 48f);
            var footRow = foot.gameObject.AddComponent<HorizontalLayoutGroup>();
            footRow.padding = new RectOffset(0, 0, 12, 0);
            footRow.spacing = 10f;
            footRow.childAlignment = TextAnchor.MiddleLeft;
            footRow.childControlWidth = footRow.childControlHeight = true;
            footRow.childForceExpandWidth = footRow.childForceExpandHeight = false;
            var footRule = Img(Node("Rule", foot, 0, 0, 424f, 1f), null, Tok.Haze2);
            var ruleRt = footRule.rectTransform;
            ruleRt.anchorMin = new Vector2(0f, 1f); ruleRt.anchorMax = new Vector2(1f, 1f);
            ruleRt.pivot = new Vector2(0.5f, 1f);
            ruleRt.offsetMin = new Vector2(0f, -1f); ruleRt.offsetMax = Vector2.zero;
            footRule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var costCol = Node("CostColumn", foot, 0, 0, 300f, 26f);
            var costColStack = costCol.gameObject.AddComponent<VerticalLayoutGroup>();
            costColStack.spacing = 3f;
            costColStack.childControlWidth = costColStack.childControlHeight = true;
            costColStack.childForceExpandWidth = true;
            costColStack.childForceExpandHeight = false;
            costCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var costs = Node("Costs", costCol, 0, 0, 300f, 22f);
            var costRow = costs.gameObject.AddComponent<HorizontalLayoutGroup>();
            costRow.spacing = 6f;
            costRow.childControlWidth = costRow.childControlHeight = true;
            costRow.childForceExpandWidth = costRow.childForceExpandHeight = false;

            var reason = Txt("ReasonLine", costCol, 0, 0, 300f, 15f, "", TypeRole.Label400, 11f, Tok.Ink3);
            reason.fontStyle = FontStyles.Italic;
            reason.margin = new Vector4(0f, 2f, 0f, 0f);

            // Height 36, padding 0 / 18, radius 9, serif 600 at 16.
            var realize = PrimaryButton("Realize", foot, 0, 0, 88f, 36f, "Realize", 16f);
            var realizeSize = realize.root.gameObject.AddComponent<LayoutElement>();
            realizeSize.preferredWidth = 88f;
            realizeSize.preferredHeight = 36f;

            var view = root.gameObject.AddComponent<RevelationsView>();
            view.field = field;
            view.mass = massRt;
            view.massGroup = massGroup;
            view.fieldCaption = caption;
            view.absorbedSlots = absorbed;
            view.sigilLayer = sigilLayer;
            view.sigilPrefab = Load("UI_Sigil").GetComponent<SigilView>();
            view.readout = readout.gameObject;
            view.readoutGround = readoutGround;
            view.readoutBorder = readoutBorder;
            view.readoutShadow = readoutShadow;
            view.markRule = markRule;
            view.markPanel = markPanelImg;
            view.markGlyph = markGlyph;
            view.kindCaption = kind;
            view.revelationName = name;
            view.realization = realization;
            view.effects = effectsStack;
            view.effectRowPrefab = Load("UI_EffectRow");
            view.costs = costs;
            view.costPillPrefab = Load("UI_CostPill").GetComponent<CostPill>();
            view.realize = realize.button;
            view.realizeGround = realize.ground;
            view.realizeLabel = realize.label;
            view.reasonLine = reason;
            view.readoutContents = contentsGroup;
            view.fieldHover = fieldHover;
            view.readoutHover = readoutHover;

            Save(root.gameObject, "UI_Revelations");
        }

        // ---- Constructs --------------------------------------------------------
        // Card 432, plate 430 x 150 at 2.87 : 1, body padding 13 / 14 / 14, gap 9.
        static void ConstructCard()
        {
            var root = Node("UI_ConstructCard", null, 0, 0, 432f, 330f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            // The root is only the hit area. The 4px iris ring sits OUTSIDE the card, so it
            // has to be drawn before the ground — as a child of a root that drew the ground it
            // would wash the whole card iris instead of ringing it.
            Img(root, null, Tok.Veil, 0f, true);
            var ring = Img(Stretch(Node("Ring", root), -4, -4, -4, -4), SpriteFactory.Round(16), Tok.Iris, 0f);
            var ground = Img(Stretch(Node("Ground", root)), SpriteFactory.Round(13), Tok.Veil);
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(13), Tok.Haze);
            var hover = root.gameObject.AddComponent<UiButton>();

            var plateHolder = Node("Plate", root, 1f, 1f, 430f, 150f);
            plateHolder.gameObject.AddComponent<RectMask2D>();
            var plate = Img(Stretch(Node("Art", plateHolder)), SpriteFactory.Load("Plate_Construct_hut"), Tok.Veil);
            plate.color = Color.white;
            var pt = plate.GetComponent<ThemedGraphic>();
            if (pt != null) Object.DestroyImmediate(pt);

            // The owned count overlaid on the plate, top right, 10 inset.
            var pill = Node("OwnedPill", plateHolder, 340f, 10f, 80f, 22f);
            Img(pill, SpriteFactory.Round(999), Tok.Veil, 0.9f);
            Img(Stretch(Node("Border", pill)), SpriteFactory.Outline(999), Tok.Haze);
            var owned = Figures("Count", pill, 0, 0, 80f, 22f, "×0", TypeRole.Mono500, 13f, Tok.IrisD,
                                TextAlignmentOptions.Center);

            // The new marker: a 10px iris dot on the plate, top left, 11 inset.
            var dot = Dot("Dot", plateHolder, 11f, 11f, 10f);

            var name = Txt("Name", root, 14f, 164f, 404f, 26f, "Construct", TypeRole.Serif, 20f, Tok.Ink);
            var kind = Caps("Kind", root, 14f, 190f, 404f, 10f, "KIND", 8f, Tok.Ink3);
            var blurb = Prose("Blurb", root, 14f, 206f, 404f, 42f, "", 13.5f, Tok.Ink2, 1.5f, TypeRole.SerifItalic);

            var effects = Node("Effects", root, 14f, 252f, 404f, 56f);
            Img(effects, SpriteFactory.Round(9), Tok.Block);
            var effectsStack = Node("Rows", effects, 11f, 9f, 382f, 38f);
            Stack(effectsStack, 4f);

            Rule("Rule", root, 14f, 317f, 404f, Tok.Haze2);

            var costs = Node("Costs", root, 14f, 326f, 280f, 26f);
            var costRow = costs.gameObject.AddComponent<HorizontalLayoutGroup>();
            costRow.spacing = 8f;
            costRow.childControlWidth = true;
            costRow.childControlHeight = true;
            costRow.childForceExpandWidth = false;

            // Build is the only thing on the card you can press.
            var build = PrimaryButton("Build", root, 330f, 322f, 88f, 34f, "Build");
            var reason = Txt("ReasonLine", root, 14f, 358f, 404f, 16f, "", TypeRole.Label400, 11f, Tok.Ink3);
            reason.fontStyle = FontStyles.Italic;

            // As above: the reason line ends at 374, plus 14 of padding.
            At(root, 0, 0, 432f, 388f);

            var view = root.gameObject.AddComponent<ConstructCardView>();
            view.hoverTarget = hover;
            view.cardGround = ground;
            view.cardBorder = border;
            view.ring = ring;
            view.cardGroup = group;
            view.plate = plate;
            view.ownedCount = owned;
            view.constructName = name;
            view.kind = kind;
            view.blurb = blurb;
            view.effects = effectsStack;
            view.effectRowPrefab = Load("UI_EffectRow");
            view.costs = costs;
            view.costPillPrefab = Load("UI_CostPill").GetComponent<CostPill>();
            view.build = build.button;
            view.buildGround = build.ground;
            view.buildLabel = build.label;
            view.reasonLine = reason;
            view.dot = dot;

            Save(root.gameObject, "UI_ConstructCard");
        }

        // A 74px map form in a 100px hit circle, on a soft elliptical shadow. A second
        // asset — not the card glyph scaled up.
        static void PalaceBuilding()
        {
            var root = Node("UI_PalaceBuilding", null, 0, 0, 100f, 100f);
            Img(Node("Shadow", root, 14f, 74f, 72f, 18f), SpriteFactory.Load("Disc"), Tok.Ink, 0.10f);
            // A second asset, not the card glyph scaled up: 48x48 grid, pale fills, ground
            // line at y = 41. Artwork, so it is not tinted. The view swaps in each form.
            Artwork(Node("Form", root, 13f, 10f, 74f, 74f), SpriteFactory.Load("Map/hut"));
            Img(Stretch(Node("UnbuiltRing", root)), SpriteFactory.Load("Ring_Dashed"), Tok.Ink4).gameObject.SetActive(false);

            var badge = Node("CountBadge", root, 62f, 4f, 40f, 20f);
            Img(badge, SpriteFactory.Round(999), Tok.Veil, 0.92f);
            Img(Stretch(Node("Border", badge)), SpriteFactory.Outline(999), Tok.Haze);
            Figures("Count", badge, 0, 0, 40f, 20f, "0", TypeRole.Mono500, 12f, Tok.IrisD, TextAlignmentOptions.Center);

            Button(Stretch(Node("Hit", root)));
            Save(root.gameObject, "UI_PalaceBuilding");
        }

        // Map 880 x 860, radius 16. With the bar, the view row and their gaps the menu is
        // 943 tall in a 985 track.
        static void Constructs()
        {
            var root = Node("UI_Constructs", null, 0, 0, W, TrackH);

            // The view row is not part of the tab bar: the bar carries destinations only,
            // and this is a view of one destination, not another destination.
            var viewRow = Node("ViewRow", root, 0, 0, W, 26f);
            var menuName = Txt("MenuName", viewRow, 0, 0, 300f, 26f, "Constructs", TypeRole.Serif, 17f, Tok.Ink,
                                TextAlignmentOptions.MidlineLeft);

            var toggle = Node("ViewToggle", viewRow, W - 160f, 0, 160f, 26f);
            Img(toggle, SpriteFactory.Round(999), Tok.Block);
            var seg = toggle.gameObject.AddComponent<SegmentedToggle>();
            Dress(seg);
            string[] names = { "Map", "List" };
            for (int i = 0; i < names.Length; i++)
            {
                var segRt = Node(names[i], toggle, 2f + i * 78f, 2f, 78f, 22f);
                var segImg = Img(segRt, SpriteFactory.Round(999), Tok.IrisL, i == 0 ? 1f : 0f, true);
                var segLabel = Caps("Label", segRt, 0, 0, 78f, 22f, names[i], 9f, i == 0 ? Tok.IrisD : Tok.Ink3,
                                    0.16f, TextAlignmentOptions.Center);
                var segBtn = segRt.gameObject.AddComponent<UiButton>();
                seg.segments.Add(segBtn);
                seg.grounds.Add(segImg);
                seg.labels.Add(segLabel);
            }

            // --- the map, which is the default view
            var mapView = Node("MapView", root, 0, 36f, W, 860f);
            // The map's own ground is its hit area, so the empty grounds count as inside it
            // and the buildings and the court card are all descendants: uGUI sends the map
            // its exit only when the pointer leaves the whole map.
            Img(mapView, SpriteFactory.Round(16), Tok.Veil, 0.6f, true);
            var mapHover = mapView.gameObject.AddComponent<UiButton>();
            Img(Stretch(Node("Border", mapView)), SpriteFactory.Outline(16), Tok.Haze2);

            var grounds = Node("Grounds", mapView, 0, 0, W, 860f);
            var groundsGroup = grounds.gameObject.AddComponent<CanvasGroup>();
            // The spec's own drawing of the grounds — four soft quarters and the open court.
            Artwork(Stretch(Node("Art", grounds)), SpriteFactory.Load("Art_Palace"));
            // Spec .qname: 9px caps at .18em, 34 in from the map's left or right edge, 30 from
            // its top or 818 from it — measured on the map, not on a quarter.
            string[] quarters = { "WORKS", "DWELLINGS", "RESERVOIRS", "WARDS" };
            for (int i = 0; i < 4; i++)
            {
                bool right = i % 2 == 1;
                float x = right ? W - 34f - 162f : 34f;
                float y = i < 2 ? 30f : 818f;
                Caps(quarters[i], grounds, x, y, 162f, 12f, quarters[i], 9f, Tok.Ink3, 0.18f,
                     right ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
            }
            // The court — the open ring where nothing is ever built, which is what lets the
            // card land dead center and cost nothing — is part of the grounds artwork.

            var buildings = Node("Buildings", mapView, 0, 0, W, 860f);

            var courtCard = Node("CourtCard", mapView, 224f, 240f, 432f, 382f);
            // Reports the pointer entering and leaving the card as a whole — moving onto
            // its Build button is not leaving it.
            // UiButton needs a Graphic; this one is invisible and never a hit target itself —
            // the card's own ground is — so the events arrive from its children.
            Img(courtCard, null, Tok.Veil, 0f, false);
            var cardHover = courtCard.gameObject.AddComponent<UiButton>();
            var courtInstance = Nest("UI_ConstructCard", courtCard, 0, 0);
            var courtView = courtInstance.GetComponent<ConstructCardView>();

            // --- the list
            var listView = Node("ListView", root, 0, 36f, W, TrackH - 36f);
            var (scroll, content) = Scroll("Scroll", listView, 0, 0, W, TrackH - 36f);
            Stack(content, 0f, new RectOffset(0, 0, 0, 40));

            var view = root.gameObject.AddComponent<ConstructsView>();
            view.menuName = menuName;
            view.viewToggle = seg;
            view.mapView = mapView.gameObject;
            view.listView = listView.gameObject;
            view.listContent = content;
            view.sectionHeadPrefab = Load("UI_SectionHead");
            view.cardPrefab = Load("UI_ConstructCard").GetComponent<ConstructCardView>();
            view.mapRoot = buildings;
            view.buildingPrefab = Load("UI_PalaceBuilding");
            view.courtCard = courtCard;
            view.courtCardView = courtView;
            view.groundsGroup = groundsGroup;
            view.mapHover = mapHover;
            view.cardHover = cardHover;

            Save(root.gameObject, "UI_Constructs");
        }

        // ---- Visions -----------------------------------------------------------
        // 80px hit circle, 38px glyph, and a 3px progress ring at r=36 — 80 rather than a
        // sigil's 62, because the ring needs somewhere to live.
        static void VisionMark()
        {
            var root = Node("UI_VisionMark", null, 0, 0, 80f, 80f);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var track = Img(Stretch(Node("RingTrack", root)), SpriteFactory.Load("Ring_Progress"), Tok.Track);
            var fillRt = Stretch(Node("RingFill", root));
            var fill = Img(fillRt, SpriteFactory.Load("Ring_Progress"), Tok.Iris);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = 2;
            fill.fillAmount = 0.5f;

            var glyphRt = Node("Glyph", root, 21f, 21f, 38f, 38f);
            var glyph = Img(glyphRt, SpriteFactory.Glyph("Vision", "vigil") ?? SpriteFactory.Load("Disc"), Tok.IrisD);

            var channel = Node("ChannelDot", root, 62f, 4f, 9f, 9f);
            Img(channel, SpriteFactory.Load("Disc"), Tok.Iris);

            var btn = Button(Stretch(Node("Hit", root)));

            var view = root.gameObject.AddComponent<VisionMarkView>();
            view.button = btn;
            view.glyph = glyph;
            view.ringTrack = track;
            view.ringFill = fill;
            view.channelDot = channel.gameObject;
            view.group = group;

            Save(root.gameObject, "UI_VisionMark");
        }

        // Field 880 x 760, radius 16. Taller than the column's 985 once the bar is counted,
        // so the pane scrolls.
        static void Visions()
        {
            var root = Node("UI_Visions", null, 0, 0, W, TrackH);

            var field = Node("Field", root, 0, 0, W, 760f);
            // The field is its own hit area, and the marks, the eye and the readout are all
            // descendants: uGUI sends it its exit only when the pointer leaves the whole field.
            Img(field, SpriteFactory.Round(16), Tok.Veil, 0.2f, true);
            var fieldHover = field.gameObject.AddComponent<UiButton>();
            FieldBloom(field, 440f, 380f, 880f * 0.56f * 0.74f, 760f * 0.56f * 0.74f);
            Img(Stretch(Node("Border", field)), SpriteFactory.Outline(16), Tok.Haze2);

            var center = Node("Center", field, 0, 0, 0, 0);
            center.anchorMin = center.anchorMax = new Vector2(0.5f, 0.5f);
            center.pivot = new Vector2(0.5f, 0.5f);
            center.anchoredPosition = Vector2.zero;

            var eyeRt = Node("Eye", center, 0, 0, 480f, 250f);
            eyeRt.anchorMin = eyeRt.anchorMax = new Vector2(0.5f, 0.5f);
            eyeRt.pivot = new Vector2(0.5f, 0.5f);
            eyeRt.anchoredPosition = Vector2.zero;
            var eyeImg = Img(eyeRt, SpriteFactory.Load("Art_Eye"), Tok.Veil);
            eyeImg.color = Color.white;
            var et = eyeImg.GetComponent<ThemedGraphic>();
            if (et != null) Object.DestroyImmediate(et);
            var eyeBtn = eyeRt.gameObject.AddComponent<UiButton>();
            eyeImg.raycastTarget = true;
            var eyeGroup = eyeRt.gameObject.AddComponent<CanvasGroup>();

            // Italic 11px ink4, 12 above the foot: how many Visions, and how many channelling.
            var fieldCaption = Txt("Caption", field, 0f, 760f - 12f - 16f, W, 16f, "", TypeRole.Label400, 11f,
                                   Tok.Ink4, TextAlignmentOptions.Bottom);
            fieldCaption.fontStyle = FontStyles.Italic;
            fieldCaption.raycastTarget = false;
            fieldCaption.transform.SetSiblingIndex(center.GetSiblingIndex());

            var irisRing = Node("IrisRing", eyeRt, 0, 0, 0, 0);
            irisRing.anchorMin = irisRing.anchorMax = new Vector2(0.5f, 0.5f);
            irisRing.pivot = new Vector2(0.5f, 0.5f);
            irisRing.anchoredPosition = Vector2.zero;

            var markLayer = Node("Marks", center, 0, 0, 0, 0);
            markLayer.anchorMin = markLayer.anchorMax = new Vector2(0.5f, 0.5f);
            markLayer.pivot = new Vector2(0.5f, 0.5f);
            markLayer.anchoredPosition = Vector2.zero;

            // Readout: 620 x 336, dead center. A left column of 344 that says what the Vision
            // is and how far along it is, a 1px rule, and a tinted right column that pours.
            var readout = Node("Readout", center, 0, 0, 620f, 336f);
            readout.anchorMin = readout.anchorMax = new Vector2(0.5f, 0.5f);
            readout.pivot = new Vector2(0.5f, 0.5f);
            readout.anchoredPosition = Vector2.zero;
            Img(readout, null, Tok.Veil, 0f, true);
            var readoutHover = readout.gameObject.AddComponent<UiButton>();
            // box-shadow 0 22px 48px -28px, as on the Revelations readout.
            var shadow = Img(Stretch(Node("Shadow", readout), -20f, 2f, -20f, -42f),
                             SpriteFactory.Load("Shadow_Readout"), Tok.Ink, 0.45f);
            shadow.raycastTarget = false;
            var ground = Img(Stretch(Node("Ground", readout)), SpriteFactory.Round(14), Tok.Veil);
            ground.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var border = Img(Stretch(Node("Border", readout)), SpriteFactory.Outline(14), Tok.Haze);
            border.raycastTarget = false;

            VerticalLayoutGroup Column(RectTransform rt, float gap)
            {
                var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(20, 20, 19, 17);
                v.spacing = gap;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                return v;
            }
            VerticalLayoutGroup Stacked(RectTransform rt, float gap)
            {
                var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = gap;
                v.childControlWidth = v.childControlHeight = true;
                v.childForceExpandWidth = true;
                v.childForceExpandHeight = false;
                return v;
            }
            HorizontalLayoutGroup Inline(RectTransform rt, float gap, TextAnchor align = TextAnchor.MiddleLeft)
            {
                var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = gap;
                h.childAlignment = align;
                h.childControlWidth = h.childControlHeight = true;
                h.childForceExpandWidth = h.childForceExpandHeight = false;
                return h;
            }
            LayoutElement Fixed(Component c, float w, float h)
            {
                var le = c.gameObject.AddComponent<LayoutElement>();
                if (w > 0f) le.preferredWidth = w;
                if (h > 0f) le.preferredHeight = h;
                return le;
            }

            // --- left: 344, padding 19 / 20 / 17, gap 10
            var left = Node("Left", ground.transform, 0, 0, 344f, 336f);
            Column(left, 10f);

            var head = Node("Head", left, 0, 0, 304f, 60f);
            Stacked(head, 5f);
            var kind = Caps("Kind", head, 0, 0, 304f, 10f, "VISION", 8.5f, Tok.IrisD);
            var nameRow = Node("NameRow", head, 0, 0, 304f, 28f);
            Inline(nameRow, 9f);
            var mark = Img(Node("Mark", nameRow, 0, 0, 22f, 22f),
                           SpriteFactory.Glyph("Vision", "tide") ?? SpriteFactory.Load("Disc"), Tok.IrisD);
            mark.raycastTarget = false;
            Fixed(mark, 22f, 22f);
            var name = Txt("Name", nameRow, 0, 0, 260f, 28f, "Vision", TypeRole.Serif, 26f, Tok.Ink,
                           TextAlignmentOptions.MidlineLeft);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            var repeatLine = Txt("RepeatLine", head, 0, 0, 304f, 14f, "", TypeRole.Label400, 10.5f, Tok.Ink3);
            repeatLine.fontStyle = FontStyles.Italic;

            var blurb = Prose("Blurb", left, 0, 0, 304f, 42f, "", 14f, Tok.Ink2, 1.5f, TypeRole.SerifItalic);

            var prog = Node("Progress", left, 0, 0, 304f, 44f);
            Stacked(prog, 6f);
            var pnum = Node("Number", prog, 0, 0, 304f, 34f);
            Inline(pnum, 8f, TextAnchor.LowerLeft);
            var percent = Figures("Percent", pnum, 0, 0, 80f, 34f, "0%", TypeRole.Mono500, 30f, Tok.IrisD,
                                  TextAlignmentOptions.BottomLeft);
            percent.textWrappingMode = TextWrappingModes.NoWrap;
            var ofVision = Txt("Of", pnum, 0, 0, 90f, 20f, "of the Vision", TypeRole.Label400, 11f, Tok.Ink3,
                               TextAlignmentOptions.BottomLeft);
            ofVision.textWrappingMode = TextWrappingModes.NoWrap;
            ofVision.margin = new Vector4(0f, 0f, 0f, 5f);
            var percentBar = Track("Bar", prog, 0, 0, 304f, 6f);
            Fixed(percentBar, 0f, 6f);

            Node("Spacer", left, 0, 0, 304f, 0f).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // On completion: block ground, radius 9, padding 9 / 11, gap 4.
            var effects = Node("Effects", left, 0, 0, 304f, 60f);
            Img(effects, SpriteFactory.Round(9), Tok.Block);
            var fxStack = Stacked(effects, 4f);
            fxStack.padding = new RectOffset(11, 11, 9, 9);
            Caps("Caption", effects, 0, 0, 280f, 10f, "ON COMPLETION", 8f, Tok.Ink3);

            // --- the rule, then right: 275, its own faint ground, padding 19 / 20 / 17, gap 9
            Img(Node("Rule", ground.transform, 344f, 0, 1f, 336f), null, Tok.Haze2);
            var right = Node("Right", ground.transform, 345f, 0, 275f, 336f);
            var rightGround = Img(right, null, Tok.Veil);
            rightGround.raycastTarget = false;
            Column(right, 9f);

            var rightCaption = Caps("Caption", right, 0, 0, 235f, 10f, "POUR", 8f, Tok.Ink3);
            var offers = Node("Offers", right, 0, 0, 235f, 110f);
            Stacked(offers, 6f);

            Node("Spacer", right, 0, 0, 235f, 0f).gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var pour = PrimaryButton("Pour", right, 0, 0, 235f, 38f, "Pour", 16f, 9);
            Fixed(pour.root, 0f, 38f);

            // Channel: a switch and its name above a 1px rule, the hint indented under it.
            var chRow = Node("ChannelRow", right, 0, 0, 235f, 28f);
            var chRowLayout = Inline(chRow, 9f);
            chRowLayout.padding = new RectOffset(0, 0, 9, 0);
            var chRule = Img(Node("Rule", chRow, 0, 0, 235f, 1f), null, Tok.Haze2);
            var chRuleRt = chRule.rectTransform;
            chRuleRt.anchorMin = new Vector2(0f, 1f); chRuleRt.anchorMax = new Vector2(1f, 1f);
            chRuleRt.pivot = new Vector2(0.5f, 1f);
            chRuleRt.offsetMin = new Vector2(0f, -1f); chRuleRt.offsetMax = Vector2.zero;
            chRule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var swRt = Node("Switch", chRow, 0, 0, 34f, 19f);
            Fixed(swRt, 34f, 19f);
            var swTrack = Img(swRt, SpriteFactory.Round(999), Tok.Track, 1f, true);
            Img(Stretch(Node("Border", swRt)), SpriteFactory.Outline(999), Tok.Haze).raycastTarget = false;
            var knob = Node("Knob", swRt, 3f, 3f, 13f, 13f);
            Img(knob, SpriteFactory.Load("Disc"), Tok.Veil).raycastTarget = false;
            var swBtn = swRt.gameObject.AddComponent<UiButton>();
            var sw = swRt.gameObject.AddComponent<ToggleSwitch>();
            Dress(sw);
            sw.button = swBtn;
            sw.track = swTrack;
            sw.knob = knob;
            sw.knobOffX = 3f;
            sw.knobOnX = 18f;
            var chLabel = Txt("Label", chRow, 0, 0, 80f, 19f, "Channel", TypeRole.Label500, 12.5f, Tok.Ink,
                              TextAlignmentOptions.MidlineLeft);
            chLabel.textWrappingMode = TextWrappingModes.NoWrap;

            var chHint = Txt("Hint", right, 0, 0, 235f, 14f, "once a second, while you can pay",
                             TypeRole.Label400, 10.5f, Tok.Ink3);
            chHint.fontStyle = FontStyles.Italic;
            chHint.margin = new Vector4(43f, -4f, 0f, 0f);

            // A finished golden Vision, read back out of the iris: 420 wide, centered, 512 down
            // the field. A 92 gold mark panel and a body that flows; veil on a gold-b edge.
            // The shadow is a sibling, because the plaque clips what it holds to its corners.
            var pShadow = Img(Node("PlaqueShadow", field, (W - 420f) * 0.5f - 20f, 512f + 2f, 460f, 200f),
                              SpriteFactory.Load("Shadow_Readout"), Tok.GoldD, 0.4f);
            pShadow.raycastTarget = false;
            var plaque = Node("Plaque", field, (W - 420f) * 0.5f, 512f, 420f, 160f);
            Img(plaque, SpriteFactory.Round(14), Tok.Veil, 1f, true);
            plaque.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var plaqueHover = plaque.gameObject.AddComponent<UiButton>();
            var plaqueFit = plaque.gameObject.AddComponent<ContentSizeFitter>();
            plaqueFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var plaqueRow = plaque.gameObject.AddComponent<HorizontalLayoutGroup>();
            plaqueRow.childControlWidth = plaqueRow.childControlHeight = true;
            plaqueRow.childForceExpandWidth = false;
            plaqueRow.childForceExpandHeight = true;
            var pMarkPanel = Node("Mark", plaque, 0, 0, 92f, 160f);
            Size(pMarkPanel, w: 92f);
            var pMarkGround = Img(pMarkPanel, null, Tok.GoldL);
            pMarkGround.raycastTarget = false;
            var pMarkRule = Img(Node("Rule", pMarkPanel, 91f, 0, 1f, 160f), null, Tok.GoldB);
            var pmr = pMarkRule.rectTransform;
            pmr.anchorMin = new Vector2(1f, 0f); pmr.anchorMax = new Vector2(1f, 1f);
            pmr.pivot = new Vector2(1f, 0.5f);
            pmr.offsetMin = new Vector2(-1f, 0f); pmr.offsetMax = Vector2.zero;
            var pGlyph = Img(Node("Glyph", pMarkPanel, 0, 0, 46f, 46f), SpriteFactory.Glyph("Vision", "bell"), Tok.GoldD);
            Centered(pGlyph.rectTransform);
            var pBody = Node("Body", plaque, 0, 0, 328f, 160f);
            Size(pBody, flexW: 1f);
            VCol(pBody, 8f, new RectOffset(17, 17, 15, 14));
            var pHead = Node("Head", pBody, 0, 0, 294f, 34f);
            VCol(pHead, 4f);
            var pKind = Caps("Kind", pHead, 0, 0, 294f, 10f, "A GOLDEN VISION \u00B7 COMPLETE", 8.5f, Tok.GoldD);
            pKind.raycastTarget = false;
            var pName = Txt("Name", pHead, 0, 0, 294f, 24f, "Vision", TypeRole.Serif, 22f, Tok.Ink);
            pName.raycastTarget = false;
            var pBlurb = Prose("Blurb", pBody, 0, 0, 294f, 40f, "", 13.5f, Tok.Ink2, 1.5f, TypeRole.SerifItalic);
            pBlurb.raycastTarget = false;
            var pFx = Node("Effects", pBody, 0, 0, 294f, 60f);
            Img(pFx, SpriteFactory.Round(9), Tok.Block).raycastTarget = false;
            VCol(pFx, 4f, new RectOffset(11, 11, 9, 9));
            Caps("Caption", pFx, 0, 0, 270f, 10f, "WHAT IT CHANGED", 8f, Tok.Ink3).raycastTarget = false;
            var pBorder = Img(Stretch(Node("Border", plaque)), SpriteFactory.Outline(14), Tok.GoldB);
            pBorder.raycastTarget = false;
            Size(pBorder).ignoreLayout = true;

            var view = root.gameObject.AddComponent<VisionsView>();
            view.plaque = plaque.gameObject;
            view.plaqueHover = plaqueHover;
            view.plaqueShadow = pShadow.rectTransform;
            view.plaqueGlyph = pGlyph;
            view.plaqueName = pName;
            view.plaqueBlurb = pBlurb;
            view.plaqueEffects = pFx;
            view.field = field;
            view.eye = eyeRt;
            view.markLayer = markLayer;
            view.markPrefab = Load("UI_VisionMark").GetComponent<VisionMarkView>();
            view.irisRing = irisRing;
            view.eyeHover = eyeBtn;
            view.readout = readout.gameObject;
            view.visionName = name;
            view.blurb = blurb;
            view.effects = effects;
            view.effectRowPrefab = Load("UI_EffectRow");
            view.percentLabel = percent;
            view.percentBar = percentBar;
            view.repeatLine = repeatLine;
            view.offers = offers;
            view.offerRowPrefab = Load("UI_OfferRow");
            view.pour = pour.button;
            view.pourLabel = pour.label;
            view.channel = sw;
            view.readoutHover = readoutHover;
            view.fieldHover = fieldHover;
            view.readoutBorder = border;
            view.readoutShadow = shadow;
            view.rightGround = rightGround;
            view.kindCaption = kind;
            view.mark = mark;
            view.rightCaption = rightCaption;
            view.pourGround = pour.ground;
            view.channelHint = chHint;
            view.fieldCaption = fieldCaption;
            view.eyeGroup = eyeGroup;

            Save(root.gameObject, "UI_Visions");
        }

        // ---- Assault -----------------------------------------------------------
        // Small layout helpers for the flowed parts of this menu.
        static HorizontalLayoutGroup HRow(RectTransform rt, float gap, TextAnchor align = TextAnchor.MiddleLeft,
                                          RectOffset pad = null, bool expandHeight = false)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = gap;
            h.childAlignment = align;
            h.padding = pad ?? new RectOffset();
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = expandHeight;
            return h;
        }

        static VerticalLayoutGroup VCol(RectTransform rt, float gap, RectOffset pad = null)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = gap;
            v.padding = pad ?? new RectOffset();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        static LayoutElement Size(Component c, float w = -1f, float h = -1f, float flexW = -1f, float flexH = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) { le.preferredWidth = w; le.minWidth = w; }
            if (h >= 0f) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0f) le.flexibleWidth = flexW;
            if (flexH >= 0f) le.flexibleHeight = flexH;
            return le;
        }

        static RectTransform Spacer(Transform parent, bool vertical)
        {
            var s = Node("Spacer", parent, 0, 0, 0f, 0f);
            if (vertical) Size(s, flexH: 1f); else Size(s, flexW: 1f);
            return s;
        }

        static TMP_Text Line(string name, Transform parent, string text, TypeRole face, float size, Tok color)
        {
            var t = Txt(name, parent, 0, 0, 100f, size * 1.4f, text, face, size, color, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>One copy of the benefit line inside its masked window: unwrapped and free to
        /// run long, since the marquee sizes and moves it.</summary>
        static TMP_Text BenefitLine(string name, RectTransform window)
        {
            var t = Line(name, window, "", TypeRole.Label400, 12f, Tok.Prose);
            t.overflowMode = TextOverflowModes.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(100f, 17f);
            rt.anchoredPosition = Vector2.zero;
            return t;
        }

        /// <summary>Small caps tracked .16em, and — like every tracked run — set without pair
        /// kerning, which TMP would otherwise let swallow the tracking at each kerned pair.</summary>
        static TMP_Text Tracked(TMP_Text t)
        {
            t.characterSpacing = 16f;
            Typeset.SetKerning(t, false);
            return t;
        }

        /// <summary>A pill that sizes to its words: ground and border behind a padded row.</summary>
        static (RectTransform root, Image ground, Image border, TMP_Text label) Pill(
            string name, Transform parent, RectOffset pad, int radius, float size, TypeRole face)
        {
            var root = Node(name, parent, 0, 0, 60f, 20f);
            HRow(root, 5f, TextAnchor.MiddleLeft, pad);
            var fit = root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var ground = Img(Stretch(Node("Ground", root)), SpriteFactory.Round(radius), Tok.Block);
            ground.raycastTarget = false;
            Size(ground).ignoreLayout = true;
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(radius), Tok.Haze);
            border.raycastTarget = false;
            Size(border).ignoreLayout = true;
            var label = Line("Label", root, "", face, size, Tok.Ink2);
            return (root, ground, border, label);
        }

        // A unit card: a 96 portrait, a body that flows, a footer pinned to the bottom.
        static void UnitCard()
        {
            var root = Node("UI_UnitCard", null, 0, 0, 432f, 164f);
            var ground = Img(root, SpriteFactory.Round(13), Tok.Veil, 1f, true);
            root.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            HRow(root, 0f, TextAnchor.UpperLeft, null, true);
            var hover = root.gameObject.AddComponent<UiButton>();

            // Portrait: 96 wide, the full height of the card, cut to fill (slice).
            var pt = Node("Portrait", root, 0, 0, 96f, 164f);
            Size(pt, w: 96f);
            pt.gameObject.AddComponent<RectMask2D>();
            var portrait = Artwork(Node("Art", pt, 0, 0, 96f, 150f), SpriteFactory.Load("Portrait_Unit_guard"));
            portrait.raycastTarget = false;
            var art = portrait.rectTransform;
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = Vector2.zero;
            var fitter = art.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 96f / 150f;
            var ptRule = Img(Node("Rule", pt, 95f, 0, 1f, 164f), null, Tok.Haze2);
            var prt = ptRule.rectTransform;
            prt.anchorMin = new Vector2(1f, 0f); prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 0.5f);
            prt.offsetMin = new Vector2(-1f, 0f); prt.offsetMax = Vector2.zero;

            // "×48", bottom left, 7 in.
            var owned = Pill("Owned", pt, new RectOffset(7, 7, 1, 1), 999, 11f, TypeRole.Mono500);
            var ort = owned.root;
            ort.anchorMin = ort.anchorMax = new Vector2(0f, 0f);
            ort.pivot = new Vector2(0f, 0f);
            ort.anchoredPosition = new Vector2(7f, 7f);
            owned.root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Body: padding 12 / 13, gap 7.
            var body = Node("Body", root, 0, 0, 336f, 164f);
            Size(body, flexW: 1f);
            VCol(body, 7f, new RectOffset(13, 13, 12, 12));
            var name = Txt("Name", body, 0, 0, 310f, 22f, "Unit", TypeRole.Serif, 19f, Tok.Ink);
            name.raycastTarget = false;
            var blurb = Prose("Blurb", body, 0, 0, 310f, 36f, "", 12.5f, Tok.Ink2, 1.42f, TypeRole.SerifItalic);
            blurb.raycastTarget = false;
            var pwrRow = Node("PowerRow", body, 0, 0, 310f, 20f);
            HRow(pwrRow, 0f);
            var pwr = Pill("Power", pwrRow, new RectOffset(8, 8, 2, 2), 7, 11.5f, TypeRole.Label500);
            Spacer(pwrRow, false);
            Spacer(body, true);

            // Footer: 9 above a 1px rule, costs left, Muster right.
            var foot = Node("Foot", body, 0, 0, 310f, 41f);
            HRow(foot, 10f, TextAnchor.LowerLeft, new RectOffset(0, 0, 9, 0));
            var footRule = Img(Node("Rule", foot, 0, 0, 310f, 1f), null, Tok.Haze2);
            var frt = footRule.rectTransform;
            frt.anchorMin = new Vector2(0f, 1f); frt.anchorMax = new Vector2(1f, 1f);
            frt.pivot = new Vector2(0.5f, 1f);
            frt.offsetMin = new Vector2(0f, -1f); frt.offsetMax = Vector2.zero;
            Size(footRule).ignoreLayout = true;
            var costs = Node("Costs", foot, 0, 0, 200f, 24f);
            HRow(costs, 5f, TextAnchor.LowerLeft);
            Size(costs, flexW: 1f);
            // Height 32, padding 0 / 15, radius 9, serif 600 at 15.
            var muster = PrimaryButton("Muster", foot, 0, 0, 78f, 32f, "Muster", 15f, 9);
            Size(muster.root, w: 78f, h: 32f);

            // The border and the new-dot go over everything.
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(13), Tok.Haze);
            border.raycastTarget = false;
            Size(border).ignoreLayout = true;
            var dotRt = Node("NewDot", root, 9f, 9f, 10f, 10f);
            Size(dotRt).ignoreLayout = true;
            Img(Stretch(Node("Halo", dotRt), -3, -3, -3, -3), SpriteFactory.Load("Disc"), Tok.Veil, 0.9f);
            Img(Stretch(Node("Dot", dotRt)), SpriteFactory.Load("Disc"), Tok.Iris);

            var view = root.gameObject.AddComponent<UnitCardView>();
            view.ground = ground;
            view.border = border;
            view.hover = hover;
            view.portrait = portrait;
            view.unitName = name;
            view.blurb = blurb;
            view.powerGround = pwr.ground;
            view.powerBorder = pwr.border;
            view.power = pwr.label;
            view.ownedGround = owned.ground;
            view.ownedBorder = owned.border;
            view.ownedPill = owned.label;
            view.costs = costs;
            view.costPillPrefab = Load("UI_CostPill").GetComponent<CostPill>();
            view.muster = muster.button;
            view.musterGround = muster.ground;
            view.musterLabel = muster.label;
            view.newDot = dotRt.gameObject;

            Save(root.gameObject, "UI_UnitCard");
        }

        // A place on the road. Taken: a 30 teal dot with a check. Next: 38, white, a 2px iris
        // ring and a 5px glow. The label hangs 5 below the dot, so the dot is what sits on
        // the road, not the pin's center of mass.
        static void RoadPin()
        {
            var root = Node("UI_RoadPin", null, 0, 0, 38f, 38f);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            Centered(Img(Node("Glow", root, 0, 0, 48f, 48f), SpriteFactory.Load("Disc"), Tok.Iris, 0.13f).rectTransform);
            Centered(Img(Node("Dot", root, 0, 0, 38f, 38f), SpriteFactory.Load("Disc"), Tok.Veil).rectTransform);
            Centered(Img(Node("Ring", root, 0, 0, 38f, 38f), SpriteFactory.Load("Ring_Pin38"), Tok.Iris).rectTransform);
            Centered(Img(Node("Glyph", root, 0, 0, 18f, 18f), SpriteFactory.Glyph("Place", "mile"), Tok.IrisD).rectTransform);
            var label = Caps("Label", root, -81f, 43f, 200f, 12f, "PLACE", 8.5f, Tok.Ink, 0.12f,
                             TextAlignmentOptions.Top);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var hit = Button(Stretch(Node("Hit", root)));
            var view = root.gameObject.AddComponent<RoadPinView>();
            view.button = hit;
            view.glow = root.Find("Glow").GetComponent<Image>();
            view.dot = root.Find("Dot").GetComponent<Image>();
            view.ring = root.Find("Ring").GetComponent<Image>();
            view.glyph = root.Find("Glyph").GetComponent<Image>();
            view.label = label;
            view.ring30 = SpriteFactory.Load("Ring_Pin30");
            view.ring38 = SpriteFactory.Load("Ring_Pin38");
            Save(root.gameObject, "UI_RoadPin");
        }

        static void Centered(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        // A roster section head: Karla 700 9px caps and a trailing rule; 14 above, 6 below.
        static void RosterHead()
        {
            var root = Node("UI_RosterHead", null, 0, 0, W, 30f);
            HRow(root, 10f, TextAnchor.MiddleLeft, new RectOffset(2, 2, 14, 6));
            var label = Caps("Label", root, 0, 0, 80f, 10f, "SECTION", 9f, Tok.Ink3);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var rule = Img(Node("Rule", root, 0, 0, 100f, 1f), null, Tok.Haze2);
            Size(rule, h: 1f, flexW: 1f);
            Save(root.gameObject, "UI_RosterHead");
        }

        // The menu is one scroll track, the way Focus is: the band, the 430 map and the 152
        // reading band are fixed, then the roster; 12 between.
        static void Assault()
        {
            RosterHead();
            var root = Node("UI_Assault", null, 0, 0, W, TrackH);
            var (scroll, content) = Scroll("Scroll", root, 0, 0, W, TrackH);
            var stack = content.GetComponent<VerticalLayoutGroup>();
            stack.spacing = 12f;
            stack.padding = new RectOffset(0, 0, 0, 40);

            // ---- the band: padding 12 / 15, gap 12, block ground, 1px haze2, radius 11
            // The band is 48; the spec's map sits 22 under it, not the pane's usual 12, so the
            // band's slot carries the other 10 beneath it.
            var band = Node("Band", content, 0, 0, W, 58f);
            Size(band, h: 58f);
            var bandGround = Img(Stretch(Node("Ground", band), 0, 0, 0, 10f), SpriteFactory.Round(11), Tok.Block);
            var bandBorder = Img(Stretch(Node("Border", band), 0, 0, 0, 10f), SpriteFactory.Outline(11), Tok.Haze2);
            var pad = new RectOffset(15, 15, 12, 12);

            var rest = Stretch(Node("Rest", band), 0, 0, 0, 10f);
            HRow(rest, 12f, TextAnchor.MiddleLeft, pad);
            var you = Node("You", rest, 0, 0, 300f, 24f);
            HRow(you, 9f);
            var sword = Img(Node("Sword", you, 0, 0, 17f, 17f), SpriteFactory.Glyph("Ui", "sword"), Tok.IrisD);
            Size(sword, 17f, 17f);
            Line("Title", you, "Your army", TypeRole.Serif, 17f, Tok.Ink);
            var power = Line("Power", you, "0", TypeRole.Mono500, 22f, Tok.Ink);
            Line("Unit", you, "strength", TypeRole.Label400, 11f, Tok.Ink3);
            Spacer(rest, false);
            var vs = Node("Versus", rest, 0, 0, 300f, 24f);
            HRow(vs, 9f);
            var fields = Line("Fields", vs, "", TypeRole.Label400, 12.5f, Tok.Ink2);
            var enemy = Line("Enemy", vs, "0", TypeRole.Mono500, 16f, Tok.Prose);
            var verdict = Pill("Verdict", vs, new RectOffset(10, 10, 3, 3), 7, 11.5f, TypeRole.Label500);

            var fight = Stretch(Node("Fight", band), 0, 0, 0, 10f);
            HRow(fight, 12f, TextAnchor.MiddleLeft, pad);
            Tracked(Line("Caption", fight, "GIVING BATTLE AT", TypeRole.Label700, 8.5f, Tok.IrisD));
            var fightName = Line("Name", fight, "", TypeRole.Serif, 18f, Tok.Ink);
            var fbar = Track("Bar", fight, 0, 0, 300f, 7f);
            Size(fbar, h: 7f, flexW: 1f).minWidth = 120f;
            var fightNums = Line("Numbers", fight, "", TypeRole.Label400, 12f, Tok.Ink2);
            fight.gameObject.SetActive(false);

            var told = Stretch(Node("Told", band), 0, 0, 0, 10f);
            HRow(told, 12f, TextAnchor.MiddleLeft, pad);
            var fmark = Node("Mark", told, 0, 0, 22f, 22f);
            Size(fmark, 22f, 22f);
            Img(Stretch(Node("Disc", fmark)), SpriteFactory.Load("Disc"), Tok.Veil).color = Color.white;
            var fmarkRing = Img(Stretch(Node("Ring", fmark)), SpriteFactory.Load("Ring_Pin30"), Tok.TealL);
            var fmarkGlyph = Img(Node("Glyph", fmark, 4f, 4f, 14f, 14f), SpriteFactory.Glyph("Ui", "check"), Tok.TealD);
            var toldCaption = Line("Caption", told, "TAKEN", TypeRole.Label700, 8.5f, Tok.TealD);
            Tracked(toldCaption);
            var toldName = Line("Name", told, "", TypeRole.Serif, 18f, Tok.Ink);
            Spacer(told, false);
            var toldTell = Line("Tell", told, "", TypeRole.Label400, 12.5f, Tok.Ink2);
            told.gameObject.SetActive(false);

            // ---- the map: 880 x 430, radius 14, 1px haze2
            var map = Node("Map", content, 0, 0, W, 430f);
            Size(map, h: 430f);
            var mapGround = Img(Stretch(Node("Ground", map)), SpriteFactory.Round(14), Tok.Block);
            mapGround.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var terrain = Artwork(Stretch(Node("Terrain", mapGround.transform)), SpriteFactory.Load("Map_Terrain"));
            terrain.raycastTarget = false;
            // The road ahead: dashed 7 / 8, 3.4 wide, #B9A8D6 at .75, fading out to the top
            // right. The road walked: solid #8A72A8, as far as the place being faced.
            var roadAhead = Stretch(Node("RoadAhead", mapGround.transform)).gameObject.AddComponent<PathLine>();
            roadAhead.color = new Color32(0xB9, 0xA8, 0xD6, 0xBF);
            roadAhead.thickness = 3.4f; roadAhead.dash = 7f; roadAhead.gap = 8f;
            roadAhead.fade = true; roadAhead.raycastTarget = false;
            var walked = Stretch(Node("RoadWalked", mapGround.transform)).gameObject.AddComponent<PathLine>();
            walked.color = new Color32(0x8A, 0x72, 0xA8, 0xFF);
            walked.thickness = 3.4f; walked.raycastTarget = false;
            var mist = Artwork(Stretch(Node("Mist", mapGround.transform)), SpriteFactory.Load("Map_Mist"));
            mist.raycastTarget = false;

            // The Mind Palace, where the march starts: 34, radius 9, iris-l on iris-b.
            var home = Node("Home", mapGround.transform, 0, 0, 34f, 34f);
            home.anchorMin = home.anchorMax = new Vector2(0f, 1f);
            home.pivot = new Vector2(0.5f, 0.5f);
            Img(Stretch(Node("Ground", home)), SpriteFactory.Round(9), Tok.IrisL);
            Img(Stretch(Node("Border", home)), SpriteFactory.Outline(9), Tok.IrisB);
            Centered(Img(Node("Glyph", home, 0, 0, 18f, 18f), SpriteFactory.Glyph("Place", "palace"), Tok.IrisD).rectTransform);
            var homeLabel = Caps("Label", home, -83f, 39f, 200f, 12f, "MIND PALACE", 8.5f, Tok.Ink2, 0.12f,
                                 TextAlignmentOptions.Top);
            homeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var pins = Stretch(Node("Pins", mapGround.transform));
            var mapCaption = Txt("Caption", mapGround.transform, W - 18f - 200f, 430f - 13f - 16f, 200f, 16f,
                                 "the road goes on", TypeRole.Label400, 11f, Tok.Ink4, TextAlignmentOptions.BottomRight);
            mapCaption.fontStyle = FontStyles.Italic;
            mapCaption.raycastTarget = false;
            Img(Stretch(Node("Border", map)), SpriteFactory.Outline(14), Tok.Haze2).raycastTarget = false;

            // ---- the reading band: 880 x 152. Art 236, the words, then 244 of arithmetic.
            var slot = Node("Slot", content, 0, 0, W, 152f);
            Size(slot, h: 152f);
            Img(Stretch(Node("Shadow", slot), -14f, 0f, -14f, -18f), SpriteFactory.Load("Shadow_Panel"), Tok.Ink, 0.12f)
                .raycastTarget = false;
            var slotGround = Img(Stretch(Node("Ground", slot)), SpriteFactory.Round(13), Tok.Veil);
            slotGround.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var placeArt = Artwork(Node("Art", slotGround.transform, 0, 0, 236f, 152f), SpriteFactory.Load("Art_Place_mile"));
            placeArt.raycastTarget = false;
            placeArt.preserveAspect = false;

            var mid = Node("Words", slotGround.transform, 236f, 0, W - 236f - 244f, 152f);
            VCol(mid, 7f, new RectOffset(16, 16, 14, 14));
            var kind = Line("Kind", mid, "THE NEXT PLACE", TypeRole.Label700, 8.5f, Tok.IrisD);
            Tracked(kind);
            var placeName = Txt("Name", mid, 0, 0, 368f, 24f, "Place", TypeRole.Serif, 22f, Tok.Ink);
            var placeBlurb = Prose("Blurb", mid, 0, 0, 368f, 38f, "", 13.5f, Tok.Ink2, 1.45f, TypeRole.SerifItalic);
            Spacer(mid, true);
            var ben = Node("Benefit", mid, 0, 0, 368f, 30f);
            HRow(ben, 7f, TextAnchor.MiddleLeft, new RectOffset(10, 10, 7, 7));
            var benGround = Img(Stretch(Node("Ground", ben)), SpriteFactory.Round(8), Tok.Block);
            Size(benGround).ignoreLayout = true;
            var benCaption = Line("Caption", ben, "WHEN WON", TypeRole.Label700, 8f, Tok.Ink3);
            Tracked(benCaption);
            // The caption keeps its whole width (the view sets it to the words it is given);
            // what it gave or would give is read through the rest of the pill, and creeps
            // along when it is longer than that, rather than running out past the pill's edge.
            var benCaptionSize = Size(benCaption, flexW: 0f);
            benCaptionSize.minWidth = benCaptionSize.preferredWidth = Mathf.Ceil(benCaption.GetPreferredValues("WHEN WON").x);
            var benWindow = Node("Value", ben, 0, 0, 100f, 17f);
            var benWindowSize = Size(benWindow, h: 17f, flexW: 1f);
            benWindowSize.minWidth = 0f;
            benWindowSize.preferredWidth = 0f;
            benWindow.gameObject.AddComponent<RectMask2D>();
            var benGroup = benWindow.gameObject.AddComponent<CanvasGroup>();
            benGroup.blocksRaycasts = false;
            var benValue = BenefitLine("Label", benWindow);
            var benEcho = BenefitLine("Echo", benWindow);
            benEcho.gameObject.SetActive(false);
            var benMarquee = benWindow.gameObject.AddComponent<Marquee>();
            benMarquee.viewport = benWindow;
            benMarquee.label = benValue;
            benMarquee.echo = benEcho;
            benMarquee.group = benGroup;
            benMarquee.pixelsPerSecond = 22f;
            benMarquee.gap = 48f;
            benMarquee.holdSeconds = 2f;
            benMarquee.fadeSeconds = 0.2f;

            Img(Node("Rule", slotGround.transform, W - 244f, 0, 1f, 152f), null, Tok.Haze2);
            var right = Node("Arithmetic", slotGround.transform, W - 243f, 0, 243f, 152f);
            var rightGround = Img(right, null, Tok.Veil);
            rightGround.color = new Color32(0xFB, 0xF9, 0xFD, 0xFF);
            Object.DestroyImmediate(rightGround.GetComponent<ThemedGraphic>());
            VCol(right, 6f, new RectOffset(16, 16, 14, 14));
            var fieldCaption = Line("Caption", right, "THEY FIELD", TypeRole.Label700, 8f, Tok.Ink3);
            Tracked(fieldCaption);
            var fieldNumber = Line("Fielded", right, "0", TypeRole.Mono500, 26f, Tok.Prose);
            var yours = Line("Yours", right, "", TypeRole.Label400, 11.5f, Tok.Ink2);
            var chanceRow = Node("ChanceRow", right, 0, 0, 211f, 20f);
            HRow(chanceRow, 0f);
            var chance = Pill("Chance", chanceRow, new RectOffset(8, 8, 2, 2), 7, 11.5f, TypeRole.Label400);
            Spacer(chanceRow, false);
            Spacer(right, true);
            var give = PrimaryButton("GiveBattle", right, 0, 0, 211f, 38f, "Give battle", 16f, 9);
            Size(give.root, h: 38f);
            var wonRow = Node("WonRow", right, 0, 0, 211f, 38f);
            HRow(wonRow, 0f);
            Size(wonRow, h: 38f);
            var won = Node("Won", wonRow, 0, 0, 80f, 38f);
            HRow(won, 7f, TextAnchor.MiddleLeft, new RectOffset(13, 13, 0, 0));
            var wonGround = Img(Stretch(Node("Ground", won)), SpriteFactory.Round(9), Tok.TealL);
            Size(wonGround).ignoreLayout = true;
            var wonBorder = Img(Stretch(Node("Border", won)), SpriteFactory.Outline(9), Tok.TealL);
            wonBorder.color = new Color32(0xBF, 0xDF, 0xDA, 0xFF);
            Object.DestroyImmediate(wonBorder.GetComponent<ThemedGraphic>());
            Size(wonBorder).ignoreLayout = true;
            var wonCheck = Img(Node("Check", won, 0, 0, 14f, 14f), SpriteFactory.Glyph("Ui", "check"), Tok.TealD);
            Size(wonCheck, 14f, 14f);
            Line("Label", won, "Won", TypeRole.Serif, 16f, Tok.TealD);
            Spacer(wonRow, false);
            Img(Stretch(Node("Border", slot)), SpriteFactory.Outline(13), Tok.Haze).raycastTarget = false;

            // ---- the roster, built by the view: a head per section, then rows of two.
            var roster = Node("Roster", content, 0, 0, W, 10f);
            VCol(roster, 0f);

            var view = root.gameObject.AddComponent<AssaultView>();
            view.bandGround = bandGround;
            view.bandBorder = bandBorder;
            view.restRow = rest.gameObject;
            view.yourStrength = power;
            view.fieldsLine = fields;
            view.theirStrength = enemy;
            view.verdictGround = verdict.ground;
            view.verdictBorder = verdict.border;
            view.verdictLabel = verdict.label;
            view.fightRow = fight.gameObject;
            view.fightName = fightName;
            view.fightBar = fbar;
            view.fightNumbers = fightNums;
            view.toldRow = told.gameObject;
            view.toldRing = fmarkRing;
            view.toldGlyph = fmarkGlyph;
            view.toldCaption = toldCaption;
            view.toldName = toldName;
            view.toldTell = toldTell;
            view.roadAhead = roadAhead;
            view.roadWalked = walked;
            view.home = home;
            view.pinLayer = pins;
            view.pinPrefab = Load("UI_RoadPin").GetComponent<RoadPinView>();
            view.placeArt = placeArt;
            view.kindCaption = kind;
            view.placeName = placeName;
            view.placeBlurb = placeBlurb;
            view.benefitCaption = benCaption;
            view.benefitValue = benValue;
            view.benefitMarquee = benMarquee;
            view.fieldCaption = fieldCaption;
            view.fieldNumber = fieldNumber;
            view.yours = yours;
            view.chanceRow = chanceRow.gameObject;
            view.chanceGround = chance.ground;
            view.chanceBorder = chance.border;
            view.chanceLabel = chance.label;
            view.giveBattle = give.button;
            view.giveBattleGround = give.ground;
            view.giveBattleLabel = give.label;
            view.wonRow = wonRow.gameObject;
            view.roster = roster;
            view.rosterHeadPrefab = Load("UI_RosterHead");
            view.unitCardPrefab = Load("UI_UnitCard").GetComponent<UnitCardView>();
            view.scroll = scroll;

            Save(root.gameObject, "UI_Assault");
        }
    }
}
