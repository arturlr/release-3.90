# Scheduled Tasks Services

## Bounded Context
Background task scheduling — task registration, execution, and lifecycle management.

## Legacy Source
- `src/Libraries/Nop.Services/Tasks/` — all files
- Key interfaces: `IScheduleTaskService`, `ITask`
- Domain: `Nop.Core.Domain.Tasks`

## Key Entities
- ScheduleTask (Name, Seconds interval, Type, Enabled, StopOnError, LastStart/End/Success)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Legacy uses custom task scheduler with `ScheduleTask` records in DB
- Tasks: send queued emails, delete guests, update exchange rates, clear cache, keep-alive, etc.
- In .NET 10: use `IHostedService` / `BackgroundService` with DB-driven scheduling
- Keep `ScheduleTask` entity for admin UI management of task intervals

## Acceptance Criteria
- [ ] Background tasks execute on configured intervals using `IHostedService`
- [ ] Task execution records last start, end, and success timestamps
- [ ] Tasks can be enabled/disabled and configured via admin UI
- [ ] Error handling: `StopOnError` flag controls whether task stops on exception
