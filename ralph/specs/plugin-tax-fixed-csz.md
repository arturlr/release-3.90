# Plugin: Tax.FixedOrByCountryStateZip

## Bounded Context
Tax provider — fixed tax rate or country/state/zip-based tax calculation.

## Legacy Source
- `src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/`

## Key Entities
- Implements `ITaxProvider`
- TaxRate record (tax category, country, state, zip, percentage)

## External Dependencies
- None (local calculation)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Has own DB table for tax rate configuration

## Acceptance Criteria
- [ ] Fixed mode returns configured rate per tax category
- [ ] Country/state/zip mode looks up rate from configuration table
- [ ] Admin configuration UI for managing tax rate records
