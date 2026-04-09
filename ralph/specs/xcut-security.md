# Security

## Bounded Context
Security infrastructure — permission management, access control lists (ACL), encryption services, CAPTCHA/anti-spam, HTTPS enforcement, and anti-forgery protection.

## Legacy Source
- `src/Libraries/Nop.Services/Security/` — all files
- `src/Presentation/Nop.Web.Framework/Security/` — anti-forgery, HTTPS, IP validation
- `src/Presentation/Nop.Web.Framework/Security/Captcha/` — Google reCAPTCHA integration (10 files)
- `src/Presentation/Nop.Web.Framework/Security/Honeypot/` — honeypot anti-spam (2 files)
- Key interfaces: `IPermissionService`, `IAclService`, `IEncryptionService`
- Domain: `Nop.Core.Domain.Security` (includes `SecuritySettings` with honeypot and captcha config)

## Key Entities
- PermissionRecord (Name, SystemName, Category)
- AclRecord (EntityId, EntityName, CustomerRoleId)
- CaptchaSettings (Enabled, ReCaptchaPublicKey, ReCaptchaPrivateKey, ReCaptchaVersion)
- SecuritySettings (HoneypotEnabled, AdminAreaAllowedIpAddresses)

## External Dependencies
- Google reCAPTCHA API (v2/v3) — `GReCaptchaValidator` calls `https://www.google.com/recaptcha/api/siteverify`

## Migration Notes
- **Decision**: Rewrite
- Permission model: permissions assigned to roles, OR logic across customer's roles
- ACL: entity-level access control via `IAclSupported` marker interface
- Encryption: password hashing (SHA1/SHA256/MD5 → bcrypt/Argon2), text encrypt/decrypt
- Standard permissions installed during setup; plugins can add custom permissions
- In .NET 10: integrate with ASP.NET Core authorization policies
- reCAPTCHA: used on registration, login, contact, blog comments, news comments, product reviews, email-a-friend, vendor apply (7+ controllers, 7 model factories)
- Honeypot: hidden form field anti-spam on registration
- HTTPS: `NopHttpsRequirementAttribute` → ASP.NET Core HTTPS redirection middleware
- Anti-forgery: `AdminAntiForgeryAttribute`, `PublicAntiForgeryAttribute` → ASP.NET Core `[ValidateAntiForgeryToken]`
- Admin IP whitelist: `AdminValidateIpAddressAttribute` → custom middleware

## Acceptance Criteria
- [ ] `IPermissionService.Authorize` checks if any of the customer's roles has the requested permission
- [ ] `IAclService.Authorize` filters entities by customer role when `SubjectToAcl` is true
- [ ] Password hashing uses modern algorithm (bcrypt/Argon2) with legacy hash verification for migration
- [ ] Text encryption/decryption works for sensitive data storage
- [ ] reCAPTCHA validation calls Google API and blocks form submission on failure
- [ ] Honeypot hidden field rejects bot submissions on registration
