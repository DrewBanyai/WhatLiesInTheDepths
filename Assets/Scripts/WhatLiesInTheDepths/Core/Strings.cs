// What Lies In The Depths — where the words live.
//
// Every word the game shows is in Assets/Resources/WhatLiesInTheDepths/Strings.json, under a
// section per language ("en" first). This installs that file as Ursine's Loc source, at
// startup and in the editor alike, so builders and views read the same words.
using UnityEngine;
using Ursine.Text;

namespace WhatLiesInTheDepths.Core
{
    public static class Strings
    {
        /// <summary>Resources path of the strings file, without its extension.</summary>
        public const string ResourcePath = "WhatLiesInTheDepths/Strings";
        /// <summary>The same file as an asset path, for editor tools that write to it.</summary>
        public const string AssetPath = "Assets/Resources/WhatLiesInTheDepths/Strings.json";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        public static void Install()
        {
            Loc.Source = () =>
            {
                var asset = Resources.Load<TextAsset>(ResourcePath);
                return asset != null ? asset.text : null;
            };
            Loc.Warn = m => Debug.LogWarning("[What Lies In The Depths] " + m);
        }

        /// <summary>A small number in words ("three"), from the strings file's own list; past the
        /// end of the list, the figure.</summary>
        public static string Number(int n) => Loc.Item("ui.numbers", n) ?? Ursine.Fmt.Count(n);

        /// <summary>An ordinal in words ("second"), 1-based, from the strings file's own list.</summary>
        public static string Ordinal(int n) => Loc.Item("ui.ordinals", n - 1) ?? Suffixed(n);

        /// <summary>"13th", "21st", "102nd": past the list's words, the figure with its suffix.</summary>
        static string Suffixed(int n)
        {
            int t = n % 100, u = n % 10;
            string s = (t >= 11 && t <= 13) ? "th" : u == 1 ? "st" : u == 2 ? "nd" : u == 3 ? "rd" : "th";
            return n + s;
        }

        /// <summary>Shorthand for views.</summary>
        public static string T(string key) => Loc.T(key);
        public static string T(string key, params object[] args) => Loc.T(key, args);
    }
}
