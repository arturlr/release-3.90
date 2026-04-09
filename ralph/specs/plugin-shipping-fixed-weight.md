# Plugin: Shipping.FixedOrByWeight

## Bounded Context
Shipping rate computation — fixed rate or weight-based shipping calculation.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.FixedOrByWeight/`

## Key Entities
- Implements `IShippingRateComputationMethod`
- ShippingByWeightRecord (country, state, zip, weight range, rate)

## External Dependencies
- None (local calculation)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Has own DB table for weight-based rate configuration

## Acceptance Criteria
- [ ] Fixed rate mode returns configured flat rate per shipping method
- [ ] Weight-based mode calculates rate from weight/country/state/zip lookup table
- [ ] Admin configuration UI for managing rate records
