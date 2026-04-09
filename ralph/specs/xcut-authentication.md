# Authentication

## Bounded Context
Authentication — forms authentication, cookie management, and authentication service abstraction.

## Legacy Source
- `src/Libraries/Nop.Services/Authentication/FormsAuthenticationService.cs`
- `src/Libraries/Nop.Services/Authentication/IAuthenticationService.cs`
- ASP.NET Forms Authentication in web.config

## Key Entities
- Authentication cookie/ticket
- Customer identity

## External Dependencies
- ASP.NET Forms Authentication → ASP.NET Core Cookie Authentication

## Migration Notes
- **Decision**: Rewrite
- Legacy uses Forms Authentication with custom `IAuthenticationService`
- In .NET 10: use ASP.NET Core Cookie Authentication middleware
- `SignIn` / `SignOut` operations
- Customer impersonation support (admin impersonates customer)
- Remember-me functionality

## Acceptance Criteria
- [ ] Cookie-based authentication using ASP.NET Core authentication middleware
- [ ] Sign-in creates authentication cookie; sign-out removes it
- [ ] Customer impersonation allows admin to browse as another customer
- [ ] Remember-me extends cookie lifetime
