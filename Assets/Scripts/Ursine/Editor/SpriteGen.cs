// Ursine — generated placeholder sprites.
//
// Unity UI has no box-shadow, so soft shadows are 9-sliced sprites and spread-only halos
// are a second image behind at a slightly larger rect. Rounded corners are a 9-sliced
// sprite per radius. Gradients are textures, not code.
//
// Everything here writes a PNG and sets its import settings. What sprites a project needs,
// and what they are called, is the project's business — this only knows how to draw them.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ursine.EditorTools
{
    public static class SpriteGen
    {
        // ---- rounded rectangles ------------------------------------------------

        /// <summary>A filled rounded rectangle, 9-sliced so it stretches to any size.
        /// Pass radius 999 or more for a pill, which slices into a capsule at any height.</summary>
        public static void RoundedRect(string dir, string name, int radius)
        {
            int r = radius >= 999 ? 32 : radius;
            int size = r * 2 + 4;
            var tex = New(size, size);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = EdgeDistance(x + 0.5f, y + 0.5f, size, size, r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
            }

            Write(tex, dir, name, new Vector4(r + 1, r + 1, r + 1, r + 1));
        }

        /// <summary>A one-pixel outline of the same shape, for a border that has to sit on
        /// a ground it does not own.</summary>
        public static void RoundedOutline(string dir, string name, int radius, float thickness = 1f)
        {
            int r = radius >= 999 ? 32 : radius;
            int size = r * 2 + 4;
            var tex = New(size, size);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = EdgeDistance(x + 0.5f, y + 0.5f, size, size, r);
                float a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + thickness));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }

            Write(tex, dir, name, new Vector4(r + 1, r + 1, r + 1, r + 1));
        }

        /// <summary>Signed distance from a rounded rectangle's edge. Negative inside.</summary>
        static float EdgeDistance(float x, float y, int w, int h, float r)
        {
            float dx = Mathf.Max(Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r), 0f);
            float dy = Mathf.Max(Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r), 0f);
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        // ---- shadows -----------------------------------------------------------

        /// <summary>A soft shadow as a 9-sliced sprite. The caller tints it with a token and
        /// offsets the rect; this supplies only the falloff.</summary>
        public static void SoftShadow(string dir, string name, int blur)
        {
            int pad = Mathf.Max(4, blur);
            const int core = 8;
            int size = core + pad * 2;
            var tex = New(size, size);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - core * 0.5f);
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - core * 0.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d / Mathf.Max(1f, blur));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));   // an ease, not a cone
            }

            Write(tex, dir, name, new Vector4(pad + 2, pad + 2, pad + 2, pad + 2));
        }

        // ---- gradients ---------------------------------------------------------

        /// <summary>A radial bloom, for a page ground that is lit from one corner.</summary>
        public static void RadialBloom(string dir, string name, int w, int h,
                                       Vector2 origin, Vector2 spread, float reach = 0.6f)
        {
            var tex = New(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x / (float)w, y / (float)h);
                var d = new Vector2((p.x - origin.x) / spread.x, (p.y - origin.y) / spread.y);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d.magnitude / reach)));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        /// <summary>A vertical scrim, for text that has to stay legible over artwork.</summary>
        public static void LinearScrim(string dir, string name, int w, int h, bool downward)
        {
            var tex = New(w, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                float a = downward ? t : 1f - t;
                a *= a;
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        // ---- marks -------------------------------------------------------------

        public static void Ring(string dir, string name, int size, int thickness)
        {
            var tex = New(size, size);
            float outer = size * 0.5f - 1f;
            float inner = outer - thickness;
            var c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner + 1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        public static void DashedRing(string dir, string name, int size,
                                      float dashDegrees = 7f, float periodDegrees = 12f)
        {
            var tex = New(size, size);
            float outer = size * 0.5f - 1f;
            float inner = outer - 1.5f;
            var c = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, c);
                float ang = Mathf.Atan2(p.y - c.y, p.x - c.x) * Mathf.Rad2Deg;
                bool on = Mathf.Repeat(ang, periodDegrees) < dashDegrees;
                float a = on ? Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner + 1f) : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        public static void Disc(string dir, string name, int size)
        {
            var tex = New(size, size);
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size * 0.5f - 1f - d)));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        /// <summary>A flat stand-in at a given aspect, so a layout can be judged before any
        /// art exists. Drawn small and stretched — these are placeholders, not deliverables.</summary>
        public static void FlatPlaceholder(string dir, string name, int w, int h)
        {
            int tw = Mathf.Max(8, w / 4), th = Mathf.Max(8, h / 4);
            var tex = New(tw, th);
            for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                float t = (x / (float)tw) * 0.6f + (1f - y / (float)th) * 0.4f;
                float band = Mathf.Repeat((x + y) / 18f, 1f) < 0.5f ? 0.02f : 0f;
                float v = Mathf.Lerp(0.90f, 0.80f, t) + band;
                tex.SetPixel(x, y, new Color(v, v * 0.985f, v, 1f));
            }
            Write(tex, dir, name, Vector4.zero);
        }

        // ---- plumbing ----------------------------------------------------------

        static Texture2D New(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(new Color[w * h]);
            return t;
        }

        static void Write(Texture2D tex, string dir, string name, Vector4 border)
        {
            tex.Apply();
            Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = border;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }

        public static Sprite Load(string dir, string name)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{name}.png");
    }
}
