using MediatR;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public record UpdateTaskCommand(Guid Id, UpdateTaskDto Dto, Guid UserId, bool IsAdmin) : IRequest<bool>;
}