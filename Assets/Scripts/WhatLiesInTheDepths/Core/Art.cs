// What Lies In The Depths — finding the art for a thing by its id.
//
// Every glyph and plate here was converted from the spec set's own inline SVG — the resource
// icons, tab glyphs, Focus task glyphs, Revelation sigils, Vision glyphs, construct and
// palace forms, place and unit glyphs, and the plates that composite them over a sky. The
// spec calls all of it placeholder; replacing a PNG under Assets/Art with real art of the
// same name is the whole hand-off.
//
// Glyphs are drawn white so a token can tint them — "a glyph is always its label's color".
// Plates are drawn in their own colors, because artwork does not follow the palette.
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.Core
{
    public static class Art
    {
        public const string ResourcePath = "WhatLiesInTheDepths/Art";

        static SpriteSet _set;
        public static SpriteSet Set
        {
            get
            {
                if (_set == null) _set = Resources.Load<SpriteSet>(ResourcePath);
                return _set;
            }
        }

        static Sprite Get(string folder, string key) => Set != null ? Set.Get(folder, key) : null;

        // ---- glyphs: white, tinted by a token ----------------------------------
        public static Sprite Resource(string k)  => Get("Glyphs/Resource", k);
        public static Sprite Tab(string k)       => Get("Glyphs/Tab", k);
        public static Sprite Focus(string k)     => Get("Glyphs/Focus", k);
        public static Sprite Sigil(string k)     => Get("Glyphs/Sigil", k);
        public static Sprite Vision(string k)    => Get("Glyphs/Vision", k);
        public static Sprite Construct(string k) => Get("Glyphs/Construct", k);
        public static Sprite Place(string k)     => Get("Glyphs/Place", k);
        public static Sprite Unit(string k)      => Get("Glyphs/Unit", k);
        public static Sprite Achievement(string k) => Get("Glyphs/Achievement", k);

        /// <summary>The Journal's four source marks — a Revelation, a Vision, a battle, a veil.</summary>
        public static Sprite Source(string k)    => Get("Glyphs/Source", k);

        // ---- artwork: its own colors, never tinted ----------------------------
        /// <summary>A construct's form on the Mind Palace map. A second asset, with pale
        /// fills and a ground line — not the card glyph scaled up.</summary>
        public static Sprite MapForm(string k)   => Get("Spec/Map", k);

        /// <summary>A construct card's plate: its section's sky, with its own glyph on it.</summary>
        public static Sprite ConstructPlate(string k) => Get("Spec", "Plate_Construct_" + k);

        /// <summary>A road location's art for the Assault reading band.</summary>
        public static Sprite PlaceArt(string k)  => Get("Spec", "Art_Place_" + k);

        /// <summary>A unit's portrait. A unit is a figure, not a place.</summary>
        public static Sprite Portrait(string k)  => Get("Spec", "Portrait_Unit_" + k);

        public static Sprite Spec(string name)   => Get("Spec", name);
        /// <summary>Interface marks: arrow, mark, check, cross, sword.</summary>
        public static Sprite Ui(string k)        => Get("Glyphs/Ui", k);

        /// <summary>Puts a sprite on an image if there is one, and leaves what was there if
        /// not — content can outrun the artist, and a missing glyph should not blank a row.</summary>
        public static void Apply(Image img, Sprite s)
        {
            if (img != null && s != null) img.sprite = s;
        }
    }
}
