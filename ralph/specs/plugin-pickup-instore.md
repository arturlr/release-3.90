# Plugin: Pickup.PickupInStore

## Bounded Context
Shipping/pickup — in-store pickup point provider.

## Legacy Source
- `src/Plugins/Nop.Plugin.Pickup.PickupInStore/`

## Key Entities
- Implements `IPickupPointProvider`
- StorePickupPoint entity (store location, address, fees)

## External Dependencies
- None

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin

## Acceptance Criteria
- [ ] Pickup points configurable per store with address and pickup fee
- [ ] Appears as shipping option during checkout when active
- [ ] Plugin installs and uninstalls cleanly with own DB table
