# Authorization

## Bounded Context
Authorization — role-based access control, admin area protection, and API authorization.

## Legacy Source
- `src/Presentation/Nop.Web.Framework/Security/` — AdminAuthorize attribute, etc.
- `src/Libraries/Nop.Services/Security/IPermissionService.cs`
- `src/Libraries/Nop.Services/Security/IPermissionProvider.cs` — permission registration interface
- `src/Libraries/Nop.Services/Security/StandardPermissionProvider.cs` — 40+ default permission constants
- Custom MVC authorization filters

## Key Entities
- CustomerRole-based permissions
- Admin area authorization
- Store-level authorization
- `IPermissionProvider` — interface for registering permission records (plugins implement this to add custom permissions)
- `StandardPermissionProvider` — default implementation defining 40+ admin and public permission constants (ManageProducts, ManageOrders, AccessAdminPanel, PublicStoreAllowNavigation, etc.)

## External Dependencies
- ASP.NET MVC Authorization filters → ASP.NET Core Authorization policies

## Migration Notes
- **Decision**: Rewrite
- Legacy uses custom `[AdminAuthorize]` attribute and `IPermissionService`
- In .NET 10: use ASP.NET Core `[Authorize]` with custom policies and requirements
- Policy per permission: `[Authorize(Policy = "ManageProducts")]`
- `IAuthorizationHandler` implementations that call `IPermissionService`
- `IPermissionProvider` → keep interface; plugins register permissions via DI; `StandardPermissionProvider` migrated as-is with all 40+ permission constants
- `PermissionService.InstallPermissions()` reads `IPermissionProvider` implementations to seed permission records on startup

## Acceptance Criteria
- [ ] Admin area requires authentication and admin role
- [ ] Permission-based authorization via ASP.NET Core policies maps to legacy permission records
- [ ] Unauthorized access returns 403 or redirects to access denied page
- [ ] Vendor-specific authorization restricts vendors to their own products/orders
- [ ] `IPermissionProvider` implementations (including `StandardPermissionProvider`) are discovered via DI and seed permission records on installation
