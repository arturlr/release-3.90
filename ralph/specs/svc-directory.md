# Directory Services

## Bounded Context
Directory data — countries, states/provinces, currencies, exchange rates, measure weights, and measure dimensions.

## Legacy Source
- `src/Libraries/Nop.Services/Directory/` — all files
- Key interfaces: `ICountryService`, `IStateProvinceService`, `ICurrencyService`, `IExchangeRateProvider`, `IMeasureService`, `IGeoLookupService`
- Domain: `Nop.Core.Domain.Directory`

## Key Entities
- Country, StateProvince
- Currency, ExchangeRate
- MeasureWeight, MeasureDimension

## External Dependencies
- Exchange rate providers (via plugins, e.g., ECB)

## Migration Notes
- **Decision**: Rewrite
- Currency conversion: products priced in primary currency, converted to working currency
- Exchange rates: manual or via `IExchangeRateProvider` plugin (scheduled task)
- Measure conversions: ratio-based conversion to base unit

## Acceptance Criteria
- [ ] Country/state CRUD with store mapping and published filtering
- [ ] Currency conversion produces correct amounts using stored exchange rates
- [ ] `IExchangeRateProvider` plugin interface supports live rate fetching
- [ ] Measure weight/dimension conversions use correct ratios
