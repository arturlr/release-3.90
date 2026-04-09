# Discount Services

## Bounded Context
Discount and promotion management — discount types, coupon codes, discount requirements, usage tracking.

## Legacy Source
- `src/Libraries/Nop.Services/Discounts/` — all files
- Key interfaces: `IDiscountService`, `IDiscountRequirementRule`
- Cache event consumer: `DiscountEventConsumer` (161 LOC) — invalidates discount caches on entity changes to Discount, DiscountRequirement, Category, Manufacturer, Setting
- Domain: `Nop.Core.Domain.Discounts`

## Key Entities
- Discount (6 types: order total, order subtotal, product, category, manufacturer, shipping)
- DiscountRequirement (extensible via plugins, hierarchical with AND/OR logic)
- DiscountUsageHistory

## External Dependencies
- Discount requirement rule plugins

## Migration Notes
- **Decision**: Rewrite
- Discount types: assigned to products, categories, manufacturers, shipping, order total, order subtotal
- Validation: active flag, date range, usage limits (total + per-customer), coupon code
- Requirements: extensible via `IDiscountRequirementRule` plugins with hierarchical AND/OR conditions
- Cumulative discounts: `IsCumulative` flag controls stacking
- Maximum discount amount cap for percentage discounts

## Acceptance Criteria
- [ ] All 6 discount types apply correctly to their respective targets
- [ ] Coupon code validation is case-insensitive and checks all discount conditions
- [ ] Usage limits (total and per-customer) are enforced
- [ ] Hierarchical discount requirements with AND/OR logic evaluate correctly
