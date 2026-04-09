# Customer Services

## Bounded Context
Customer management — registration, authentication, customer roles, customer attributes, activity tracking, and guest customer lifecycle.

## Legacy Source
- `src/Libraries/Nop.Services/Customers/` — all files
- Key interfaces: `ICustomerService`, `ICustomerRegistrationService`, `ICustomerAttributeService`, `ICustomerActivityService`
- Domain: `Nop.Core.Domain.Customers`

## Key Entities
- Customer, CustomerRole, CustomerPassword, CustomerAttribute, CustomerAttributeValue
- ExternalAuthenticationRecord, RewardPointsHistory

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Password hashing: legacy supports SHA1/SHA256/MD5 — migrate to bcrypt/Argon2, keep legacy hash verification for migration
- Guest customer pattern: anonymous users get a `Customer` record with guest role — keep this pattern
- `ValidateCustomer` → integrate with ASP.NET Core Identity or keep custom (decision needed)
- Customer role permissions: OR logic across roles
- `DeleteGuestCustomers` cleanup job must be preserved as background task

## Acceptance Criteria
- [ ] Customer registration validates uniqueness of email and username, enforces password policy
- [ ] `ValidateCustomer` authenticates against stored password hash (supporting legacy SHA1/SHA256 for migration)
- [ ] Guest customers are created for anonymous visitors and migrated to registered accounts on registration
- [ ] Customer roles support OR-based permission aggregation
- [ ] Customer activity logging records actions with timestamps and IP addresses
