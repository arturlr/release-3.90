# Plugin: DiscountRules.CustomerRoles

## Bounded Context
Discount requirement rule — restricts discount to customers with specific roles.

## Legacy Source
- `src/Plugins/Nop.Plugin.DiscountRules.CustomerRoles/`

## Key Entities
- Implements `IDiscountRequirementRule`
- Configuration: select customer role(s) required for discount

## External Dependencies
- Nop.Core, Nop.Services

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Simple rule: check if customer has required role

## Acceptance Criteria
- [ ] Discount requirement evaluates to true only when customer has the configured role
- [ ] Admin configuration UI allows selecting customer role
- [ ] Plugin installs and uninstalls cleanly
