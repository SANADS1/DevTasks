using DevTasks.Application.Interfaces;
using DevTasks.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DevTasks.Application.Services
{
    public class OverdueTaskChecker : IOverdueTaskChecker
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILogger<OverdueTaskChecker> _logger;

        public OverdueTaskChecker(ITaskRepository taskRepository, ILogger<OverdueTaskChecker> logger)
        {
            _taskRepository = taskRepository;
            _logger = logger;
        }

        public async Task CheckOverdueTasksAsync()
        {
            var allTasks = await _taskRepository.GetAllAsync();
            var overdue = allTasks.Where(t =>
                !t.IsCompleted &&
                t.DueDate.HasValue &&
                t.DueDate.Value.Date < DateTime.UtcNow.Date);

            var overdueCount = 0;
            foreach (var task in overdue)
            {
                _logger.LogWarning(
                    "Task {TaskId} ({Title}) is overdue — was due {DueDate}, owned by user {UserId}",
                    task.Id, task.Title, task.DueDate, task.UserId);
                overdueCount++;
            }

            _logger.LogInformation("Overdue task check complete — {OverdueCount} overdue task(s) found", overdueCount);
        }
    }
}