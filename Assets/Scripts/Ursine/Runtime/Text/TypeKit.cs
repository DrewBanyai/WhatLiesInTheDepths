// Ursine — type roles.
//
// Three faces is a common house rule for an incremental game: a serif for anything named
// or written, a monospace for anything that counts (tabular figures, so a changing number
// does not shift its neighbours), and a sans for labels only. The roles are named for the
// job, not the typeface, so a project can fill them with whatever it likes.
using TMPro;
using UnityEngine;
using Ursine.Theming;

namespace Ursine.Text
{
    public enum TypeRole
    {
        /// <summary>Anything named or written: titles, prose, verb-labelled buttons.</summary>
        Serif,
        /// <summary>The same, italic: blurbs, rate lines, asides.</summary>
        SerifItalic,
        /// <summary>Figures, secondary weight — maximums, caps, dividers.</summary>
        Mono400,
        /// <summary>Figures, the one a row is about.</summary>
        Mono500,
        /// <summary>Figures, heaviest.</summary>
        Mono700,
        /// <summary>Labels, light — small body text inside panels.</summary>
        Label400,
        /// <summary>Labels, medium — names in a table, row labels.</summary>
        Label500,
        /// <summary>Labels, heavy — small-caps heads and captions.</summary>
        Label700
    }

    [CreateAssetMenu(menuName = "Ursine/Type Kit", fileName = "TypeKit")]
    public sealed class TypeKit : ScriptableObject
    {
        public TMP_FontAsset serif;
        public TMP_FontAsset serifItalic;
        public TMP_FontAsset mono400;
        public TMP_FontAsset mono500;
        public TMP_FontAsset mono700;
        public TMP_FontAsset label400;
        public TMP_FontAsset label500;
        public TMP_FontAsset label700;

        /// <summary>Installed by the game, the same way Theme.DefaultPalette is.</summary>
        public static System.Func<TypeKit> Default;

        static TypeKit _i;
        public static TypeKit I
        {
            get
            {
                if (_i == null && Default != null) _i = Default();
                return _i;
            }
            set => _i = value;
        }

        public TMP_FontAsset Face(TypeRole role)
        {
            switch (role)
            {
                case TypeRole.Serif: return serif;
                case TypeRole.SerifItalic: return serifItalic != null ? serifItalic : serif;
                case TypeRole.Mono400: return mono400;
                case TypeRole.Mono500: return mono500 != null ? mono500 : mono400;
                case TypeRole.Mono700: return mono700 != null ? mono700 : mono400;
                case TypeRole.Label400: return label400;
                case TypeRole.Label500: return label500 != null ? label500 : label400;
                case TypeRole.Label700: return label700 != null ? label700 : label400;
            }
            return serif;
        }
    }

    public static class Typeset
    {
        /// <summary>Applies a face, a size and a palette token. Sizes are UI units, which on
        /// a fixed stage are the same number the design specifies.</summary>
        public static TMP_Text Set(TMP_Text t, TypeRole face, float sizePx, int token)
        {
            if (t == null) return null;

            var kit = TypeKit.I;
            if (kit != null)
            {
                var f = kit.Face(face);
                if (f != null) t.font = f;
            }

            t.fontSize = sizePx;
            t.fontStyle = face == TypeRole.SerifItalic ? FontStyles.Italic : FontStyles.Normal;
            t.characterSpacing = 0f;
            t.richText = true;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            SetKerning(t, true);

            var themed = t.GetComponent<ThemedGraphic>();
            if (themed == null) themed = t.gameObject.AddComponent<ThemedGraphic>();
            themed.Bind(token);
            return t;
        }

        /// <summary>Real upper case with tracking, never a synthesised small-caps variant.</summary>
        public static TMP_Text SmallCaps(TMP_Text t, float sizePx, float emTracking, int token,
                                         TypeRole face = TypeRole.Label700)
        {
            Set(t, face, sizePx, token);
            t.text = (t.text ?? string.Empty).ToUpperInvariant();
            t.fontStyle = FontStyles.UpperCase;
            // TMP character spacing is expressed in hundredths of an em.
            t.characterSpacing = emTracking * 100f;
            // Tracked capitals are set without pair kerning. TMP drops the tracking at every
            // pair its kerning table names (AT, LT, AV...), so a tracked word with kerning
            // on comes out with those pairs jammed together and the rest spaced out.
            SetKerning(t, false);
            return t;
        }

        /// <summary>Turns the font's pair kerning on or off for one text.</summary>
        public static void SetKerning(TMP_Text t, bool on)
        {
            var list = new System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag>(t.fontFeatures ?? new System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag>());
            list.Remove(UnityEngine.TextCore.OTL_FeatureTag.kern);
            if (on) list.Add(UnityEngine.TextCore.OTL_FeatureTag.kern);
            t.fontFeatures = list;
        }

        /// <summary>A run of figures, tightened so columns line up.</summary>
        public static TMP_Text Figures(TMP_Text t, TypeRole monoFace, float sizePx, int token,
                                       float emTracking = -0.02f)
        {
            Set(t, monoFace, sizePx, token);
            t.characterSpacing = emTracking * 100f;
            return t;
        }

        public static TMP_Text Wrap(TMP_Text t, float lineHeightMultiple = 1f)
        {
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            if (lineHeightMultiple > 0f) t.lineSpacing = (lineHeightMultiple - 1f) * 100f;
            return t;
        }

        /// <summary>CSS line-height on a TMP text: every line advances by the given multiple
        /// of the size, and half the leading sits above the first line and below the last, so
        /// the block is exactly lines x multiple tall, as a browser draws it. Wrap's lineSpacing
        /// is added on top of the font's own line height, which comes out taller than the
        /// spec's number; this subtracts it first. Call after the font and size are set.</summary>
        public static TMP_Text Leading(TMP_Text t, float lineHeightMultiple)
        {
            if (t == null || t.font == null) return t;
            var fi = t.font.faceInfo;
            if (fi.pointSize <= 0f) return t;
            float natural = fi.lineHeight / fi.pointSize;
            float content = (fi.ascentLine - fi.descentLine) / fi.pointSize;
            t.lineSpacing = (lineHeightMultiple - natural) * 100f;
            float half = (lineHeightMultiple - content) * 0.5f * t.fontSize;
            var m = t.margin;
            m.y = half;
            m.w = half;
            t.margin = m;
            return t;
        }

        public static TMP_Text Align(TMP_Text t, TextAlignmentOptions a)
        {
            t.alignment = a;
            return t;
        }
    }
}
