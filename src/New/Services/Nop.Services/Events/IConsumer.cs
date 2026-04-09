namespace Nop.Services.Events;

/// <summary>
/// Consumer interface — handles domain events of type <typeparamref name="T"/>.
/// </summary>
public interface IConsumer<in T>
{
    Task HandleEventAsync(T eventMessage);
}
