using MediatR;
using DevTasks.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DeleteTaskCommandHandler> _logger;

        public DeleteTaskCommandHandler(
            ITaskRepository taskRepository,
            IDistributedCache cache,
            ILogger<DeleteTaskCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null) return false;

            if (!request.IsAdmin && task.UserId != request.UserId)
            {
                _logger.LogWarning(
                    "Unauthorized delete attempt on task {TaskId} by user {UserId} (owner: {OwnerId})",
                    request.Id, request.UserId, task.UserId);
                throw new UnauthorizedAccessException("You do not have access to this task.");
            }

            var result = await _taskRepository.DeleteAsync(request.Id);

            await _cache.RemoveAsync($"task:{request.Id}", cancellationToken);
            await _cache.RemoveAsync("tasks:all", cancellationToken);
            await _cache.RemoveAsync($"tasks:user:{task.UserId}", cancellationToken);

            _logger.LogInformation("Task {TaskId} deleted by user {UserId} (admin: {IsAdmin})",
                request.Id, request.UserId, request.IsAdmin);

            return result;
        }
    }
}