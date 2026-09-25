using System;
using System.Globalization;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>Full number with grouped thousands: "1 234 567".</summary>
    [Serializable]
    public class PlainFormatter : ICurrencyFormatter
    {
        internal static readonly PlainFormatter Default = new();

        [SerializeField, Tooltip("Put between groups of thousands, empty for none")]
        private string separator = " ";

        public PlainFormatter() { }

        public PlainFormatter(string separator) => this.separator = separator;

        public string Format(long value) => string.IsNullOrEmpty(separator)
            ? value.ToString(CultureInfo.InvariantCulture)
            : value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", separator);
    }
}
