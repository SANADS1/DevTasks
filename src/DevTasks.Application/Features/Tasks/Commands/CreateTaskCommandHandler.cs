using MediatR;
using DevTasks.Application.DTOs;
using DevTasks.Application.Interfaces.Repositories;
using DevTasks.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskItemDto>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreateTaskCommandHandler> _logger;

        public CreateTaskCommandHandler(
            ITaskRepository taskRepository,
            IMapper mapper,
            IDistributedCache cache,
            ILogger<CreateTaskCommandHandler> logger)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<TaskItemDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = _mapper.Map<TaskItem>(request.Dto);
            task.UserId = request.UserId;
            var createdTask = await _taskRepository.AddAsync(task);

            await _cache.RemoveAsync("tasks:all");
            await _cache.RemoveAsync($"tasks:user:{request.UserId}");

            _logger.LogInformation("Task {TaskId} created by user {UserId}", createdTask.Id, request.UserId);

            return _mapper.Map<TaskItemDto>(createdTask);
        }
    }
}