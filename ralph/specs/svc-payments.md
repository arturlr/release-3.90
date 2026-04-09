# Payment Services

## Bounded Context
Payment processing coordination — payment method loading, transaction processing (authorize, capture, refund, void), and recurring payment management.

## Legacy Source
- `src/Libraries/Nop.Services/Payments/` — all files
- Key interfaces: `IPaymentService`, `IPaymentMethod`
- Domain: `Nop.Core.Domain.Payments`

## Key Entities
- ProcessPaymentRequest, ProcessPaymentResult
- CapturePaymentRequest, CapturePaymentResult
- RefundPaymentRequest, RefundPaymentResult
- VoidPaymentRequest, VoidPaymentResult
- CancelRecurringPaymentRequest, CancelRecurringPaymentResult
- PaymentStatus enum (Pending, Authorized, Paid, PartiallyRefunded, Refunded, Voided)

## External Dependencies
- Payment gateway SDKs (via plugins, not direct)

## Migration Notes
- **Decision**: Rewrite
- `IPaymentService` is the coordinator; actual processing delegated to `IPaymentMethod` plugin implementations
- Payment method filtering: active + country restriction + store limitation + customer role
- Transaction types: Authorize-only vs Authorize-and-Capture (configured per method)
- Refund: full, partial, offline variants
- Additional handling fee per payment method

## Acceptance Criteria
- [ ] `LoadActivePaymentMethods` filters by active status, country, store, and customer role
- [ ] `ProcessPayment` delegates to the correct `IPaymentMethod` plugin and returns proper status
- [ ] Capture, Refund (full + partial), and Void operations execute correctly through the payment method
- [ ] Recurring payment processing creates new orders on each cycle
