using System;
using System.Collections.Generic;
using System.Threading;
using fefek5.SaveDataVariable.Runtime;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>Every currency together: global events, prices in several currencies and saving.</summary>
    public static class Wallet
    {
        /// <summary>Currencies listed in the CurrencyDatabase.</summary>
        public static IReadOnlyList<Currency> All => CurrencyDatabase.Instance
            ? CurrencyDatabase.Instance.Currencies
            : Array.Empty<Currency>();

        /// <summary>Raised after Currency.OnEarn of any currency.</summary>
        public static event Action<Currency, int, CurrencySource> OnAnyEarn;

        /// <summary>Raised after Currency.OnSpend of any currency.</summary>
        public static event Action<Currency, int, SpendReason> OnAnySpend;

        // Every currency that loaded its balance, listed in the database or not, and their save files.
        // Files are kept apart so a currency that got unloaded still has its changes saved.
        private static readonly HashSet<Currency> Loaded = new();
        private static readonly HashSet<string> SavePaths = new();

        #region Price

        public static bool CanAfford(Price price) => TryGetTotals(price, out _);

        /// <summary>Takes the whole price or nothing.</summary>
        public static bool TrySpend(Price price, SpendReason reason = null)
        {
            if (!TryGetTotals(price, out var totals)) return false;

            foreach (var (currency, amount) in totals)
                currency.TrySpend(amount, reason);

            return true;
        }

        // Sums the price per currency, so the same currency listed twice is checked against its total
        private static bool TryGetTotals(Price price, out Dictionary<Currency, int> totals)
        {
            totals = new Dictionary<Currency, int>();

            if (price == null) return false;

            foreach (var (currency, amount) in price.Amounts)
            {
                if (amount <= 0) continue;
                if (!currency) return false;

                totals.TryGetValue(currency, out var total);
                totals[currency] = total > int.MaxValue - amount ? int.MaxValue : total + amount;
            }

            foreach (var (currency, total) in totals)
                if (!currency.CanAfford(total))
                    return false;

            return true;
        }

        #endregion

        #region Save

        /// <summary>Writes every unsaved balance to disk, one write per save file.</summary>
        public static void SaveAll()
        {
            foreach (var path in SavePaths)
                if (SaveVar.IsSaveDataDirty(path))
                    SaveVar.PushSaveData(path);
        }

        public static async Awaitable SaveAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var path in new List<string>(SavePaths))
                if (SaveVar.IsSaveDataDirty(path))
                    await SaveVar.PushSaveDataAsync(path, cancellationToken);
        }

        public static bool IsAnyDirty()
        {
            foreach (var path in SavePaths)
                if (SaveVar.IsSaveDataDirty(path))
                    return true;

            return false;
        }

        #endregion

        #region Internal

        internal static void Register(Currency currency)
        {
            Loaded.Add(currency);
            SavePaths.Add(currency.SavePath);
        }

        internal static void RaiseEarn(Currency currency, int amount, CurrencySource source) =>
            OnAnyEarn?.Invoke(currency, amount, source);

        internal static void RaiseSpend(Currency currency, int amount, SpendReason reason) =>
            OnAnySpend?.Invoke(currency, amount, reason);

        // Assets and statics outlive Play Mode in the editor when domain reload is off
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            foreach (var currency in Loaded)
                if (currency)
                    currency.ResetRuntimeState();

            Loaded.Clear();
            SavePaths.Clear();

            OnAnyEarn = null;
            OnAnySpend = null;
        }

        #endregion
    }
}
