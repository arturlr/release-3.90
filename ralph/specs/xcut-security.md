# Security

## Bounded Context
Security infrastructure — permission management, access control lists (ACL), and encryption services.

## Legacy Source
- `src/Libraries/Nop.Services/Security/` — all files
- Key interfaces: `IPermissionService`, `IAclService`, `IEncryptionService`
- Domain: `Nop.Core.Domain.Security`

## Key Entities
- PermissionRecord (Name, SystemName, Category)
- AclRecord (EntityId, EntityName, CustomerRoleId)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Permission model: permissions assigned to roles, OR logic across customer's roles
- ACL: entity-level access control via `IAclSupported` marker interface
- Encryption: password hashing (SHA1/SHA256/MD5 → bcrypt/Argon2), text encrypt/decrypt
- Standard permissions installed during setup; plugins can add custom permissions
- In .NET 10: integrate with ASP.NET Core authorization policies

## Acceptance Criteria
- [ ] `IPermissionService.Authorize` checks if any of the customer's roles has the requested permission
- [ ] `IAclService.Authorize` filters entities by customer role when `SubjectToAcl` is true
- [ ] Password hashing uses modern algorithm (bcrypt/Argon2) with legacy hash verification for migration
- [ ] Text encryption/decryption works for sensitive data storage
