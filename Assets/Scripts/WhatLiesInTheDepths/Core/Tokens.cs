// What Lies In The Depths — the color tokens, and the game's face on Ursine's theming.
//
// Spec: _Whole Screen.html section 13. Twenty-eight tokens, and nothing in the UI may use
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
    /// <summary>The twenty-eight tokens, in the order of section 13. The four semantic hues
    /// matter more than the hexes: iris is the player's own agency, teal is gain, rose is
    /// loss or shortfall, gold is a ceiling or the greater tier.</summary>
    public enum Tok
    {
        Mist = 0, Veil, Haze, Haze2, Wait,
        Ink, Prose, Ink2, Ink3, Ink4,
        Iris, IrisD, IrisL, IrisB,
        Teal, TealD, TealL,
        Rose, RoseD, RoseL, RoseB, RoseT,
        Gold, GoldD, GoldL, GoldB,
        Block, Track
    }

    /// <summary>Orthogonal to the palette: it touches only the four ink steps and the three
    /// rules, plus the four semantic text colors at Hard. It never changes a hue and never
    /// changes a size, so no layout can move. Spec section 10.</summary>
    public enum Contrast { Soft = 0, Firm = 1, Hard = 2 }

    /// <summary>The game's typed view of the live token set.</summary>
    public static class Theme
    {
        public const int TokenCount = 28;

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
        /// twenty-eight values at once, and anything written as a literal will not follow.</summary>
        public static void Use(Palette p) => UrsineTheme.Use(p);

        public static Color Get(Tok t) => UrsineTheme.Get((int)t);
        public static Color Get(Tok t, float alpha) => UrsineTheme.Get((int)t, alpha);

        // ---- the rules Ursine cannot know --------------------------------------

        static readonly HashSet<int> InkSteps = new HashSet<int>
        { (int)Tok.Ink, (int)Tok.Prose, (int)Tok.Ink2, (int)Tok.Ink3, (int)Tok.Ink4 };

        static readonly HashSet<int> Rules = new HashSet<int>
        { (int)Tok.Haze, (int)Tok.Haze2, (int)Tok.Wait };

        static readonly HashSet<int> Semantic = new HashSet<int>
        { (int)Tok.IrisD, (int)Tok.TealD, (int)Tok.RoseD, (int)Tok.GoldD };

        static Color ApplyContrast(int token, int level, Color c)
        {
            bool deepen = InkSteps.Contains(token) || Rules.Contains(token)
                          || (level >= (int)Core.Contrast.Hard && Semantic.Contains(token));
            if (!deepen) return c;

            float k = level == (int)Core.Contrast.Firm ? 0.12f : 0.26f;
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

            Ursine.Text.TypeKit.Default = () =>
                Resources.Load<Ursine.Text.TypeKit>($"{PaletteResourceFolder}/TypeKit");
        }
    }
}
