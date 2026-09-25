// What Lies In The Depths — indexing the converted spec art so it can be found by id.
using System.IO;
using UnityEditor;
using UnityEngine;
using Ursine.EditorTools;
using Ursine.UI;
using WhatLiesInTheDepths.Core;

namespace WhatLiesInTheDepths.EditorTools
{
    public static class ArtFactory
    {
        public const string GlyphsDir = "Assets/Art/Glyphs";
        public const string SpecDir = "Assets/Art/Spec";

        public static SpriteSet Build()
        {
            // The PNGs arrive as files, not assets; import them as sprites before indexing.
            AssetDatabase.Refresh();
            int a = SpriteImport.ApplySpriteSettings(GlyphsDir);
            int b = SpriteImport.ApplySpriteSettings(SpecDir);

            Directory.CreateDirectory(AssetFactory.ResourcesDir);
            string path = $"{AssetFactory.ResourcesDir}/Art.asset";
            var set = AssetDatabase.LoadAssetAtPath<SpriteSet>(path);
            bool created = set == null;
            if (created) set = ScriptableObject.CreateInstance<SpriteSet>();

            set.Clear();
            int g = SpriteImport.Index(set, GlyphsDir, "Glyphs");
            int s = SpriteImport.Index(set, SpecDir, "Spec");

            if (created) AssetDatabase.CreateAsset(set, path);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            Debug.Log($"[What Lies In The Depths] Art indexed: {g} glyphs, {s} plates " +
                      $"({a + b} re-imported as sprites) -> {path}");
            return set;
        }
    }
}
