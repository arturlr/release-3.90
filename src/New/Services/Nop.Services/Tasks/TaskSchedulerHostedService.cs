using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nop.Core.Domain.Tasks;

namespace Nop.Services.Tasks;

/// <summary>
/// BackgroundService-based scheduler replacing legacy TaskManager/TaskThread/Task.
/// Loads all enabled ScheduleTask records, runs each on its configured interval,
/// records timestamps, and handles errors per the StopOnError flag.
/// </summary>
public class TaskSchedulerHostedService : BackgroundService
{
    private const int NotRunTasksSeconds = 60 * 30; // 30 minutes
    private const int StartupDelaySeconds = 30;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TaskSchedulerHostedService> _logger;

    public TaskSchedulerHostedService(
        IServiceProvider serviceProvider,
        ILogger<TaskSchedulerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish starting before running tasks
        await Task.Delay(TimeSpan.FromSeconds(StartupDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduleTasks = await LoadEnabledTasksAsync();

            foreach (var scheduleTask in scheduleTasks)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (!IsDue(scheduleTask))
                    continue;

                await RunTaskAsync(scheduleTask, stoppingToken);
            }

            // Poll every 30 seconds for due tasks
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task<IList<ScheduleTask>> LoadEnabledTasksAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduleTaskService = scope.ServiceProvider.GetRequiredService<IScheduleTaskService>();
            return await scheduleTaskService.GetAllTasksAsync(showHidden: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scheduled tasks");
            return [];
        }
    }

    private static bool IsDue(ScheduleTask task)
    {
        if (task.Seconds <= 0)
            return false;

        // Never run → due immediately
        if (!task.LastStartUtc.HasValue)
            return true;

        return task.LastStartUtc.Value.AddSeconds(task.Seconds) <= DateTime.UtcNow;
    }

    private async Task RunTaskAsync(ScheduleTask scheduleTask, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleTaskService = scope.ServiceProvider.GetRequiredService<IScheduleTaskService>();

        try
        {
            var taskType = Type.GetType(scheduleTask.Type ?? string.Empty);
            if (taskType is null)
            {
                _logger.LogWarning("Scheduled task type '{Type}' could not be resolved", scheduleTask.Type);
                return;
            }

            if (scope.ServiceProvider.GetService(taskType) is not ITask taskInstance)
            {
                _logger.LogWarning("Scheduled task type '{Type}' does not implement ITask", scheduleTask.Type);
                return;
            }

            // Update last start
            scheduleTask.LastStartUtc = DateTime.UtcNow;
            await scheduleTaskService.UpdateTaskAsync(scheduleTask);

            await taskInstance.ExecuteAsync();

            // Update success timestamps
            scheduleTask.LastEndUtc = DateTime.UtcNow;
            scheduleTask.LastSuccessUtc = scheduleTask.LastEndUtc;
            await scheduleTaskService.UpdateTaskAsync(scheduleTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running scheduled task '{Name}'", scheduleTask.Name);

            scheduleTask.LastEndUtc = DateTime.UtcNow;
            await scheduleTaskService.UpdateTaskAsync(scheduleTask);

            if (scheduleTask.StopOnError)
            {
                scheduleTask.Enabled = false;
                await scheduleTaskService.UpdateTaskAsync(scheduleTask);
            }
        }
    }
}
