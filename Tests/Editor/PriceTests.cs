using NUnit.Framework;

namespace fefek5.Currency.Tests
{
    using Runtime;

    public class PriceTests
    {
        private TestCurrencies _currencies;

        [SetUp]
        public void SetUp() => _currencies = new TestCurrencies();

        [TearDown]
        public void TearDown() => _currencies.Dispose();

        [Test]
        public void TrySpendTakesNothingWhenOneCurrencyIsShort()
        {
            var coins = _currencies.Create(startingAmount: 100);
            var gems = _currencies.Create(startingAmount: 10);
            var price = new Price(new CurrencyAmount(coins, 50), new CurrencyAmount(gems, 20));

            Assert.IsFalse(price.CanAfford());
            Assert.IsFalse(price.TrySpend());
            Assert.AreEqual(100, coins.Value);
            Assert.AreEqual(10, gems.Value);
        }

        [Test]
        public void TrySpendTakesEveryCurrency()
        {
            var coins = _currencies.Create(startingAmount: 100);
            var gems = _currencies.Create(startingAmount: 10);
            var price = new Price(new CurrencyAmount(coins, 50), new CurrencyAmount(gems, 10));

            Assert.IsTrue(price.TrySpend());
            Assert.AreEqual(50, coins.Value);
            Assert.AreEqual(0, gems.Value);
        }

        [Test]
        public void SameCurrencyTwiceIsSummed()
        {
            var coins = _currencies.Create(startingAmount: 100);

            Assert.IsFalse(new Price(new CurrencyAmount(coins, 60), new CurrencyAmount(coins, 60)).CanAfford());

            var price = new Price(new CurrencyAmount(coins, 60), new CurrencyAmount(coins, 40));

            Assert.IsTrue(price.TrySpend());
            Assert.AreEqual(0, coins.Value);
        }

        [Test]
        public void MissingCurrencyCannotBeAfforded()
        {
            Assert.IsFalse(new Price(null, 10).CanAfford());
        }
    }
}
