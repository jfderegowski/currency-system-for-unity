using System;
using System.Globalization;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>
    /// Short form for big numbers: 12 345 → "12.3K", 4 500 000 → "4.5M". Truncates rather than rounds,
    /// so 999 999 shows "999.9K", never "1000.0K" — the player never sees more than they have.
    /// </summary>
    [Serializable]
    public class AbbreviatedFormatter : ICurrencyFormatter
    {
        // int.MaxValue ≈ 2.1B
        private static readonly string[] Suffixes = { "", "K", "M", "B" };

        [SerializeField, Range(0, 3)] private int decimals = 1;

        [SerializeField, Min(0), Tooltip("Smaller values are shown in full")]
        private int abbreviateFrom = 10_000;

        public AbbreviatedFormatter() { }

        public AbbreviatedFormatter(int decimals, int abbreviateFrom)
        {
            this.decimals = decimals;
            this.abbreviateFrom = abbreviateFrom;
        }

        public string Format(int value)
        {
            var magnitude = Math.Abs((decimal)value);

            if (magnitude < abbreviateFrom || magnitude < 1000)
                return value.ToString(CultureInfo.InvariantCulture);

            var tier = 0;
            var divisor = 1m;

            while (tier < Suffixes.Length - 1 && magnitude >= divisor * 1000)
            {
                divisor *= 1000;
                tier++;
            }

            var digits = Math.Clamp(decimals, 0, 3);
            var scale = (decimal)Math.Pow(10, digits);
            var shown = decimal.Truncate(magnitude / divisor * scale) / scale;
            var text = shown.ToString("F" + digits, CultureInfo.InvariantCulture) + Suffixes[tier];

            return value < 0 ? "-" + text : text;
        }
    }
}
