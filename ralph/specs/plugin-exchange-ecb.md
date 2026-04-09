# Plugin: ExchangeRate.EcbExchange

## Bounded Context
Exchange rate provider — fetches live currency rates from European Central Bank.

## Legacy Source
- `src/Plugins/Nop.Plugin.ExchangeRate.EcbExchange/`

## Key Entities
- Implements `IExchangeRateProvider`

## External Dependencies
- ECB XML feed (HTTP)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Uses `HttpClient` to fetch ECB daily rates XML

## Acceptance Criteria
- [ ] Fetches live exchange rates from ECB XML feed
- [ ] Returns rates for all available currencies
- [ ] Plugin installs and uninstalls cleanly
