using System;
using System.Collections.Generic;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    [Serializable]
    public struct CurrencyAmount
    {
        public Currency Currency;
        public long Amount;

        public CurrencyAmount(Currency currency, long amount)
        {
            Currency = currency;
            Amount = amount;
        }

        public void Deconstruct(out Currency currency, out long amount)
        {
            currency = Currency;
            amount = Amount;
        }
    }

    /// <summary>A cost in one or more currencies, paid all at once or not at all.</summary>
    [Serializable]
    public class Price
    {
        [field: SerializeField] public List<CurrencyAmount> Amounts { get; private set; } = new();

        public Price() { }

        public Price(Currency currency, long amount) => Amounts.Add(new CurrencyAmount(currency, amount));

        public Price(params CurrencyAmount[] amounts) => Amounts.AddRange(amounts);

        public bool CanAfford() => Wallet.CanAfford(this);

        public bool TrySpend(SpendReason reason = null) => Wallet.TrySpend(this, reason);
    }
}
