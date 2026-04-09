# Plugin: Payments.PurchaseOrder

## Bounded Context
Payment method — purchase order (offline B2B payment).

## Legacy Source
- `src/Plugins/Nop.Plugin.Payments.PurchaseOrder/`

## Key Entities
- Implements `IPaymentMethod`

## External Dependencies
- None (offline payment)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin

## Acceptance Criteria
- [ ] Collects PO number during checkout
- [ ] Order created with Pending payment status
- [ ] PO number stored and visible in admin order details
