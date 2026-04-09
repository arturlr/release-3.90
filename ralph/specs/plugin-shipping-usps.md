# Plugin: Shipping.USPS

## Bounded Context
Shipping rate computation — USPS carrier rates.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.USPS/`

## Key Entities
- Implements `IShippingRateComputationMethod`

## External Dependencies
- USPS Web Tools API

## Migration Notes
- **Decision**: Rewrite using USPS Web Tools API with `HttpClient`

## Acceptance Criteria
- [ ] Fetches live shipping rates from USPS API
- [ ] Returns rates for configured service types (Priority, Express, etc.)
- [ ] Configuration allows API credentials and service selection
