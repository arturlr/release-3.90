# Shipping Services

## Bounded Context
Shipping coordination — shipping rate computation, shipment tracking, shipping method management, warehouse support, and delivery date ranges.

## Legacy Source
- `src/Libraries/Nop.Services/Shipping/` — all files including `Pickup/` subdirectory
- Key interfaces: `IShippingService`, `IShipmentService`, `IShippingRateComputationMethod`, `IPickupPointProvider`, `IDateRangeService`, `IShipmentTracker`
- `src/Libraries/Nop.Services/Shipping/Tracking/` — `IShipmentTracker`, `GeneralShipmentTracker`, `ShipmentStatusEvent`
- Domain: `Nop.Core.Domain.Shipping`

## Key Entities
- ShippingMethod, Shipment, ShipmentItem
- Warehouse, DeliveryDate, ProductAvailabilityRange
- GetShippingOptionRequest, GetShippingOptionResponse, ShippingOption

## External Dependencies
- Carrier APIs (via plugins: UPS, USPS, FedEx, CanadaPost, AustraliaPost)

## Migration Notes
- **Decision**: Rewrite
- `IShippingService` coordinates; actual rate computation delegated to `IShippingRateComputationMethod` plugins
- Shipping method filtering: country, store, customer role, weight/dimensions
- Free shipping rules: order total threshold, product flag, discount, customer role
- Multi-warehouse: stock tracked per warehouse, fulfillment from specified warehouse
- Partial shipments: orders can have multiple shipments

## Acceptance Criteria
- [ ] `GetShippingOptions` returns rates from all active shipping rate computation plugins filtered by address/weight/store
- [ ] Shipment creation tracks items, quantities, tracking numbers, and warehouse source
- [ ] Free shipping logic correctly evaluates all conditions (threshold, product flag, discount, role)
- [ ] Multi-warehouse inventory is correctly tracked and reduced on shipment
