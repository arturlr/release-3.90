# Affiliate Services

## Bounded Context
Affiliate tracking — affiliate management and order attribution.

## Legacy Source
- `src/Libraries/Nop.Services/Affiliates/` — all files
- Key interfaces: `IAffiliateService`
- Domain: `Nop.Core.Domain.Affiliates`

## Key Entities
- Affiliate (Address, Active, Deleted)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Simple CRUD with affiliate-to-customer and affiliate-to-order tracking
- Affiliates have an address and active/deleted status
- Orders and customers can be attributed to affiliates

## Acceptance Criteria
- [ ] Affiliate CRUD with address association
- [ ] Orders attributed to affiliates are queryable by affiliate ID
- [ ] Affiliate-based customer tracking works correctly
