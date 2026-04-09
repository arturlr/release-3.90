# Plugin: Payments.CheckMoneyOrder

## Bounded Context
Payment method — check/money order (offline payment).

## Legacy Source
- `src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/`

## Key Entities
- Implements `IPaymentMethod`

## External Dependencies
- None (offline payment)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin

## Acceptance Criteria
- [ ] Payment method appears in checkout when active
- [ ] Order created with Pending payment status
- [ ] Configuration allows setting description text shown to customer
