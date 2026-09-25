using System;

namespace fefek5.Currency.Runtime
{
    /// <summary>
    /// Extends a single Currency without touching the package: multipliers, stats, achievements...
    /// Added to Currency.Modules in the inspector. An exception thrown here is logged and skipped,
    /// so one broken module never blocks earning or spending.
    /// </summary>
    [Serializable]
    public abstract class CurrencyModule
    {
        /// <summary>Called once, when the currency loads its balance.</summary>
        public virtual void Initialize(Currency currency) { }

        /// <summary>Called when the currency drops its runtime state (entering Play Mode again).</summary>
        public virtual void Dispose() { }

        /// <summary>Changes the amount before it is added (multipliers, bonuses). Modules run in list order.</summary>
        public virtual long ModifyEarn(Currency currency, long amount, CurrencySource source) => amount;

        /// <summary>Amount actually added, after modifiers and the MaxAmount clamp.</summary>
        public virtual void OnEarn(Currency currency, long amount, CurrencySource source) { }

        public virtual void OnSpend(Currency currency, long amount, SpendReason reason) { }

        /// <summary>New balance after Earn, TrySpend or Set. Not called for SetWithoutNotify.</summary>
        public virtual void OnChanged(Currency currency, long value) { }
    }
}
