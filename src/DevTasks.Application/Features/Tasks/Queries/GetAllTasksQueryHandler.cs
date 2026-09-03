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
    public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, IEnumerable<TaskItemDto>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetAllTasksQueryHandler> _logger;

        public GetAllTasksQueryHandler(
            ITaskRepository taskRepository,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<GetAllTasksQueryHandler> logger)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskItemDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = request.IsAdmin ? "tasks:all" : $"tasks:user:{request.UserId}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

            IEnumerable<TaskItem> tasks;
            if (cached != null)
            {
                tasks = JsonSerializer.Deserialize<IEnumerable<TaskItem>>(cached) ?? Enumerable.Empty<TaskItem>();
                _logger.LogInformation("Cache hit for task list ({CacheKey})", cacheKey);
            }
            else
            {
                tasks = request.IsAdmin
                    ? await _taskRepository.GetAllAsync()
                    : await _taskRepository.GetAllByUserIdAsync(request.UserId);

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(tasks),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    },
                    cancellationToken);
                _logger.LogInformation("Cache miss for task list ({CacheKey}) — cached for 2 minutes", cacheKey);
            }

            return _mapper.Map<IEnumerable<TaskItemDto>>(tasks);
        }
    }
}