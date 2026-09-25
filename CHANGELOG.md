# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-09-25

### Added

- `Currency` ScriptableObject: `long` balance kept in a `SaveVar<long>`, `Earn`, `TrySpend`, `Set`,
  `SetWithoutNotify`, events, `MaxAmount`, saving on demand.
- `CurrencySource` and `SpendReason` assets, passed with `Earn` and `TrySpend`.
- `CurrencyModule` for extending a currency from the inspector.
- `Wallet`: global earn/spend events, `Price` checks and `SaveAll`.
- `Price` / `CurrencyAmount` for costs in several currencies.
- `CurrencyDatabase` in Resources and `CurrencyAutoSaver`.
- `AbbreviatedFormatter`, `PlainFormatter` and `CurrencyLabel`.
- `ProtectedLong`, moved from drag-race-screen.
- Currency inspector with Play Mode controls, database postprocessor and the Currencies debug window.
- Stat Tracker and Basic Shop samples.
