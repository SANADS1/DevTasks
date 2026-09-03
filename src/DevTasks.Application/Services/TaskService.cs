using DevTasks.Application.DTOs;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Application.Interfaces;
using DevTasks.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace DevTasks.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<TaskService> _logger;
        private readonly IDistributedCache _cache;


        public TaskService(
        ITaskRepository taskRepository,
        IMapper mapper,
        ILogger<TaskService> logger,
        IDistributedCache cache)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<IEnumerable<TaskItemDto>> GetAllTasksAsync(Guid userId, bool isAdmin)
        {
            var cacheKey = isAdmin ? "tasks:all" : $"tasks:user:{userId}";
            var cached = await _cache.GetStringAsync(cacheKey);

            IEnumerable<TaskItem> tasks;
            if (cached != null)
            {
                tasks = JsonSerializer.Deserialize<IEnumerable<TaskItem>>(cached) ?? Enumerable.Empty<TaskItem>();
                _logger.LogInformation("Cache hit for task list ({CacheKey})", cacheKey);
            }
            else
            {
                tasks = isAdmin
                    ? await _taskRepository.GetAllAsync()
                    : await _taskRepository.GetAllByUserIdAsync(userId);

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(tasks),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                    });
                _logger.LogInformation("Cache miss for task list ({CacheKey}) — cached for 2 minutes", cacheKey);
            }

            return _mapper.Map<IEnumerable<TaskItemDto>>(tasks);
        }


        public async Task<TaskItemDto?> GetTaskByIdAsync(Guid id, Guid userId, bool isAdmin)
        {
            var cacheKey = $"task:{id}";
            var cached = await _cache.GetStringAsync(cacheKey);

            TaskItem? task;
            if (cached != null)
            {
                task = JsonSerializer.Deserialize<TaskItem>(cached);
                _logger.LogInformation("Cache hit for task {TaskId}", id);
            }
            else
            {
                task = await _taskRepository.GetByIdAsync(id);
                if (task != null)
                {
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(task),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                    _logger.LogInformation("Cache miss for task {TaskId} — cached for 5 minutes", id);
                }
            }

            if (task == null) return null;

            if (!isAdmin && task.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this task.");

            return _mapper.Map<TaskItemDto>(task);
        }

        public async Task<TaskItemDto> CreateTaskAsync(CreateTaskDto createTaskDto, Guid userId)
        {
            var task = _mapper.Map<TaskItem>(createTaskDto);
            task.UserId = userId;
            var createdTask = await _taskRepository.AddAsync(task);

            await InvalidateListCachesAsync(userId);
            _logger.LogInformation("Task {TaskId} created by user {UserId}", createdTask.Id, userId);

            return _mapper.Map<TaskItemDto>(createdTask);
        }

        public async Task<bool> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto, Guid userId, bool isAdmin)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return false;

            if (!isAdmin && task.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this task.");

            _mapper.Map(updateTaskDto, task);
            var result = await _taskRepository.UpdateAsync(task);

            await _cache.RemoveAsync($"task:{id}");
            await InvalidateListCachesAsync(userId);

            return result;
        }

       
        public async Task<bool> DeleteTaskAsync(Guid id, Guid userId, bool isAdmin)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null) return false;

            if (!isAdmin && task.UserId != userId)
            {
                _logger.LogWarning(
                    "Unauthorized delete attempt on task {TaskId} by user {UserId} (owner: {OwnerId})",
                    id, userId, task.UserId);
                throw new UnauthorizedAccessException("You do not have access to this task.");
            }

            var result = await _taskRepository.DeleteAsync(id);
            await _cache.RemoveAsync($"task:{id}");
            await InvalidateListCachesAsync(userId);

            _logger.LogInformation("Task {TaskId} deleted by user {UserId} (admin: {IsAdmin})", id, userId, isAdmin);
            return result;
        }

        private async Task InvalidateListCachesAsync(Guid userId)
        {
            await _cache.RemoveAsync("tasks:all");
            await _cache.RemoveAsync($"tasks:user:{userId}");
        }
    }
}