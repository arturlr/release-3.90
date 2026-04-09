# GDPR Services

## Bounded Context
GDPR compliance — customer data export, right to deletion, consent tracking, and data anonymization.

## Legacy Source
- `src/Libraries/Nop.Services/Gdpr/` (if exists) or spread across CustomerService
- Customer data export, anonymization, and consent features

## Key Entities
- GdprLog (consent records)
- Customer data export format
- Anonymization rules

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Right to access: export all customer data (orders, addresses, reviews, etc.)
- Right to deletion: anonymize customer data while preserving order history
- Consent tracking: log customer consent for data processing
- Anonymization: replace PII with generic values, change email to deleted+GUID format

## Acceptance Criteria
- [ ] Customer data export produces complete data package (orders, addresses, reviews, activity)
- [ ] Customer deletion anonymizes PII while preserving order history integrity
- [ ] Consent tracking logs customer consent/withdrawal with timestamps
