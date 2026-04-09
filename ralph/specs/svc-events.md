# Event Services

## Bounded Context
Domain event system — event publishing and consumer infrastructure.

## Legacy Source
- `src/Libraries/Nop.Services/Events/` — EventPublisher, SubscriptionService
- `src/Libraries/Nop.Core/Events/` — EntityInserted, EntityUpdated, EntityDeleted
- `src/Libraries/Nop.Services/Caching/` — CacheEventConsumer

## Key Entities
- `IEventPublisher` — publishes domain events
- `IConsumer<T>` — handles domain events
- `EntityInserted<T>`, `EntityUpdated<T>`, `EntityDeleted<T>` — standard events

## External Dependencies
- None (or MediatR as replacement)

## Migration Notes
- **Decision**: Rewrite
- Legacy: custom event publisher scans for `IConsumer<T>` implementations via DI
- Consider MediatR `INotification` / `INotificationHandler<T>` as replacement
- Cache event consumers invalidate cache on entity changes
- Plugins can register their own event consumers

## Acceptance Criteria
- [ ] Domain events published after entity insert/update/delete operations
- [ ] All registered consumers invoked for matching event types
- [ ] Cache invalidation consumers clear relevant cache entries on entity changes
