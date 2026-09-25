// What Lies In The Depths — the palettes, the type kit and the TMP font assets.
// The twenty-eight tokens are copied from the :root block that is identical across the
// whole spec set. Dusk and Parchment are whole token sets of their own, not filters:
// the token names describe a role, not a lightness, so on Dusk --iris-l (an iris ground)
// becomes dark and --iris-d (iris text) becomes light. Spec section 10.
using System.IO;
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using Ursine.EditorTools;
using TypeKit = Ursine.Text.TypeKit;
using Palette = Ursine.Theming.Palette;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class AssetFactory
    {
        public const string ResourcesDir = "Assets/Resources/WhatLiesInTheDepths";
        public const string FontsDir = "Assets/Fonts";

        // ---- palettes ----------------------------------------------------------

        /// <summary>The default set, exactly as specified. Twenty-eight values, in Tok order.</summary>
        static readonly string[] Dream =
        {
            "#F2EEF8", "#FCFAFE", "#E3DBEE", "#EDE7F4", "#E9E3F1",
            "#39324D", "#453D5E", "#6C6486", "#9D95B2", "#C4BCD4",
            "#9C86CE", "#6E549F", "#F1ECFA", "#C6B7E4",
            "#6FB3AB", "#3F7F77", "#EAF5F3",
            "#D3849F", "#A4506C", "#FBEFF3", "#F2DCE4", "#FBF0F4",
            "#D6AC6E", "#8A6A2F", "#FAF2E6", "#EEDCBB",
            "#F8F5FC", "#EEE9F6"
        };

        /// <summary>The same dream at night. PLACEHOLDER VALUES — the brief's section 9 says
        /// the chooser is the mechanism and the values in it still need checking against a
        /// contrast standard. Nobody has audited these.</summary>
        static readonly string[] Dusk =
        {
            "#191524", "#211C30", "#332B47", "#2A2439", "#2F2842",
            "#EDE8F7", "#D9D2EA", "#AFA6C6", "#7E7596", "#544B6B",
            "#9C86CE", "#C6B7E4", "#2E2545", "#4B3E6B",
            "#6FB3AB", "#8FCFC7", "#1E3330", "#D3849F", "#E9A8BD", "#3A2430",
            "#5A3A48", "#2E1E28", "#D6AC6E", "#E7C894", "#33291A", "#5A4526",
            "#241E33", "#2B2440"
        };

        /// <summary>Warm paper with more separation by default. PLACEHOLDER VALUES, as above.</summary>
        static readonly string[] Parchment =
        {
            "#F4EFE4", "#FDFAF3", "#E0D6C2", "#EAE1D1", "#E6DCC9",
            "#332B21", "#463C2E", "#6B5F4D", "#9A8C76", "#C4B8A2",
            "#8A6FBE", "#5E4690", "#EFE9F8", "#BFAEDD",
            "#5E9E95", "#33736A", "#E4F0ED", "#C97189", "#94405C", "#F8E8EC",
            "#EED2DA", "#F9EDF0", "#C79A54", "#75561F", "#F6EDDB", "#E3CEA6",
            "#F7F2E7", "#EAE2D3"
        };

        public static Palette BuildPalette(string name, string[] hexes)
        {
            Directory.CreateDirectory(ResourcesDir);
            string path = $"{ResourcesDir}/Palette_{name}.asset";

            var p = AssetDatabase.LoadAssetAtPath<Palette>(path);
            bool created = p == null;
            if (created) p = ScriptableObject.CreateInstance<Palette>();

            p.displayName = name;
            // Ursine does not know how many tokens a game has; Theme.TokenCount does.
            p.Resize(Theme.TokenCount);
            for (int i = 0; i < Theme.TokenCount && i < hexes.Length; i++)
                p.Set(i, Palette.Hex(hexes[i]));

            if (created) AssetDatabase.CreateAsset(p, path);
            EditorUtility.SetDirty(p);
            return p;
        }

        public static Palette[] BuildPalettes()
        {
            var a = BuildPalette("Dream", Dream);
            var b = BuildPalette("Dusk", Dusk);
            var c = BuildPalette("Parchment", Parchment);
            AssetDatabase.SaveAssets();
            return new[] { a, b, c };
        }

        // ---- fonts -------------------------------------------------------------

        /// <summary>Which face fills which role. The type rule is absolute (spec section
        /// 14): Cormorant for anything named or written, IBM Plex Mono for anything that
        /// counts, Karla for labels only. Tabular figures matter — if the mono face lost
        /// them numbers would jitter as they tick — which is why every figure in the game
        /// is set in a monospaced face, where they are inherent.</summary>
        static TMP_FontAsset Font(string ttfName)
        {
            var asset = FontAssetBuilder.Build(FontsDir, ttfName);
            // The project is in Linear color space; see FontAssetBuilder.Weight.
            FontAssetBuilder.Weight(asset, LinearDilate);
            return asset;
        }

        /// <summary>Chosen by comparing Game view captures with the spec rendered in Chromium:
        /// at .15 the Revelations readout's text reads at about the browser's weight.</summary>
        const float LinearDilate = 0.15f;

        public static TypeKit BuildTypeKit()
        {
            Directory.CreateDirectory(ResourcesDir);
            string path = $"{ResourcesDir}/TypeKit.asset";

            var kit = AssetDatabase.LoadAssetAtPath<TypeKit>(path);
            bool created = kit == null;
            if (created) kit = ScriptableObject.CreateInstance<TypeKit>();

            kit.serif = Font("CormorantGaramond-SemiBold");
            kit.serifItalic = Font("CormorantGaramond-SemiBoldItalic");
            kit.mono400 = Font("IBMPlexMono-Regular");
            kit.mono500 = Font("IBMPlexMono-Medium");
            kit.mono700 = Font("IBMPlexMono-Bold");
            kit.label400 = Font("Karla-Regular");
            kit.label500 = Font("Karla-Medium");
            kit.label700 = Font("Karla-Bold");

            if (created) AssetDatabase.CreateAsset(kit, path);
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            return kit;
        }
    }
}
