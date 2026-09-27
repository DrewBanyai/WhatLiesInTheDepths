// Ursine — TextMeshPro font assets from TTFs in the project.
//
// The assets are dynamic, so a glyph nobody planned for still renders. But a dynamic asset
// that learns a glyph in play mode saves it, and the atlas, the glyph tables and the file
// change with it: every session that shows new text leaves a changed font asset behind.
// So each asset is taught its whole character set up front, when it is built, and only
// relearns when the TTF or the character set changes. After that, play mode has nothing
// to add and the file stays put. A glyph that still turns up at runtime is one the set is
// missing; add it to the set rather than let the atlas pick it up.
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Ursine.EditorTools
{
    public static class FontAssetBuilder
    {
        /// <summary>Printable ASCII, the floor for any character set.</summary>
        public static string Ascii
        {
            get
            {
                var b = new StringBuilder();
                for (char c = ' '; c <= '~'; c++) b.Append(c);
                return b.ToString();
            }
        }

        /// <summary>Every distinct character in <paramref name="sources"/>, in code point
        /// order, leaving out controls. Order and duplicates do not change the result, so the
        /// set only changes when a character is actually added or dropped.</summary>
        public static string CharacterSet(params string[] sources)
        {
            var set = new System.Collections.Generic.SortedSet<int>();
            foreach (var s in sources)
            {
                if (string.IsNullOrEmpty(s)) continue;
                for (int i = 0; i < s.Length; i++)
                {
                    int cp = CodePoint(s, ref i);
                    if (cp < 0x20 || (cp >= 0x7F && cp < 0xA0)) continue;
                    if (cp >= 0xD800 && cp <= 0xDFFF) continue;
                    set.Add(cp);
                }
            }
            var b = new StringBuilder();
            foreach (int cp in set) b.Append(char.ConvertFromUtf32(cp));
            return b.ToString();
        }

        static int CodePoint(string s, ref int i)
            => char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])
                ? char.ConvertToUtf32(s[i], s[++i])
                : s[i];

        /// <summary>Builds "<name> SDF.asset" beside the TTF, or returns the existing one.
        /// With <paramref name="characters"/>, the asset holds exactly those glyphs, and is
        /// cleared and repopulated only when the TTF, the set or the atlas settings change.</summary>
        public static TMP_FontAsset Build(string fontsDir, string ttfName,
                                          int samplingPointSize = 90, int padding = 9,
                                          int atlasWidth = 1024, int atlasHeight = 1024,
                                          string characters = null)
        {
            string ttf = $"{fontsDir}/{ttfName}.ttf";
            string outPath = $"{fontsDir}/{ttfName} SDF.asset";

            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (asset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
                if (font == null)
                {
                    Debug.LogError($"[Ursine] Missing font {ttf}.");
                    return null;
                }

                asset = TMP_FontAsset.CreateFontAsset(
                    font, samplingPointSize, padding, GlyphRenderMode.SDFAA,
                    atlasWidth, atlasHeight, AtlasPopulationMode.Dynamic, true);

                asset.name = ttfName + " SDF";
                AssetDatabase.CreateAsset(asset, outPath);

                if (asset.atlasTextures != null && asset.atlasTextures.Length > 0 && asset.atlasTextures[0] != null)
                {
                    asset.atlasTextures[0].name = ttfName + " Atlas";
                    AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                }
                if (asset.material != null)
                {
                    asset.material.name = ttfName + " Material";
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                }

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            if (characters != null)
                Populate(asset, ttf, outPath, characters, samplingPointSize, padding, atlasWidth, atlasHeight);
            return asset;
        }

        /// <summary>Clears the asset and teaches it <paramref name="characters"/>, unless the
        /// stamp recorded last time says it already knows exactly those, from this TTF.</summary>
        static void Populate(TMP_FontAsset asset, string ttf, string outPath, string characters,
                             int samplingPointSize, int padding, int atlasWidth, int atlasHeight)
        {
            string stamp = Stamp(ttf, characters, samplingPointSize, padding, atlasWidth, atlasHeight);
            var importer = AssetImporter.GetAtPath(outPath);
            if (importer == null || importer.userData == stamp) return;

            var codes = new System.Collections.Generic.List<uint>();
            for (int i = 0; i < characters.Length; i++) codes.Add((uint)CodePoint(characters, ref i));

            asset.ClearFontAssetData(true);
            bool all = asset.TryAddCharacters(codes.ToArray(), out uint[] missing, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            // The stamp lives in the asset's .meta, beside it in version control.
            importer.userData = stamp;
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(outPath);

            string lacking = all || missing == null || missing.Length == 0 ? string.Empty
                : $" The face has no glyph for: {string.Join(" ", missing.Select(c => $"U+{c:X4}"))}.";
            Debug.Log($"[Ursine] {asset.name}: rebuilt with {codes.Count} characters.{lacking}");
        }

        /// <summary>What the asset was built from: the TTF's bytes, the characters and the
        /// atlas settings. A timestamp would change on a checkout; the content does not.</summary>
        static string Stamp(string ttf, string characters, int samplingPointSize, int padding,
                            int atlasWidth, int atlasHeight)
        {
            using (var sha = SHA1.Create())
            {
                var ttfBytes = System.IO.File.Exists(ttf) ? System.IO.File.ReadAllBytes(ttf) : new byte[0];
                var settings = Encoding.UTF8.GetBytes($"|{samplingPointSize}|{padding}|{atlasWidth}x{atlasHeight}|");
                var chars = Encoding.UTF8.GetBytes(characters);
                sha.TransformBlock(ttfBytes, 0, ttfBytes.Length, null, 0);
                sha.TransformBlock(settings, 0, settings.Length, null, 0);
                sha.TransformFinalBlock(chars, 0, chars.Length);
                var b = new StringBuilder("ursine-font:");
                foreach (var x in sha.Hash) b.Append(x.ToString("x2"));
                return b.ToString();
            }
        }

        /// <summary>Thickens a face's default material. uGUI blends in linear space when the
        /// project does, which thins an SDF's anti-aliased edge — small text reads lighter and
        /// lighter-weight than the same face in a browser, which blends in gamma. A small
        /// dilation buys the weight back. Idempotent: it sets, it does not add.</summary>
        public static void Weight(TMP_FontAsset asset, float faceDilate)
        {
            if (asset == null || asset.material == null) return;
            var m = asset.material;
            if (Mathf.Approximately(m.GetFloat(ShaderUtilities.ID_FaceDilate), faceDilate)) return;
            m.SetFloat(ShaderUtilities.ID_FaceDilate, faceDilate);
            ShaderUtilities.UpdateShaderRatios(m);
            EditorUtility.SetDirty(m);
            EditorUtility.SetDirty(asset);
        }
    }
}
