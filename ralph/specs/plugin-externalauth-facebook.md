# Plugin: ExternalAuth.Facebook

## Bounded Context
External authentication — Facebook OAuth login.

## Legacy Source
- `src/Plugins/Nop.Plugin.ExternalAuth.Facebook/`

## Key Entities
- Implements `IExternalAuthenticationMethod`

## External Dependencies
- Facebook OAuth API
- `Microsoft.AspNetCore.Authentication.Facebook` in .NET 10

## Migration Notes
- **Decision**: Rewrite using ASP.NET Core Facebook authentication
- Legacy uses custom OAuth flow → use built-in `AddFacebook()` authentication

## Acceptance Criteria
- [ ] Facebook login redirects to Facebook OAuth and returns authenticated user
- [ ] New users auto-registered or linked to existing account by email
- [ ] Plugin configuration allows setting App ID and App Secret
