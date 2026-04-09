# Authorization

## Bounded Context
Authorization — role-based access control, admin area protection, and API authorization.

## Legacy Source
- `src/Presentation/Nop.Web.Framework/Security/` — AdminAuthorize attribute, etc.
- `src/Libraries/Nop.Services/Security/IPermissionService.cs`
- Custom MVC authorization filters

## Key Entities
- CustomerRole-based permissions
- Admin area authorization
- Store-level authorization

## External Dependencies
- ASP.NET MVC Authorization filters → ASP.NET Core Authorization policies

## Migration Notes
- **Decision**: Rewrite
- Legacy uses custom `[AdminAuthorize]` attribute and `IPermissionService`
- In .NET 10: use ASP.NET Core `[Authorize]` with custom policies and requirements
- Policy per permission: `[Authorize(Policy = "ManageProducts")]`
- `IAuthorizationHandler` implementations that call `IPermissionService`

## Acceptance Criteria
- [ ] Admin area requires authentication and admin role
- [ ] Permission-based authorization via ASP.NET Core policies maps to legacy permission records
- [ ] Unauthorized access returns 403 or redirects to access denied page
- [ ] Vendor-specific authorization restricts vendors to their own products/orders
