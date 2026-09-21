// Ursine — import a folder of PNGs as sprites, and index them by name.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ursine.UI;

namespace Ursine.EditorTools
{
    public static class SpriteImport
    {
        /// <summary>Forces every PNG under a folder to import as a sprite. Artwork drawn at
        /// 2x and displayed at half needs no mipmaps and should not be point-filtered.</summary>
        public static int ApplySpriteSettings(string folder, bool overrideCompression = true)
        {
            if (!Directory.Exists(folder)) return 0;
            int n = 0;
            foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                var unityPath = path.Replace('\\', '/');
                var imp = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (imp == null) continue;

                bool dirty = false;
                if (imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; dirty = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single)
                { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
                if (imp.filterMode != FilterMode.Bilinear)
                { imp.filterMode = FilterMode.Bilinear; dirty = true; }
                if (imp.wrapMode != TextureWrapMode.Clamp)
                { imp.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (overrideCompression && imp.textureCompression != TextureImporterCompression.Uncompressed)
                { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }

                if (!dirty) continue;
                imp.SaveAndReimport();
                n++;
            }
            return n;
        }

        /// <summary>Indexes every sprite under a folder into a SpriteSet, keyed by its path
        /// relative to that folder without the extension — Resource/silt, Spec/Art_Eye.</summary>
        public static int Index(SpriteSet set, string folder, string keyPrefix = "")
        {
            if (!Directory.Exists(folder)) return 0;
            int n = 0;
            foreach (var path in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories)
                                          .OrderBy(p => p))
            {
                var unityPath = path.Replace('\\', '/');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
                if (sprite == null) continue;

                var rel = unityPath.Substring(folder.Length).TrimStart('/');
                rel = rel.Substring(0, rel.Length - 4);          // drop .png
                set.Set(string.IsNullOrEmpty(keyPrefix) ? rel : keyPrefix + "/" + rel, sprite);
                n++;
            }
            return n;
        }
    }
}
