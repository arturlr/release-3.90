# Logging

## Bounded Context
Logging — application logging, customer activity logging, and log management.

## Legacy Source
- `src/Libraries/Nop.Services/Logging/` — all files
- Key interfaces: `ILogger`, `ICustomerActivityService`
- Domain: `Nop.Core.Domain.Logging`

## Key Entities
- Log (LogLevel, ShortMessage, FullMessage, IpAddress, CustomerId, PageUrl, ReferrerUrl)
- ActivityLog, ActivityLogType

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- `ILogger` stores logs in database (not file-based) — keep for admin UI log viewer
- Log levels: Debug, Info, Warning, Error, Fatal
- Customer activity logging: tracks admin and customer actions (e.g., "Edited product X")
- Activity log types: system-defined, enabled/disabled per type
- In .NET 10: also integrate with `Microsoft.Extensions.Logging` for structured logging to external sinks

## Acceptance Criteria
- [ ] Application logs stored in database with level, message, IP, customer, and URL
- [ ] Customer activity logs record actions with entity references
- [ ] Activity log types can be enabled/disabled
- [ ] Integration with `Microsoft.Extensions.Logging` for structured logging
