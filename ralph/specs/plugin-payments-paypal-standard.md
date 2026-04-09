# Plugin: Payments.PayPalStandard

## Bounded Context
Payment method — PayPal Standard (redirect to PayPal for payment).

## Legacy Source
- `src/Plugins/Nop.Plugin.Payments.PayPalStandard/`

## Key Entities
- Implements `IPaymentMethod`

## External Dependencies
- PayPal Standard (HTTP POST redirect, PDT/IPN)

## Migration Notes
- **Decision**: Rewrite using PayPal Checkout SDK
- Legacy uses PayPal Standard with PDT handler → use PayPal Checkout v2

## Acceptance Criteria
- [ ] Redirects customer to PayPal for payment
- [ ] Handles payment confirmation callback (IPN/webhook)
- [ ] Configuration allows sandbox/production mode and business email
