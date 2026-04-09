# Plugin: Shipping.Fedex

## Bounded Context
Shipping rate computation — FedEx carrier rates and tracking.

## Legacy Source
- `src/Plugins/Nop.Plugin.Shipping.Fedex/` — includes SOAP web references (16721 LOC Reference.cs, 4850 LOC TrackService.cs)

## Key Entities
- Implements `IShippingRateComputationMethod`

## External Dependencies
- FedEx Web Services (SOAP → migrate to REST API)

## Migration Notes
- **Decision**: Rewrite using FedEx REST API
- Legacy uses SOAP web references (massive generated code) → use FedEx REST API v1 with `HttpClient`
- Shipment tracking support via `IShipmentTracker`

## Acceptance Criteria
- [ ] Fetches live shipping rates from FedEx REST API
- [ ] Supports shipment tracking via tracking number
- [ ] Configuration allows API credentials, service types, and packaging options
