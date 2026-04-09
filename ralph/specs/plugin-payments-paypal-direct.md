# Plugin: Payments.PayPalDirect

## Bounded Context
Payment method — PayPal Direct (credit card via PayPal API).

## Legacy Source
- `src/Plugins/Nop.Plugin.Payments.PayPalDirect/`

## Key Entities
- Implements `IPaymentMethod`

## External Dependencies
- PayPal REST API SDK

## Migration Notes
- **Decision**: Rewrite using modern PayPal SDK
- Legacy uses older PayPal SDK → use PayPal REST API v2

## Acceptance Criteria
- [ ] Processes credit card payments via PayPal Direct API
- [ ] Supports authorize, capture, refund, and void
- [ ] Configuration allows sandbox/production mode toggle
