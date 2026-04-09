# Scheduled Tasks Services

## Bounded Context
Background task scheduling — task registration, execution, and lifecycle management.

## Legacy Source
- `src/Libraries/Nop.Services/Tasks/` — all files
- Key interfaces: `IScheduleTaskService`, `ITask`
- Key classes: `TaskManager` (singleton scheduler), `TaskThread` (per-interval thread), `Task` (execution wrapper)
- Concrete tasks: `UpdateExchangeRateTask` (Directory), `DeleteGuestsTask` (Customers), `QueuedMessagesSendTask` (Messages), `ClearLogTask` (Logging), `ClearCacheTask` (Caching), `KeepAliveTask` (Common)
- Domain: `Nop.Core.Domain.Tasks`

## Key Entities
- ScheduleTask (Name, Seconds interval, Type, Enabled, StopOnError, LastStart/End/Success)
- TaskManager — singleton that groups tasks by interval into TaskThread instances
- TaskThread — timer-based thread that executes grouped tasks at configured intervals
- ITask — interface implemented by each concrete background task (6 implementations)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Legacy uses custom `TaskManager` singleton with `TaskThread` timer threads grouped by interval
- `TaskManager.Initialize()` resolves `IScheduleTaskService`, groups tasks by `Seconds`, creates `TaskThread` per group
- Catch-up logic: tasks not run for >30 minutes are executed immediately on startup
- 6 concrete `ITask` implementations: `UpdateExchangeRateTask`, `DeleteGuestsTask`, `QueuedMessagesSendTask`, `ClearLogTask`, `ClearCacheTask`, `KeepAliveTask`
- In .NET 10: replace `TaskManager`/`TaskThread` with `IHostedService` / `BackgroundService` with DB-driven scheduling
- Keep `ScheduleTask` entity for admin UI management of task intervals
- Each concrete task becomes a scoped service resolved per execution cycle

## Acceptance Criteria
- [ ] Background tasks execute on configured intervals using `IHostedService`
- [ ] Task execution records last start, end, and success timestamps
- [ ] Tasks can be enabled/disabled and configured via admin UI
- [ ] Error handling: `StopOnError` flag controls whether task stops on exception
