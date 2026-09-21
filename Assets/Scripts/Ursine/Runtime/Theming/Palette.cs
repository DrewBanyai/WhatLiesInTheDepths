// Ursine — a whole token set. Tokens are indices; their names belong to the game.
using System;
using UnityEngine;

namespace Ursine.Theming
{
    [CreateAssetMenu(menuName = "Ursine/Palette", fileName = "Palette")]
    public sealed class Palette : ScriptableObject
    {
        [Tooltip("Shown to the player wherever palettes are chosen.")]
        public string displayName = "Palette";

        [Tooltip("One entry per token, in the game's own token order.")]
        public Color[] colors = new Color[0];

        public int Count => colors != null ? colors.Length : 0;

        public Color Get(int token)
        {
            if (colors == null || token < 0 || token >= colors.Length) return Color.magenta;
            return colors[token];
        }

        public void Set(int token, Color c)
        {
            if (colors == null || token < 0 || token >= colors.Length) return;
            colors[token] = c;
        }

        public void Resize(int count)
        {
            var next = new Color[count];
            if (colors != null)
                Array.Copy(colors, next, Mathf.Min(count, colors.Length));
            colors = next;
        }

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            Debug.LogWarning($"[Ursine] Bad hex '{hex}'.");
            return Color.magenta;
        }
    }
}
