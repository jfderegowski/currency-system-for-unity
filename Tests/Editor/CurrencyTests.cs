using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace fefek5.Currency.Tests
{
    using Runtime;

    public class CurrencyTests
    {
        private TestCurrencies _currencies;
        private CurrencySource _source;

        [SetUp]
        public void SetUp()
        {
            _currencies = new TestCurrencies();
            _source = _currencies.CreateSource();
        }

        [TearDown]
        public void TearDown() => _currencies.Dispose();

        [Test]
        public void EarnAddsAndRaisesOnEarnBeforeOnChanged()
        {
            var currency = _currencies.Create(startingAmount: 10);
            var calls = new List<string>();

            currency.OnEarn += (amount, source) => calls.Add($"earn {amount} {(source == _source)}");
            currency.OnChanged += value => calls.Add($"changed {value}");

            currency.Earn(5, _source);

            Assert.AreEqual(15, currency.Value);
            CollectionAssert.AreEqual(new[] { "earn 5 True", "changed 15" }, calls);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void EarnNotPositiveDoesNothing(int amount)
        {
            var currency = _currencies.Create(startingAmount: 10);
            var raised = false;

            currency.OnEarn += (_, _) => raised = true;
            currency.OnChanged += _ => raised = true;

            currency.Earn(amount, _source);

            Assert.AreEqual(10, currency.Value);
            Assert.IsFalse(raised);
        }

        [Test]
        public void TrySpendWithoutFundsFails()
        {
            var currency = _currencies.Create(startingAmount: 10);
            var raised = false;

            currency.OnSpend += (_, _) => raised = true;
            currency.OnChanged += _ => raised = true;

            Assert.IsFalse(currency.TrySpend(11));
            Assert.AreEqual(10, currency.Value);
            Assert.IsFalse(raised);
        }

        [Test]
        public void TrySpendWholeBalanceLeavesZero()
        {
            var currency = _currencies.Create(startingAmount: 10);
            var calls = new List<string>();

            currency.OnSpend += (amount, _) => calls.Add($"spend {amount}");
            currency.OnChanged += value => calls.Add($"changed {value}");

            Assert.IsTrue(currency.TrySpend(10));
            Assert.AreEqual(0, currency.Value);
            CollectionAssert.AreEqual(new[] { "spend 10", "changed 0" }, calls);
        }

        [Test]
        public void TrySpendZeroSucceedsWithoutEvents()
        {
            var currency = _currencies.Create(startingAmount: 10);
            var raised = false;

            currency.OnSpend += (_, _) => raised = true;
            currency.OnChanged += _ => raised = true;

            Assert.IsTrue(currency.TrySpend(0));
            Assert.IsFalse(raised);
        }

        [Test]
        public void SetRaisesOnlyOnChanged()
        {
            var currency = _currencies.Create();
            var calls = new List<string>();
            SubscribeAll(currency, calls);

            currency.Set(42);

            Assert.AreEqual(42, currency.Value);
            CollectionAssert.AreEqual(new[] { "changed 42" }, calls);
        }

        [Test]
        public void SetWithoutNotifyRaisesOnlyOnChangedWithoutNotify()
        {
            var currency = _currencies.Create();
            var calls = new List<string>();
            SubscribeAll(currency, calls);

            currency.SetWithoutNotify(42);

            Assert.AreEqual(42, currency.Value);
            CollectionAssert.AreEqual(new[] { "silent 42" }, calls);
        }

        [Test]
        public void MaxAmountClampsAndOnEarnGetsTheAddedAmount()
        {
            var currency = _currencies.Create(startingAmount: 90, maxAmount: 100);
            int earned = 0;

            currency.OnEarn += (amount, _) => earned = amount;

            currency.Earn(50, _source);

            Assert.AreEqual(100, currency.Value);
            Assert.AreEqual(10, earned);
        }

        [Test]
        public void EarnAtIntMaxValueDoesNotOverflow()
        {
            var currency = _currencies.Create();
            int earned = 0;

            currency.OnEarn += (amount, _) => earned = amount;

            currency.Set(int.MaxValue - 5);
            currency.Earn(100, _source);

            Assert.AreEqual(int.MaxValue, currency.Value);
            Assert.AreEqual(5, earned);
        }

        [Test]
        public void ModuleModifyEarnDoublesTheAmount()
        {
            var currency = _currencies.Create();
            currency.Modules.Add(new DoubleModule());

            currency.Earn(21, _source);

            Assert.AreEqual(42, currency.Value);
        }

        [Test]
        public void ThrowingModuleDoesNotBlockEarning()
        {
            var currency = _currencies.Create();
            currency.Modules.Add(new ThrowingModule());
            currency.Modules.Add(new DoubleModule());

            LogAssert.Expect(LogType.Exception, new Regex(nameof(ThrowingModule)));
            LogAssert.Expect(LogType.Exception, new Regex(nameof(ThrowingModule)));

            currency.Earn(5, _source);

            Assert.AreEqual(10, currency.Value);
        }

        [Test]
        public void ModulesHearBeforeListeners()
        {
            var currency = _currencies.Create();
            var calls = new List<string>();
            currency.Modules.Add(new RecordingModule(calls));
            SubscribeAll(currency, calls);

            currency.Earn(5, _source);

            CollectionAssert.AreEqual(
                new[] { "module earn 5", "earn 5", "module changed 5", "changed 5" }, calls);
        }

        [Test]
        public void ResetRuntimeStateClearsListeners()
        {
            var currency = _currencies.Create();
            var calls = new List<string>();
            SubscribeAll(currency, calls);

            currency.ResetRuntimeState();
            currency.Earn(5, _source);
            currency.SetWithoutNotify(1);

            CollectionAssert.IsEmpty(calls);
        }

        [Test]
        public void ResetRuntimeStateDropsUnsavedBalance()
        {
            var currency = _currencies.Create(startingAmount: 10);

            currency.Earn(5, _source);
            currency.ResetRuntimeState();

            Assert.AreEqual(10, currency.Value);
        }

        [Test]
        public void WalletRaisesGlobalEvents()
        {
            var currency = _currencies.Create();
            Currency earnedCurrency = null;
            int spent = 0;

            void OnAnyEarn(Currency c, int amount, CurrencySource source) => earnedCurrency = c;
            void OnAnySpend(Currency c, int amount, SpendReason reason) => spent = amount;

            Wallet.OnAnyEarn += OnAnyEarn;
            Wallet.OnAnySpend += OnAnySpend;

            try
            {
                currency.Earn(10, _source);
                currency.TrySpend(3);
            }
            finally
            {
                Wallet.OnAnyEarn -= OnAnyEarn;
                Wallet.OnAnySpend -= OnAnySpend;
            }

            Assert.AreEqual(currency, earnedCurrency);
            Assert.AreEqual(3, spent);
        }

        private static void SubscribeAll(Currency currency, List<string> calls)
        {
            currency.OnEarn += (amount, _) => calls.Add($"earn {amount}");
            currency.OnSpend += (amount, _) => calls.Add($"spend {amount}");
            currency.OnChanged += value => calls.Add($"changed {value}");
            currency.OnChangedWithoutNotify += value => calls.Add($"silent {value}");
        }

        [Serializable]
        private class DoubleModule : CurrencyModule
        {
            public override int ModifyEarn(Currency currency, int amount, CurrencySource source) => amount * 2;
        }

        [Serializable]
        private class ThrowingModule : CurrencyModule
        {
            public override int ModifyEarn(Currency currency, int amount, CurrencySource source) =>
                throw new InvalidOperationException(nameof(ThrowingModule));

            public override void OnEarn(Currency currency, int amount, CurrencySource source) =>
                throw new InvalidOperationException(nameof(ThrowingModule));
        }

        [Serializable]
        private class RecordingModule : CurrencyModule
        {
            private readonly List<string> _calls;

            public RecordingModule(List<string> calls) => _calls = calls;

            public override void OnEarn(Currency currency, int amount, CurrencySource source) =>
                _calls.Add($"module earn {amount}");

            public override void OnChanged(Currency currency, int value) => _calls.Add($"module changed {value}");
        }
    }
}
