# Plugin: Shipping.AustraliaPost

## Bounded Context
Shipping rate computation — Australia Post carrier rates.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.AustraliaPost/`

## Key Entities
- Implements `IShippingRateComputationMethod`

## External Dependencies
- Australia Post API

## Migration Notes
- **Decision**: Rewrite using modern Australia Post API with `HttpClient`

## Acceptance Criteria
- [ ] Fetches live shipping rates from Australia Post API
- [ ] Returns rates for configured service types
- [ ] Configuration allows API key and service selection
