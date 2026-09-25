// Ursine — TextMeshPro font assets from TTFs in the project.
//
// A dynamic atlas rasterizes glyphs as they are first used, which keeps a web build's
// download down. Subsetting the faces is still worth doing before shipping one.
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Ursine.EditorTools
{
    public static class FontAssetBuilder
    {
        /// <summary>Builds "<name> SDF.asset" beside the TTF, or returns the existing one.</summary>
        public static TMP_FontAsset Build(string fontsDir, string ttfName,
                                          int samplingPointSize = 90, int padding = 9,
                                          int atlasWidth = 1024, int atlasHeight = 1024)
        {
            string ttf = $"{fontsDir}/{ttfName}.ttf";
            string outPath = $"{fontsDir}/{ttfName} SDF.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null)
            {
                // The TTF changed under a built asset (a fixed font, say): drop what the
                // asset learned from the old file — glyphs, atlas, kerning — so it is
                // relearned from the new one as characters are next used.
                if (System.IO.File.GetLastWriteTimeUtc(ttf) > System.IO.File.GetLastWriteTimeUtc(outPath))
                {
                    existing.ClearFontAssetData(true);
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Ursine] {ttfName} changed; its font asset was cleared to relearn it.");
                }
                return existing;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
            if (font == null)
            {
                Debug.LogError($"[Ursine] Missing font {ttf}.");
                return null;
            }

            var asset = TMP_FontAsset.CreateFontAsset(
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
            return asset;
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
