# Error Handling

## Bounded Context
Error handling — exception handling, error pages, and validation patterns.

## Legacy Source
- `src/Libraries/Nop.Services/Logging/ILogger.cs` — error logging
- `src/Presentation/Nop.Web.Framework/Controllers/` — base controller error handling
- `src/Libraries/Nop.Services/` — validation patterns throughout services (return error lists)
- `ATXDocumentation/behavior/error-handling.md` — comprehensive error handling documentation

## Key Entities
- Log entity for error persistence
- Validation result patterns (lists of error strings)

## External Dependencies
- None

## Migration Notes
- **Decision**: Rewrite
- Legacy pattern: services return `IList<string>` for validation errors (e.g., `AddToCart` returns error messages)
- Controllers catch exceptions and log via `ILogger`
- Custom error pages for 404, 500
- In .NET 10: use ASP.NET Core exception handling middleware, `ProblemDetails`, and `IExceptionHandler`
- Consider `FluentResults` or `OneOf` for Result pattern in services
- Global exception handler middleware for unhandled exceptions

## Acceptance Criteria
- [ ] Global exception handler catches unhandled exceptions, logs them, and returns appropriate error response
- [ ] Custom error pages for 404 and 500 errors
- [ ] Service validation errors returned as structured results (not thrown exceptions)
- [ ] All exceptions logged with full context (URL, IP, customer, stack trace)
