namespace Nop.Services.Tasks;

/// <summary>
/// Interface that should be implemented by each background task.
/// </summary>
public interface ITask
{
    Task ExecuteAsync();
}
