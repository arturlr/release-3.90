# Tax Services

## Bounded Context
Tax calculation — tax rate determination, tax display rules, tax provider coordination.

## Legacy Source
- `src/Libraries/Nop.Services/Tax/` — all files
- Key interfaces: `ITaxService`, `ITaxProvider`, `ITaxCategoryService`
- Domain: `Nop.Core.Domain.Tax`

## Key Entities
- TaxCategory
- CalculateTaxRequest, CalculateTaxResult
- TaxDisplayType enum (IncludingTax, ExcludingTax)

## External Dependencies
- Tax rate providers (via plugins)
- EU VIES VAT validation — SOAP web reference `EuropaCheckVatService` in `src/Libraries/Nop.Services/Web References/EuropaCheckVatService/` called by `TaxService.DoVatCheck()` → replace with HTTP client calling VIES REST/SOAP endpoint
- `IGeoLookupService` injected for IP-based tax jurisdiction lookup

## Migration Notes
- **Decision**: Rewrite
- Tax basis: shipping address, billing address, or default store address (configurable)
- Tax classification: products assigned to tax categories; rates per category/country/state/zip
- Tax exemptions: customer-level, role-level, product-level
- Both inclusive and exclusive amounts stored throughout the system
- `ITaxProvider` is plugin interface; `FixedOrByCountryStateZip` is the built-in provider

## Acceptance Criteria
- [ ] Tax rates calculated correctly based on configured basis (shipping/billing/default address)
- [ ] Tax exemptions honored for exempt customers, roles, and products
- [ ] Both tax-inclusive and tax-exclusive prices computed and stored
- [ ] `ITaxProvider` plugin interface allows custom tax calculation providers
- [ ] EU VAT number validation via VIES service returns correct status (valid/invalid/service-down)
