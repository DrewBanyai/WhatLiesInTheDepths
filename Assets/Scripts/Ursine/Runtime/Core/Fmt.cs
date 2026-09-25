// Ursine — how an incremental game writes a number.
//
// The house rules here are the ones that keep a ticking readout still: no abbreviation, a
// sign and one decimal on every rate including zero, and no decimal on a count. A game
// that wants 1.3k instead of 1,284 should not use these.
using System.Globalization;

namespace Ursine
{
    public static class Fmt
    {
        public static CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        /// <summary>A held amount or a cost. No abbreviation, ever: 1,284, never 1.3k.</summary>
        public static string Count(double v) => ((long)System.Math.Round(v)).ToString("N0", Culture);

        /// <summary>A rate. Always signed, always present, one decimal, suffixed. A rate
        /// below the threshold reads 0.0 rather than going blank, so a row never empties.</summary>
        public static string Rate(double perUnit, string suffix = " /s", double deadZone = 0.05)
        {
            if (perUnit > -deadZone && perUnit < deadZone) return "0.0" + suffix;
            string sign = perUnit > 0 ? "+" : "-";
            return sign + System.Math.Abs(perUnit).ToString("N1", Culture) + suffix;
        }

        /// <summary>An interval: a dive every 6.0s. Period, not frequency — a player can
        /// hold "one every six seconds" in their head and cannot hold "0.167 per second".</summary>
        public static string Period(double seconds) => seconds.ToString("0.0", Culture) + "s";

        public static string Percent(double pct) => ((int)System.Math.Round(pct)).ToString(Culture) + "%";

        /// <summary>Elapsed time in whole seconds however large — "90 seconds", never
        /// "a minute and a half", when the number is being compared against an interval.</summary>
        public static string Seconds(double seconds)
        {
            int s = (int)System.Math.Floor(seconds);
            return s == 1 ? "1 second" : s.ToString("N0", Culture) + " seconds";
        }

        /// <summary>A small count written out, for captions that read as prose: "nine".
        /// Past ninety-nine it falls back to figures.</summary>
        public static string Words(int n)
        {
            string[] ones = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight",
                              "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen",
                              "sixteen", "seventeen", "eighteen", "nineteen" };
            string[] tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy",
                              "eighty", "ninety" };
            if (n < 0 || n > 99) return n.ToString("N0", Culture);
            if (n < 20) return ones[n];
            return n % 10 == 0 ? tens[n / 10] : tens[n / 10] + "-" + ones[n % 10];
        }

        /// <summary>A small ordinal written out: "first", "second", "twenty-third".
        /// Past ninety-ninth it falls back to figures with a suffix: "100th".</summary>
        public static string OrdinalWords(int n)
        {
            string[] ones = { "zeroth", "first", "second", "third", "fourth", "fifth", "sixth", "seventh",
                              "eighth", "ninth", "tenth", "eleventh", "twelfth", "thirteenth", "fourteenth",
                              "fifteenth", "sixteenth", "seventeenth", "eighteenth", "nineteenth" };
            string[] tens = { "", "", "twentieth", "thirtieth", "fortieth", "fiftieth", "sixtieth",
                              "seventieth", "eightieth", "ninetieth" };
            if (n < 0 || n > 99)
            {
                int t = System.Math.Abs(n) % 100, u = System.Math.Abs(n) % 10;
                string suffix = t >= 11 && t <= 13 ? "th" : u == 1 ? "st" : u == 2 ? "nd" : u == 3 ? "rd" : "th";
                return n.ToString("N0", Culture) + suffix;
            }
            if (n < 20) return ones[n];
            return n % 10 == 0 ? tens[n / 10] : Words(n - n % 10) + "-" + ones[n % 10];
        }

        public static string Roman(int n)
        {
            if (n <= 0) return string.Empty;
            int[] v = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
            string[] s = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < v.Length; i++)
                while (n >= v[i]) { sb.Append(s[i]); n -= v[i]; }
            return sb.ToString();
        }
    }
}
