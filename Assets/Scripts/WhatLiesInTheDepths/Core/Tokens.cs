// What Lies In The Depths — the color tokens, and the game's face on Ursine's theming.
//
// Spec: _Whole Screen.html section 13. Thirty-seven tokens, and nothing in the UI may use
// a color that is not one of them. Ursine holds palettes as an indexed array because it
// cannot know what a game's tokens mean; this file is where they get their names back, so
// a call site still reads Theme.Get(Tok.Ink).
using System;
using System.Collections.Generic;
using UnityEngine;
using UrsineTheme = Ursine.Theming.Theme;
using Palette = Ursine.Theming.Palette;
using TypeKit = Ursine.Text.TypeKit;

namespace WhatLiesInTheDepths.Core
{
    /// <summary>The tokens, in the order of section 13 (the last five added for dark palettes). The five semantic hues
    /// matter more than the hexes: iris is the player's own agency, teal is gain, rose is
    /// loss or shortfall, gold is a ceiling or the greater tier, blue is a choice — one of a
    /// pair that withdraws the other. Blue was added last, so it sits at the end: every
    /// index before it is unchanged.</summary>
    public enum Tok
    {
        Mist = 0, Veil, Haze, Haze2, Wait,
        Ink, Prose, Ink2, Ink3, Ink4,
        Iris, IrisD, IrisL, IrisB,
        Teal, TealD, TealL,
        Rose, RoseD, RoseL, RoseB, RoseT,
        Gold, GoldD, GoldL, GoldB,
        Block, Track,
        Blue, BlueD, BlueL, BlueB,
        // Added for palettes that are not light. Each is appended, so no earlier index moves.
        /// <summary>A surface raised above Veil: a lit or hovered card. White in Dream.</summary>
        Lit,
        /// <summary>Text and marks on a filled iris ground (a primary button).</summary>
        OnIris,
        /// <summary>Iris, pressed deeper: a lit button's progress fill, a filled hover.</summary>
        IrisDeep,
        /// <summary>Multiplied over opaque artwork (plates, portraits, place art) so a dark
        /// palette sees the same pictures by night. White in Dream: the art as drawn.</summary>
        Art,
        /// <summary>Multiplied over soft backdrop art (the Mind Palace grounds, blooms, halos,
        /// the road's terrain); may be translucent so a dark palette keeps them faint.</summary>
        ArtGlow
    }

    /// <summary>Orthogonal to the palette: it touches only the four ink steps and the three
    /// rules, plus the four semantic text colors at Hard. It never changes a hue and never
    /// changes a size, so no layout can move. Spec section 10.</summary>
    public enum Contrast { Soft = 0, Firm = 1, Hard = 2 }

    /// <summary>The game's typed view of the live token set.</summary>
    public static class Theme
    {
        public const int TokenCount = 37;

        public const string PaletteResourceFolder = "WhatLiesInTheDepths";

        public static event Action Changed
        {
            add => UrsineTheme.Changed += value;
            remove => UrsineTheme.Changed -= value;
        }

        public static Palette Current => UrsineTheme.Current;

        public static Contrast Contrast
        {
            get => (Contrast)UrsineTheme.Contrast;
            set => UrsineTheme.Contrast = (int)value;
        }

        /// <summary>A palette is a whole token set, not a filter. Switching one rewrites all
        /// thirty-two values at once, and anything written as a literal will not follow.</summary>
        public static void Use(Palette p) => UrsineTheme.Use(p);

        public static Color Get(Tok t) => UrsineTheme.Get((int)t);
        public static Color Get(Tok t, float alpha) => UrsineTheme.Get((int)t, alpha);

        /// <summary>A color between two tokens. For the few spec colors that are not a token
        /// themselves (a deeper tile, a teal edge, a disabled ink): written as a mix of the
        /// tokens they sit between, they follow a palette change instead of staying as drawn
        /// for the light one.</summary>
        public static Color Mix(Tok a, Tok b, float t) => Color.Lerp(Get(a), Get(b), t);

        /// <summary>True when the page itself is dark (Dusk).</summary>
        public static bool IsDark
        {
            get
            {
                var c = Get(Tok.Mist);
                return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b < 0.4f;
            }
        }

        /// <summary>A token the palette asset does not have yet reads as its Dream value, so a
        /// palette made before the token was added looks as it always did until rebuilt.</summary>
        static Color Missing(int token)
        {
            switch ((Tok)token)
            {
                case Tok.Lit: return Color.white;
                case Tok.OnIris: return UrsineTheme.Get((int)Tok.Veil);
                case Tok.IrisDeep: return new Color32(0x7C, 0x61, 0xAE, 0xFF);
                case Tok.Art: return Color.white;
                case Tok.ArtGlow: return Color.white;
                default: return Color.magenta;
            }
        }

        // ---- the rules Ursine cannot know --------------------------------------

        static readonly HashSet<int> InkSteps = new HashSet<int>
        { (int)Tok.Ink, (int)Tok.Prose, (int)Tok.Ink2, (int)Tok.Ink3, (int)Tok.Ink4 };

        static readonly HashSet<int> Rules = new HashSet<int>
        { (int)Tok.Haze, (int)Tok.Haze2, (int)Tok.Wait };

        static readonly HashSet<int> Semantic = new HashSet<int>
        { (int)Tok.IrisD, (int)Tok.TealD, (int)Tok.RoseD, (int)Tok.GoldD, (int)Tok.BlueD };

        static Color ApplyContrast(int token, int level, Color c)
        {
            bool deepen = InkSteps.Contains(token) || Rules.Contains(token)
                          || (level >= (int)Core.Contrast.Hard && Semantic.Contains(token));
            if (!deepen) return c;

            // More contrast means further from the page. On a light page that is darker; on a
            // dark one (Dusk) it is lighter — darkening Dusk's light ink would have lowered
            // contrast instead of raising it.
            float k = level == (int)Core.Contrast.Firm ? 0.12f : 0.26f;
            var page = UrsineTheme.Current != null ? UrsineTheme.Current.Get((int)Tok.Mist) : Color.white;
            bool dark = 0.2126f * page.r + 0.7152f * page.g + 0.0722f * page.b < 0.4f;
            if (dark) return new Color(c.r + (1f - c.r) * k, c.g + (1f - c.g) * k, c.b + (1f - c.b) * k, c.a);
            return new Color(c.r * (1f - k), c.g * (1f - k), c.b * (1f - k), c.a);
        }

        /// <summary>Hands Ursine the two things only this game knows: where its default
        /// palette and type kit live, and what a contrast step does. Runs before the first
        /// scene in a player, and on domain reload in the editor so prefab building works too.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Install()
        {
            UrsineTheme.DefaultPalette = () =>
                Resources.Load<Palette>($"{PaletteResourceFolder}/Palette_Dream");

            UrsineTheme.ContrastFilter = ApplyContrast;
            UrsineTheme.MissingToken = Missing;

            Ursine.Text.TypeKit.Default = () =>
                Resources.Load<Ursine.Text.TypeKit>($"{PaletteResourceFolder}/TypeKit");
        }
    }
}
