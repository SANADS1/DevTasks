using MediatR;
using DevTasks.Application.DTOs;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevTasks.Application.Features.Tasks.Queries
{
    public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskItemDto?>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetTaskByIdQueryHandler> _logger;

        public GetTaskByIdQueryHandler(
            ITaskRepository taskRepository,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<GetTaskByIdQueryHandler> logger)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<TaskItemDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"task:{request.Id}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

            TaskItem? task;
            if (cached != null)
            {
                task = JsonSerializer.Deserialize<TaskItem>(cached);
                _logger.LogInformation("Cache hit for task {TaskId}", request.Id);
            }
            else
            {
                task = await _taskRepository.GetByIdAsync(request.Id);
                if (task != null)
                {
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(task),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        },
                        cancellationToken);
                    _logger.LogInformation("Cache miss for task {TaskId} — cached for 5 minutes", request.Id);
                }
            }

            if (task == null) return null;

            if (!request.IsAdmin && task.UserId != request.UserId)
                throw new UnauthorizedAccessException("You do not have access to this task.");

            return _mapper.Map<TaskItemDto>(task);
        }
    }
}