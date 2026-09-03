using MediatR;
using DevTasks.Application.DTOs;

namespace DevTasks.Application.Features.Tasks.Queries
{
    public record GetAllTasksQuery(Guid UserId, bool IsAdmin) : IRequest<IEnumerable<TaskItemDto>>;
}