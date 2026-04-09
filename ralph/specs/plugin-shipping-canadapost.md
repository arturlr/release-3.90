# Plugin: Shipping.CanadaPost

## Bounded Context
Shipping rate computation — Canada Post carrier rates.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.CanadaPost/`

## Key Entities
- Implements `IShippingRateComputationMethod`

## External Dependencies
- Canada Post API

## Migration Notes
- **Decision**: Rewrite using modern Canada Post REST API with `HttpClient`

## Acceptance Criteria
- [ ] Fetches live shipping rates from Canada Post API
- [ ] Returns rates for configured service types
- [ ] Configuration allows API credentials and service selection
