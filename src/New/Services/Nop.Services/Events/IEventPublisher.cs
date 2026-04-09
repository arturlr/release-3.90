namespace Nop.Services.Events;

/// <summary>
/// Event publisher — publishes domain events to all registered consumers.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T eventMessage);
}
