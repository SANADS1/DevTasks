using MediatR;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Features.Tasks.Queries
{
    public record GetTaskByIdQuery(Guid Id, Guid UserId, bool IsAdmin) : IRequest<TaskItemDto?>;
}