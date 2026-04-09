using Nop.Core.Data;
using Nop.Core.Domain.Tasks;

namespace Nop.Services.Tasks;

public class ScheduleTaskService : IScheduleTaskService
{
    private readonly IRepository<ScheduleTask> _taskRepository;

    public ScheduleTaskService(IRepository<ScheduleTask> taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public Task DeleteTaskAsync(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _taskRepository.Delete(task);
        return Task.CompletedTask;
    }

    public Task<ScheduleTask?> GetTaskByIdAsync(int taskId)
    {
        return Task.FromResult(taskId == 0 ? null : _taskRepository.GetById(taskId));
    }

    public Task<ScheduleTask?> GetTaskByTypeAsync(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return Task.FromResult<ScheduleTask?>(null);

        var task = _taskRepository.Table
            .Where(st => st.Type == type)
            .OrderByDescending(t => t.Id)
            .FirstOrDefault();

        return Task.FromResult<ScheduleTask?>(task);
    }

    public Task<IList<ScheduleTask>> GetAllTasksAsync(bool showHidden = false)
    {
        var query = _taskRepository.Table;

        if (!showHidden)
            query = query.Where(t => t.Enabled);

        IList<ScheduleTask> tasks = query.OrderByDescending(t => t.Seconds).ToList();
        return Task.FromResult(tasks);
    }

    public Task InsertTaskAsync(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _taskRepository.Insert(task);
        return Task.CompletedTask;
    }

    public Task UpdateTaskAsync(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _taskRepository.Update(task);
        return Task.CompletedTask;
    }
}
