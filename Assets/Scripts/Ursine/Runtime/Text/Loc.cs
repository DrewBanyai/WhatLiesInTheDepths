// Ursine — every word a player reads, looked up by key.
//
// One strings file holds a section per language:
//
//   {
//     "en": { "focus.absorb.name": "Absorb", "journal.doorway.p": ["…", "…"] },
//     "es": { "focus.absorb.name": "Absorber" }
//   }
//
// A key missing from the current language falls back to English, and one missing from
// English shows itself as ⟦key⟧ so it cannot go unnoticed. Ursine does not know where the
// file lives: the game installs Source, the same way it installs its palette and type kit.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ursine.Text
{
    public static class Loc
    {
        /// <summary>Installed by the game: returns the strings file's text.</summary>
        public static Func<string> Source;
        /// <summary>Installed by the game: where a missing key is reported.</summary>
        public static Action<string> Warn;

        public const string Fallback = "en";

        /// <summary>Raised when the language changes or the file is loaded again.</summary>
        public static event Action Changed;

        static JsonObject _file;
        static string _language = Fallback;
        static readonly HashSet<string> _warned = new HashSet<string>();

        public static string Language
        {
            get => _language;
            set
            {
                if (string.IsNullOrEmpty(value) || value == _language) return;
                _language = value;
                Changed?.Invoke();
            }
        }

        /// <summary>The language codes the file has a section for.</summary>
        public static IEnumerable<string> Languages
        {
            get { Ensure(); return _file != null ? _file.Keys : (IEnumerable<string>)Array.Empty<string>(); }
        }

        /// <summary>The file as it stands, for tools that add to it.</summary>
        public static JsonObject File { get { Ensure(); return _file; } }

        /// <summary>Loads (or reloads) from text rather than from <see cref="Source"/>.</summary>
        public static void Load(string json)
        {
            _file = MiniJson.Parse(json) as JsonObject ?? new JsonObject();
            _warned.Clear();
            Changed?.Invoke();
        }

        /// <summary>Forgets the file, so the next lookup reads <see cref="Source"/> again.</summary>
        public static void Reload()
        {
            _file = null;
            _warned.Clear();
            Changed?.Invoke();
        }

        static void Ensure()
        {
            if (_file != null) return;
            string text = null;
            try { text = Source?.Invoke(); }
            catch (Exception e) { Warn?.Invoke("[Loc] could not read the strings file: " + e.Message); }
            if (string.IsNullOrEmpty(text)) { _file = new JsonObject(); return; }
            try { _file = MiniJson.Parse(text) as JsonObject ?? new JsonObject(); }
            catch (Exception e)
            {
                Warn?.Invoke("[Loc] the strings file is not valid JSON: " + e.Message);
                _file = new JsonObject();
            }
        }

        static object Raw(string key)
        {
            Ensure();
            if (string.IsNullOrEmpty(key)) return null;
            if (_file[_language] is JsonObject lang && lang.Has(key)) return lang[key];
            if (_language != Fallback && _file[Fallback] is JsonObject en && en.Has(key)) return en[key];
            return null;
        }

        public static bool Has(string key) => Raw(key) != null;

        /// <summary>The text for a key. An array reads as its lines joined by blank lines.</summary>
        public static string T(string key)
        {
            var v = Raw(key);
            if (v is string s) return s;
            if (v is List<object> a) return string.Join("\n\n", a);
            if (v != null) return Convert.ToString(v, CultureInfo.InvariantCulture);
            if (_warned.Add(key)) Warn?.Invoke($"[Loc] missing string '{key}'");
            return "⟦" + key + "⟧";
        }

        /// <summary>The text for a key, with {0}, {1}… filled in.</summary>
        public static string T(string key, params object[] args)
        {
            string f = T(key);
            try { return string.Format(CultureInfo.InvariantCulture, f, args); }
            catch (FormatException) { return f; }
        }

        /// <summary>The text for a key if there is one, and the given English otherwise. For
        /// tools that are moving existing text into the file.</summary>
        public static string Or(string key, string fallback)
        {
            var v = Raw(key);
            if (v is string s) return s;
            return fallback;
        }

        /// <summary>A key holding a list — paragraphs, effect lines. A single string is a list of one.</summary>
        public static List<string> Lines(string key)
        {
            var v = Raw(key);
            var list = new List<string>();
            if (v is List<object> a) { foreach (var x in a) list.Add(Convert.ToString(x, CultureInfo.InvariantCulture)); }
            else if (v is string s) list.Add(s);
            else { if (_warned.Add(key)) Warn?.Invoke($"[Loc] missing string '{key}'"); }
            return list;
        }

        /// <summary>The nth entry of a list key (0-based), or null when there is none.</summary>
        public static string Item(string key, int n)
        {
            var v = Raw(key);
            if (v is List<object> a && n >= 0 && n < a.Count) return Convert.ToString(a[n], CultureInfo.InvariantCulture);
            return null;
        }
    }
}
