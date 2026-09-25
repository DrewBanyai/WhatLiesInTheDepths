// What Lies In The Depths — which placeholder sprites this game needs, and what they are
// called. How they are drawn is Ursine's business; this file is the catalog.
//
// Everything written here is PLACEHOLDER ART. Replacing a file in Assets/Art/Generated
// with real art of the same name is the whole hand-off — nothing references these by
// anything but path. The artist's brief is _Whole Screen.html section 16.
using UnityEditor;
using UnityEngine;
using Ursine.EditorTools;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class SpriteFactory
    {
        public const string Dir = "Assets/Art/Generated";

        /// <summary>Rounded corners are a 9-sliced sprite per radius. These cover almost
        /// everything in the set: 999 is the pill, 9 the default control, 14 the default
        /// panel. Spec section 4 of the brief.</summary>
        public static readonly int[] Radii = { 7, 9, 10, 11, 12, 13, 14, 16, 999 };

        public static void BuildAll()
        {
            foreach (var r in Radii)
            {
                SpriteGen.RoundedRect(Dir, Name("Round_", r), r);
                SpriteGen.RoundedOutline(Dir, Name("Outline_", r), r);
            }

            // The four shadows of section 4: a panel resting on the ground, a card under
            // the pointer, a filled iris button, and a readout over a field.
            SpriteGen.SoftShadow(Dir, "Shadow_Panel", 34);
            SpriteGen.SoftShadow(Dir, "Shadow_Lift", 26);
            SpriteGen.SoftShadow(Dir, "Shadow_Primary", 24);
            SpriteGen.SoftShadow(Dir, "Shadow_Readout", 48);

            // The ground's bloom from the upper left, and the two plate scrims.
            SpriteGen.RadialBloom(Dir, "Bloom_Page", 512, 256,
                                  origin: new Vector2(0.16f, 1.08f),
                                  spread: new Vector2(0.90f, 0.60f));
            SpriteGen.LinearScrim(Dir, "Scrim_Down", 8, 128, downward: true);
            SpriteGen.LinearScrim(Dir, "Scrim_Up", 8, 128, downward: false);

            SpriteGen.DashedRing(Dir, "Ring_Dashed", 100);   // an unbuilt plot
            SpriteGen.Ring(Dir, "Ring_Progress", 80, 3);     // a Vision's rim
            SpriteGen.Ring(Dir, "Ring_Halo", 96, 4);         // a spread-only halo
            SpriteGen.Disc(Dir, "Disc", 64);
            // A sigil's halo: radial-gradient(circle) on a 62 square reaches its farthest
            // corner at 43.8px, so the 68% stop falls 29.8px out — .48 of the width.
            SpriteGen.RadialBloom(Dir, "Halo_Sigil", 128, 128,
                                  origin: new Vector2(0.5f, 0.5f), spread: Vector2.one, reach: 0.48f);
            SpriteGen.Ring(Dir, "Ring_Sigil", 100, 2);
            SpriteGen.Ring(Dir, "Ring_Pip", 48, 6);          // 1.5px at the 12px it is drawn
            SpriteGen.Ring(Dir, "Ring_Pin30", 60, 3);        // 1.5px at a 30 pin
            SpriteGen.Ring(Dir, "Ring_Pin38", 76, 4);        // 2px at a 38 pin
            // A field's center light: linear from full at the middle to nothing at the edge.
            SpriteGen.RadialBloom(Dir, "Bloom_Field", 256, 256,
                                  origin: new Vector2(0.5f, 0.5f), spread: Vector2.one, reach: 0.5f);       // 1px at the 50px it is drawn

            // No plates are generated any more. Every glyph and plate is now converted from
            // the spec set's own SVG and lives in Assets/Art/Glyphs and Assets/Art/Spec;
            // what is generated here is only the UI furniture the spec draws with CSS —
            // rounded corners, shadows, blooms, scrims, rings and discs.

            AssetDatabase.Refresh();
            Debug.Log($"[What Lies In The Depths] Placeholder sprites written to {Dir}.");
        }

        static string Name(string prefix, int radius)
            => prefix + (radius >= 999 ? "Pill" : radius.ToString());

        public const string SpecDir = "Assets/Art/Spec";
        public const string GlyphsDir = "Assets/Art/Glyphs";

        /// <summary>The converted spec artwork first, then the generated furniture, so a
        /// builder can ask for "Art_Eye" or "Round_14" without knowing which it is.
        /// Spec art must win: a stand-in of the same name left behind in Art/Generated would
        /// otherwise shadow the real drawing, and did.</summary>
        public static Sprite Load(string name)
        {
            var s = SpriteGen.Load(SpecDir, name);
            return s != null ? s : SpriteGen.Load(Dir, name);
        }

        /// <summary>A converted spec glyph, by family and key: Glyph("Tab", "focus").
        /// Null when the spec never drew one — the caller decides what that looks like.</summary>
        public static Sprite Glyph(string family, string key)
            => string.IsNullOrEmpty(key) ? null : SpriteGen.Load($"{GlyphsDir}/{family}", key);

        public static Sprite Round(int radius) => Load(Name("Round_", radius));
        public static Sprite Outline(int radius) => Load(Name("Outline_", radius));
    }
}
