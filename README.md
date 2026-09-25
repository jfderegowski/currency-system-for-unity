# Currency

ScriptableObject-based currencies for Unity: earn, spend, save and extend with modules.
Every currency, earning source and spend reason is an asset, so adding one never means changing code.

## Installation

Add the package from its git URL in the Package Manager, or to `Packages/manifest.json`:

```json
"com.fefek5.currency": "https://github.com/jfderegowski/currency-system-for-unity.git"
```

Unity does not resolve git dependencies of a package, so the project also needs these in its manifest:

```json
"com.fefek5.save-data": "https://github.com/jfderegowski/save-data.git",
"com.fefek5.toys-for-unity": "https://github.com/jfderegowski/toys-for-unity.git",
"com.fefek5.resources-scriptable-object-singleton-system": "https://github.com/jfderegowski/resources-scriptable-object-singleton-system.git",
"com.fefek5.has-value": "https://github.com/jfderegowski/variable-has-value.git",
"com.fefek5.serializable-guid": "https://github.com/jfderegowski/serializable-guid.git"
```

## Setup

1. **Create > Currency > Currency** for every currency (Coins, Gems...).
   Give each one its own save key - the inspector has a button that uses the asset name.
2. **Create > Currency > Currency Source** for every place currency comes from (Passive, Click, Reward...).
   Optionally **Spend Reason** assets for what it is spent on.
3. **Tools > Currency > Rebuild Database** creates `Assets/Resources/CurrencyDatabase.asset`.
   After that, new currencies are added to it on their own. The database also holds the auto save settings.

## Usage

```csharp
[SerializeField] private Currency coins;
[SerializeField] private CurrencySource overtake;
[SerializeField] private Price upgradePrice;

coins.Earn(100, overtake);

if (coins.TrySpend(250))
    Upgrade();

if (upgradePrice.TrySpend())   // several currencies, all or nothing
    Upgrade();

coins.OnChanged += value => Debug.Log(value);
Wallet.OnAnyEarn += (currency, amount, source) => Analytics.Log(currency.name, amount, source.name);
```

| Method | Events |
|---|---|
| `Earn(amount, source)` | `OnEarn(amount, source)`, `Wallet.OnAnyEarn`, `OnChanged(value)` |
| `TrySpend(amount, reason = null)` | `OnSpend(amount, reason)`, `Wallet.OnAnySpend`, `OnChanged(value)` |
| `Set(value)` | `OnChanged(value)` |
| `SetWithoutNotify(value)` | `OnChangedWithoutNotify(value)` |

- The detailed event (`OnEarn`, `OnSpend`) comes before `OnChanged`, and modules hear about a change before
  outside listeners.
- `Earn` gets the amount after modules and the `MaxAmount` clamp, so `OnEarn` reports what was really added.
- `Earn` with an amount of 0 or less does nothing. `TrySpend(0)` returns true without events.
- `SetWithoutNotify` is meant for loading or migrating a save: it reaches the UI without looking like gameplay.

### Saving

Balances are `int` values in a `SaveVar<int>` (`Currencies.json` by default). A change stays in memory until it is
saved, because currencies can change several times per second:

- `CurrencyAutoSaver` saves every `AutoSaveInterval` seconds, when the app is paused or loses focus, and on quit.
  It is created on its own when the database has `AutoSave` on.
- `currency.Save()`, `Wallet.SaveAll()` and their async versions save by hand. `SaveAll` writes each file once.

In the editor, entering Play Mode drops loaded balances, unsaved changes and listeners, so it works the same with
domain reload turned off.

### Formatting

`currency.Format()` uses the currency's formatter: `AbbreviatedFormatter` (`12.3K`, `4.5M`, truncated, never rounded
up) or `PlainFormatter` (`1 234 567`). Implement `ICurrencyFormatter` for your own.
`CurrencyLabel` shows a balance in a `TMP_Text` and keeps it up to date.

### Modules

Derive from `CurrencyModule` and add it to a currency's **Modules** list to react to or change its earnings:

```csharp
[Serializable]
public class DoubleWeekendModule : CurrencyModule
{
    public override int ModifyEarn(Currency currency, int amount, CurrencySource source) =>
        DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? amount * 2 : amount;
}
```

An exception in a module is logged and skipped, so it never blocks earning.

## Debugging

- The currency inspector shows the live balance in Play Mode, with Earn, Spend, Set and Save buttons.
- **Window > Currencies** lists every currency of the database with its balance.

## Samples

- **Stat Tracker** - a module that counts earned and spent currency (in total, per source and per spend reason) into `com.fefek5.stats` `IntStat`s,
  pushed in batches. Needs `com.fefek5.stats`.
- **Basic Shop** - a uGUI button that buys something for a `Price`.
