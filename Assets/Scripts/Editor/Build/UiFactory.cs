// What Lies In The Depths — prefab construction, in this game's tokens and type roles.
//
// The geometry and the prefab plumbing are Ursine's (Ursine.EditorTools.Ui). What is here
// is everything that has an opinion: which token a panel's ground is, what a button looks
// like, how a small-caps head is set. That division is the point — Ursine can dress any
// game, and none of this file could.
//
// Coordinates are the spec's: x runs right, y runs DOWN from the parent's top-left corner.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.EditorTools;
using Ursine.Text;
using Ursine.UI;
using WhatLiesInTheDepths.Core;
using ThemedGraphic = Ursine.Theming.ThemedGraphic;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class UiFactory
    {
        public const string PrefabDir = "Assets/Prefabs";

        // ---- geometry, straight through to Ursine ------------------------------

        public static RectTransform Node(string name, Transform parent) => Ui.Node(name, parent);

        public static RectTransform Node(string name, Transform parent, float x, float y, float w, float h)
            => Ui.Node(name, parent, x, y, w, h);

        public static RectTransform At(RectTransform rt, float x, float y, float w, float h)
            => Ui.At(rt, x, y, w, h);

        public static RectTransform Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
            => Ui.Stretch(rt, l, t, r, b);

        public static RectTransform Center(RectTransform rt, float w = 0f, float h = 0f)
            => Ui.Center(rt, w, h);

        public static VerticalLayoutGroup Stack(RectTransform rt, float spacing = 0f, RectOffset padding = null)
            => Ui.Stack(rt, spacing, padding);

        public static (ScrollRect scroll, RectTransform content) Scroll(
            string name, Transform parent, float x, float y, float w, float h)
            => Ui.Scroll(name, parent, x, y, w, h);

        public static GameObject Save(GameObject go, string name) => Ui.Save(go, PrefabDir, name);

        public static GameObject Load(string name) => Ui.Load(PrefabDir, name);

        public static GameObject Nest(string prefabName, Transform parent, float x, float y)
            => Ui.Nest(PrefabDir, prefabName, parent, x, y);

        // ---- graphics ----------------------------------------------------------

        public static Image Img(RectTransform rt, Sprite sprite, Tok token, float alpha = 1f, bool raycast = false)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();

            img.sprite = sprite;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycast;
            FitCorners(img, rt);

            var themed = rt.gameObject.GetComponent<ThemedGraphic>();
            if (themed == null) themed = rt.gameObject.AddComponent<ThemedGraphic>();
            themed.Bind((int)token, alpha);
            return img;
        }

        /// <summary>A 9-sliced corner cannot be larger than half the thing it is drawn in.
        /// The pill sprite's corners are 33px across, so in a 4px progress bar or a 20px badge
        /// uGUI squeezes them and the round ends come out as points. Scaling the slices down
        /// to half the shorter side gives the spec's capsule back; anything with room to spare
        /// is left alone.</summary>
        public static void FitCorners(Image img, RectTransform rt)
        {
            if (img == null || img.sprite == null || img.type != Image.Type.Sliced) return;

            float border = Mathf.Max(img.sprite.border.x, img.sprite.border.y);
            float shorter = Mathf.Min(rt.rect.width, rt.rect.height);
            if (border <= 0f || shorter <= 0f) return;      // a stretched node has no size yet

            img.pixelsPerUnitMultiplier = Mathf.Max(1f, border / (shorter * 0.5f));
        }

        public static Image Fill(string name, Transform parent, Sprite sprite, Tok token, float alpha = 1f)
            => Img(Stretch(Node(name, parent)), sprite, token, alpha);

        /// <summary>Artwork does not follow the palette — a plate is a picture, not a
        /// surface — so it is drawn white and its token binding is stripped.</summary>
        public static Image Artwork(RectTransform rt, Sprite sprite)
        {
            var img = Img(rt, sprite, Tok.Veil);
            var themed = img.GetComponent<ThemedGraphic>();
            if (themed != null) Object.DestroyImmediate(themed);
            img.color = Color.white;
            return img;
        }

        /// <summary>Every panel is veil on mist, 1px haze, with a low soft shadow. Radius is
        /// the only thing that varies: 14 on the ledger, 16 on the gauge, 13 on a card.</summary>
        public static RectTransform Panel(string name, Transform parent, float x, float y, float w, float h,
                                          int radius = 14, string shadow = "Shadow_Panel")
        {
            var root = Node(name, parent, x, y, w, h);

            // uGUI draws a parent before its children, so a shadow parented to the panel it
            // falls from paints OVER that panel's ground — which washed every panel in the
            // game with ink. The panel root draws nothing; shadow, ground and border are
            // siblings in that order, and whatever a caller adds goes on top of all three.
            if (!string.IsNullOrEmpty(shadow))
                Img(Stretch(Node("Shadow", root), -10, -4, -10, -16), SpriteFactory.Load(shadow), Tok.Ink, 0.16f);

            Img(Stretch(Node("Ground", root)), SpriteFactory.Round(radius), Tok.Veil);
            Img(Stretch(Node("Border", root)), SpriteFactory.Outline(radius), Tok.Haze);
            return root;
        }

        /// <summary>An inset panel inside a panel — the dive block, the plug, the save note.</summary>
        public static RectTransform Block(string name, Transform parent, float x, float y, float w, float h,
                                          int radius = 12)
        {
            var root = Node(name, parent, x, y, w, h);
            Img(root, SpriteFactory.Round(radius), Tok.Block);
            Img(Stretch(Node("Border", root)), SpriteFactory.Outline(radius), Tok.Haze2);
            return root;
        }

        public static Image Rule(string name, Transform parent, float x, float y, float w, Tok token = Tok.Haze)
            => Img(Node(name, parent, x, y, w, 1f), null, token);

        // ---- text --------------------------------------------------------------

        public static TMP_Text Txt(string name, Transform parent, float x, float y, float w, float h,
                                   string text, TypeRole face, float size, Tok color,
                                   TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Node(name, parent, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text ?? string.Empty;
            Typeset.Set(t, face, size, (int)color);
            t.alignment = align;
            Localize(t, text);
            return t;
        }

        // ---- words ---------------------------------------------------------------
        // Every word typed into a builder goes into Strings.json, under ui.built.<words>, the
        // first time it is built, and the label carries a LocalizedText pointing at it. A view
        // that writes its own text over a label is left alone (LocalizedText backs off from any
        // text it did not put there), so this only ever governs the labels that stay put.

        static bool _stringsDirty;

        /// <summary>Gives a built label its key in the strings file, adding the key if the
        /// file does not have it yet. Text with no letters in it (figures, marks) is skipped.</summary>
        public static void Localize(TMP_Text t, string text)
        {
            if (t == null) return;
            var existing = t.GetComponent<LocalizedText>();
            bool hasLetter = false;
            if (!string.IsNullOrEmpty(text))
                foreach (char c in text) if (char.IsLetter(c)) { hasLetter = true; break; }
            if (!hasLetter)
            {
                if (existing != null) Object.DestroyImmediate(existing, true);
                return;
            }

            string key = "ui.built." + Slug(text);
            var file = Loc.File;
            if (!(file[Loc.Fallback] is JsonObject en)) { en = new JsonObject(); file[Loc.Fallback] = en; }
            if (!en.Has(key)) { en[key] = text; _stringsDirty = true; }

            var lt = existing != null ? existing : t.gameObject.AddComponent<LocalizedText>();
            lt.key = key;
            lt.built = text;
        }

        /// <summary>A key from the words themselves: lower case, letters and digits, dotted.
        /// Words set in capitals get their own key, since a translator sets them differently.</summary>
        static string Slug(string text)
        {
            var b = new System.Text.StringBuilder();
            bool gap = false;
            foreach (char c in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) { if (gap && b.Length > 0) b.Append('_'); b.Append(c); gap = false; }
                else gap = true;
                if (b.Length >= 48) break;
            }
            bool caps = text == text.ToUpperInvariant() && text != text.ToLowerInvariant();
            return b + (caps ? ".caps" : "");
        }

        /// <summary>Writes any keys the build added back into Strings.json. Called once, at the
        /// end of Build All UI.</summary>
        public static void FlushStrings()
        {
            if (!_stringsDirty) return;
            _stringsDirty = false;
            System.IO.File.WriteAllText(Strings.AssetPath, MiniJson.Write(Loc.File), new System.Text.UTF8Encoding(false));
            UnityEditor.AssetDatabase.ImportAsset(Strings.AssetPath);
            Debug.Log("[What Lies In The Depths] Strings.json: added the builders' labels.");
        }

        /// <summary>Small caps are real upper case with tracking, never a synthesized
        /// variant: 9-10px, .14-.20em, weight 700.</summary>
        public static TMP_Text Caps(string name, Transform parent, float x, float y, float w, float h,
                                    string text, float size, Tok color, float tracking = 0.16f,
                                    TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = Txt(name, parent, x, y, w, h, text, TypeRole.Label700, size, color, align);
            Typeset.SmallCaps(t, size, tracking, (int)color);
            t.alignment = align;
            return t;
        }

        public static TMP_Text Figures(string name, Transform parent, float x, float y, float w, float h,
                                       string text, TypeRole mono, float size, Tok color,
                                       TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = Txt(name, parent, x, y, w, h, text, mono, size, color, align);
            Typeset.Figures(t, mono, size, (int)color);
            t.alignment = align;
            return t;
        }

        public static TMP_Text Prose(string name, Transform parent, float x, float y, float w, float h,
                                     string text, float size, Tok color, float lineHeight = 1.62f,
                                     TypeRole face = TypeRole.Serif)
        {
            var t = Txt(name, parent, x, y, w, h, text, face, size, color);
            Typeset.Wrap(t, lineHeight);
            return t;
        }

        // ---- interaction -------------------------------------------------------

        /// <summary>A pointer target. Mouse only: no Selectable, no navigation, no focus ring.</summary>
        public static UiButton Button(RectTransform rt)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null)
            {
                img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f);      // an invisible hit area
            }
            img.raycastTarget = true;

            var b = rt.gameObject.GetComponent<UiButton>();
            if (b == null) b = rt.gameObject.AddComponent<UiButton>();
            return b;
        }

        /// <summary>A filled iris control. Cormorant, because a button label that is a verb
        /// is written, not labeled.</summary>
        public static (RectTransform root, Image ground, TMP_Text label, UiButton button)
            PrimaryButton(string name, Transform parent, float x, float y, float w, float h,
                          string text, float size = 15.5f, int radius = 9,
                          Tok ground = Tok.Iris, Tok ink = Tok.Veil)
        {
            var root = Node(name, parent, x, y, w, h);
            var img = Img(root, SpriteFactory.Round(radius), ground, 1f, true);
            var label = Txt("Label", root, 0, 0, w, h, text, TypeRole.Serif, size, ink,
                            TextAlignmentOptions.Center);
            Stretch((RectTransform)label.transform);
            var btn = root.gameObject.AddComponent<UiButton>();
            return (root, img, label, btn);
        }

        public static (RectTransform root, Image border, TMP_Text label, UiButton button)
            OutlineButton(string name, Transform parent, float x, float y, float w, float h,
                          string text, float size = 15.5f, int radius = 9,
                          Tok borderTok = Tok.IrisB, Tok ink = Tok.IrisD)
        {
            var root = Node(name, parent, x, y, w, h);
            Img(root, SpriteFactory.Round(radius), Tok.Veil, 0f, true);
            var border = Img(Stretch(Node("Border", root)), SpriteFactory.Outline(radius), borderTok);
            var label = Txt("Label", root, 0, 0, w, h, text, TypeRole.Serif, size, ink,
                            TextAlignmentOptions.Center);
            Stretch((RectTransform)label.transform);
            var btn = root.gameObject.AddComponent<UiButton>();
            return (root, border, label, btn);
        }

        /// <summary>The iris dot: 7px in a bar, 10px on a card plate, with a spread-only
        /// halo behind it — never a blur.</summary>
        public static AttentionDot Dot(string name, Transform parent, float x, float y, float size = 7f)
        {
            var root = Node(name, parent, x, y, size, size);
            Img(Node("Halo", root, -3f, -3f, size + 6f, size + 6f), SpriteFactory.Load("Disc"), Tok.Iris, 0.16f);
            Img(Stretch(Node("Dot", root)), SpriteFactory.Load("Disc"), Tok.Iris);

            var comp = root.gameObject.AddComponent<AttentionDot>();
            comp.root = root.gameObject;
            return comp;
        }

        /// <summary>A track-and-fill bar. Everywhere but Options a bar reports; there it asks.</summary>
        public static ProgressTrack Track(string name, Transform parent, float x, float y, float w, float h,
                                          Tok fillTok = Tok.Iris)
        {
            var root = Node(name, parent, x, y, w, h);
            Img(root, SpriteFactory.Round(999), Tok.Track);
            var fill = Node("Fill", root, 0, 0, 0, h);
            var fillImg = Img(fill, SpriteFactory.Round(999), fillTok);
            // The fill is built with no width at all, so its corners are fitted to the bar's
            // height rather than to the rect it happens to have while being built.
            PillEnds(fillImg, h);

            var t = root.gameObject.AddComponent<ProgressTrack>();
            t.fill = fill;
            t.width = w;
            return t;
        }

        /// <summary>FitCorners for a bar whose width changes as it fills: the ends stay
        /// half-circles of its height.</summary>
        public static void PillEnds(Image img, float height)
        {
            if (img == null || img.sprite == null || height <= 0f) return;
            float border = img.sprite.border.x;      // the same on all four sides
            if (border <= 0f) return;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = Mathf.Max(1f, border / (height * 0.5f));
        }

        // ---- widget dressing ---------------------------------------------------
        // Ursine's widgets take their tokens as fields so they can be dressed by any
        // palette. These are this game's answers.

        public static void Dress(Stepper s)
        {
            s.liveGroundToken = (int)Tok.IrisL;
            s.deadGroundToken = (int)Tok.Block;
            s.fullMessage = "task full";
            s.emptyPoolMessage = "no Oneiri";
        }

        public static void Dress(CostPill p)
        {
            p.payableGroundToken = (int)Tok.Block;
            p.payableInkToken = (int)Tok.Ink2;
            p.payableGroundTransparent = true;
            // Short of a resource resolves itself while the player watches.
            p.shortGroundToken = (int)Tok.RoseL;
            p.shortInkToken = (int)Tok.RoseD;
            // Above a ceiling is structural — waiting will never fix it.
            p.overGroundToken = (int)Tok.GoldL;
            p.overInkToken = (int)Tok.GoldD;
            p.shortBorderToken = (int)Tok.RoseB;
            p.overBorderToken = (int)Tok.GoldB;
        }

        public static void Dress(SegmentedToggle t)
        {
            t.selectedGroundToken = (int)Tok.IrisL;
            t.selectedInkToken = (int)Tok.IrisD;
            t.restInkToken = (int)Tok.Ink3;
        }

        public static void Dress(ToggleSwitch s)
        {
            s.onTrackToken = (int)Tok.Iris;
            s.offTrackToken = (int)Tok.Track;
        }

        public static void Dress(FillBar f) => f.slideSeconds = Layout.MotionFill;
    }
}
