using MediatR;
using DevTasks.Application.Interfaces.Repositories;
using MapsterMapper;
using Microsoft.Extensions.Caching.Distributed;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, bool>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public UpdateTaskCommandHandler(ITaskRepository taskRepository, IMapper mapper, IDistributedCache cache)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<bool> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null) return false;

            if (!request.IsAdmin && task.UserId != request.UserId)
                throw new UnauthorizedAccessException("You do not have access to this task.");

            _mapper.Map(request.Dto, task);
            var result = await _taskRepository.UpdateAsync(task);

            await _cache.RemoveAsync($"task:{request.Id}", cancellationToken);
            await _cache.RemoveAsync("tasks:all", cancellationToken);
            await _cache.RemoveAsync($"tasks:user:{task.UserId}", cancellationToken);

            return result;
        }
    }
}