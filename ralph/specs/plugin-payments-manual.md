# Plugin: Payments.Manual

## Bounded Context
Payment method — manual credit card entry (for testing/development).

## Legacy Source
- `src/Plugins/Nop.Plugin.Payments.Manual/`

## Key Entities
- Implements `IPaymentMethod`

## External Dependencies
- None (processes locally)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Collects card details and stores transaction ID locally
- NOT PCI compliant for production — development/testing only

## Acceptance Criteria
- [ ] Collects credit card details during checkout
- [ ] Supports Authorize and Authorize+Capture transaction modes
- [ ] Supports refund and void operations
