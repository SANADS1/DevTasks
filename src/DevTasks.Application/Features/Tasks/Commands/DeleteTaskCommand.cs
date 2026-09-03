using MediatR;

namespace DevTasks.Application.Features.Tasks.Commands
{
    public record DeleteTaskCommand(Guid Id, Guid UserId, bool IsAdmin) : IRequest<bool>;
}