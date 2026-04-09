using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nop.Services.Events;

/// <summary>
/// Default event publisher — resolves all IConsumer&lt;T&gt; from DI and invokes them.
/// Errors in individual consumers are logged but do not prevent other consumers from running.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T eventMessage)
    {
        var consumers = _serviceProvider.GetServices<IConsumer<T>>();

        foreach (var consumer in consumers)
        {
            try
            {
                await consumer.HandleEventAsync(eventMessage);
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "Error handling event {EventType} in {ConsumerType}",
                        typeof(T).Name, consumer.GetType().Name);
                }
                catch
                {
                    // prevent cyclic errors if logging itself fails
                }
            }
        }
    }
}
