using Nop.Core;
using Nop.Core.Events;

namespace Nop.Services.Events;

/// <summary>
/// Convenience extension methods for publishing entity lifecycle events.
/// </summary>
public static class EventPublisherExtensions
{
    public static Task EntityInsertedAsync<T>(this IEventPublisher eventPublisher, T entity) where T : BaseEntity
        => eventPublisher.PublishAsync(new EntityInserted<T>(entity));

    public static Task EntityUpdatedAsync<T>(this IEventPublisher eventPublisher, T entity) where T : BaseEntity
        => eventPublisher.PublishAsync(new EntityUpdated<T>(entity));

    public static Task EntityDeletedAsync<T>(this IEventPublisher eventPublisher, T entity) where T : BaseEntity
        => eventPublisher.PublishAsync(new EntityDeleted<T>(entity));
}
