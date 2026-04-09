# Plugin: Shipping.UPS

## Bounded Context
Shipping rate computation — UPS carrier rates and tracking.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.UPS/` — includes SOAP web references (2787 LOC Reference.cs)

## Key Entities
- Implements `IShippingRateComputationMethod`

## External Dependencies
- UPS API (SOAP → migrate to REST)

## Migration Notes
- **Decision**: Rewrite using UPS REST API
- Legacy uses SOAP web references → use UPS REST API with `HttpClient`

## Acceptance Criteria
- [ ] Fetches live shipping rates from UPS REST API
- [ ] Supports shipment tracking
- [ ] Configuration allows API credentials, service types, and packaging
