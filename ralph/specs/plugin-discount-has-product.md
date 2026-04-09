# Plugin: DiscountRules.HasOneProduct

## Bounded Context
Discount requirement rule — restricts discount to carts containing at least one of specified products.

## Legacy Source
- `src/Plugins/Nop.Plugin.DiscountRules.HasOneProduct/`

## Key Entities
- Implements `IDiscountRequirementRule`

## External Dependencies
- Nop.Core, Nop.Services

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin

## Acceptance Criteria
- [ ] Discount requirement evaluates to true when cart contains at least one configured product
- [ ] Admin configuration UI allows selecting product(s)
- [ ] Plugin installs and uninstalls cleanly
