
using MediatR;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public record CreateTaskCommand(CreateTaskDto Dto, Guid UserId) : IRequest<TaskItemDto>;
}