// Ursine — a runtime-swappable color palette.
//
// A palette is a whole token set, not a filter. Tokens are indices here rather than an
// enum, because which tokens exist and what they mean is a decision each game makes;
// Ursine only knows how many there are, how to swap them all at once, and how to tell
// everything that draws that they changed.
//
// A game defines its own enum and casts: Theme.Get((int)Tok.Ink). Most games will want a
// thin typed facade over this so call sites stay readable.
using System;
using UnityEngine;

namespace Ursine.Theming
{
    /// <summary>The live token set. Everything that draws asks Theme, never a constant —
    /// a color written as a literal will not follow a palette change.</summary>
    public static class Theme
    {
        /// <summary>Raised whenever the palette or the contrast step changes.</summary>
        public static event Action Changed;

        /// <summary>Installed by the game so the first Get has something to read, in play
        /// mode and in the editor alike. Set it from a [RuntimeInitializeOnLoadMethod] and
        /// an [InitializeOnLoadMethod] in the game assembly.</summary>
        public static Func<Palette> DefaultPalette;

        /// <summary>Installed by the game. Ursine cannot know which tokens are ink, which
        /// are rules and which are semantic, so the game decides what a contrast step does.
        /// It is handed the token, the current step and the palette color, and returns the
        /// color to use. A contrast step must never change a hue or a size, so that no
        /// layout can move.</summary>
        public static Func<int, int, Color, Color> ContrastFilter;

        static Palette _palette;
        static int _contrast;

        public static Palette Current
        {
            get
            {
                if (_palette == null && DefaultPalette != null) _palette = DefaultPalette();
                return _palette;
            }
        }

        /// <summary>The contrast step, as the game numbers them. 0 is "as drawn".</summary>
        public static int Contrast
        {
            get => _contrast;
            set { if (_contrast == value) return; _contrast = value; Changed?.Invoke(); }
        }

        public static void Use(Palette p)
        {
            if (p == null || p == _palette) return;
            _palette = p;
            Changed?.Invoke();
        }

        /// <summary>Re-raises Changed without altering anything. For an editor tool that has
        /// rewritten a palette's values in place.</summary>
        public static void Refresh() => Changed?.Invoke();

        public static Color Get(int token)
        {
            var p = Current;
            if (p == null) return Color.magenta;
            var c = p.Get(token);
            if (_contrast == 0 || ContrastFilter == null) return c;
            return ContrastFilter(token, _contrast, c);
        }

        public static Color Get(int token, float alpha)
        {
            var c = Get(token);
            c.a = alpha;
            return c;
        }
    }
}
