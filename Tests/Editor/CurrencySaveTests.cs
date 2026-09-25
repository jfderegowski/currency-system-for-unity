using fefek5.SaveDataVariable.Runtime;
using NUnit.Framework;

namespace fefek5.Currency.Tests
{
    using Runtime;

    public class CurrencySaveTests
    {
        private TestCurrencies _currencies;

        [SetUp]
        public void SetUp() => _currencies = new TestCurrencies();

        [TearDown]
        public void TearDown() => _currencies.Dispose();

        [Test]
        public void SavedValueIsWrittenToFile()
        {
            const int value = 1_500_000_000;
            var currency = _currencies.Create();

            currency.Set(value);
            currency.Save();

            var saved = new SaveData();
            saved.Load(currency.SavePath);

            Assert.AreEqual(value, saved.GetKey(currency.SaveKey, 0));
        }

        [Test]
        public void MissingKeyGivesStartingAmount()
        {
            var currency = _currencies.Create(startingAmount: 250);

            Assert.AreEqual(250, currency.Value);
        }

        [Test]
        public void ValueIsReadFromFile()
        {
            var currency = _currencies.Create(startingAmount: 1);

            new SaveData().SetKey(new SaveKey(TestCurrencies.Key), 123).Save(currency.SavePath);

            Assert.AreEqual(123, currency.Value);
        }

        [Test]
        public void IsDirtyUntilSaved()
        {
            var currency = _currencies.Create();

            Assert.IsFalse(currency.IsDirty);

            currency.Set(10);
            Assert.IsTrue(currency.IsDirty);

            currency.Save();
            Assert.IsFalse(currency.IsDirty);
        }

        [Test]
        public void SaveAllWritesEveryDirtyCurrency()
        {
            var coins = _currencies.Create();
            var gems = _currencies.Create();

            coins.Set(1);
            gems.Set(2);
            Wallet.SaveAll();

            Assert.IsFalse(coins.IsDirty);
            Assert.IsFalse(gems.IsDirty);
        }
    }
}
