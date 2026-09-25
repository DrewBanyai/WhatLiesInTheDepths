// Ursine — a small JSON reader and writer.
//
// Unity's JsonUtility cannot read a dictionary, and a strings file is nothing but
// dictionaries. This reads objects (in their written order), arrays, strings, numbers,
// booleans and null, and writes them back readably. It is not fast and does not need to be.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ursine.Text
{
    /// <summary>A JSON object that keeps its keys in the order they were written.</summary>
    public sealed class JsonObject
    {
        readonly List<string> _order = new List<string>();
        readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public IReadOnlyList<string> Keys => _order;
        public int Count => _order.Count;
        public bool Has(string key) => _values.ContainsKey(key);

        public object this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set
            {
                if (!_values.ContainsKey(key)) _order.Add(key);
                _values[key] = value;
            }
        }
    }

    public static class MiniJson
    {
        // ---- reading ----------------------------------------------------------------

        public static object Parse(string json)
        {
            if (json == null) return null;
            int i = 0;
            var v = Value(json, ref i);
            Skip(json, ref i);
            if (i < json.Length) throw Error(json, i, "unexpected text after the end");
            return v;
        }

        static Exception Error(string s, int i, string what)
        {
            int line = 1;
            for (int k = 0; k < i && k < s.Length; k++) if (s[k] == '\n') line++;
            return new FormatException($"JSON: {what} (line {line})");
        }

        static void Skip(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw Error(s, i, "unexpected end");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (c == 't' && Word(s, ref i, "true")) return true;
            if (c == 'f' && Word(s, ref i, "false")) return false;
            if (c == 'n' && Word(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        static JsonObject Obj(string s, ref int i)
        {
            var o = new JsonObject();
            i++; // {
            Skip(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return o; }
            while (true)
            {
                Skip(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "expected a key");
                string key = Str(s, ref i);
                Skip(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "expected ':'");
                i++;
                o[key] = Value(s, ref i);
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return o; }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var a = new List<object>();
            i++; // [
            Skip(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return a; }
            while (true)
            {
                a.Add(Value(s, ref i));
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return a; }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        static string Str(string s, ref int i)
        {
            var b = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return b.ToString();
                if (c != '\\') { b.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': b.Append('"'); break;
                    case '\\': b.Append('\\'); break;
                    case '/': b.Append('/'); break;
                    case 'b': b.Append('\b'); break;
                    case 'f': b.Append('\f'); break;
                    case 'n': b.Append('\n'); break;
                    case 'r': b.Append('\r'); break;
                    case 't': b.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "short \\u escape");
                        b.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw Error(s, i, "unknown escape");
                }
            }
            throw Error(s, i, "unterminated string");
        }

        static object Num(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw Error(s, i, "unexpected character");
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        // ---- writing ----------------------------------------------------------------

        /// <summary>Writes a value readably: one key per line, arrays of strings one per line.</summary>
        public static string Write(object value)
        {
            var b = new StringBuilder();
            Write(b, value, 0);
            b.Append('\n');
            return b.ToString();
        }

        static void Indent(StringBuilder b, int n) => b.Append(' ', n * 2);

        static void Write(StringBuilder b, object v, int depth)
        {
            switch (v)
            {
                case null: b.Append("null"); return;
                case string s: Quote(b, s); return;
                case bool t: b.Append(t ? "true" : "false"); return;
                case double d: b.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case int n: b.Append(n.ToString(CultureInfo.InvariantCulture)); return;
                case JsonObject o:
                    if (o.Count == 0) { b.Append("{}"); return; }
                    b.Append("{\n");
                    for (int k = 0; k < o.Count; k++)
                    {
                        Indent(b, depth + 1);
                        Quote(b, o.Keys[k]);
                        b.Append(": ");
                        Write(b, o[o.Keys[k]], depth + 1);
                        if (k < o.Count - 1) b.Append(',');
                        b.Append('\n');
                    }
                    Indent(b, depth);
                    b.Append('}');
                    return;
                case List<object> a:
                    if (a.Count == 0) { b.Append("[]"); return; }
                    b.Append("[\n");
                    for (int k = 0; k < a.Count; k++)
                    {
                        Indent(b, depth + 1);
                        Write(b, a[k], depth + 1);
                        if (k < a.Count - 1) b.Append(',');
                        b.Append('\n');
                    }
                    Indent(b, depth);
                    b.Append(']');
                    return;
                default: Quote(b, v.ToString()); return;
            }
        }

        static void Quote(StringBuilder b, string s)
        {
            b.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4"));
                        else b.Append(c);
                        break;
                }
            }
            b.Append('"');
        }
    }
}
