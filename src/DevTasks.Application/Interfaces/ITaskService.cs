using DevTasks.Application.DTOs;

namespace DevTasks.Application.Interfaces
{
    public interface ITaskService
    {
        Task<IEnumerable<TaskItemDto>> GetAllTasksAsync(Guid userId, bool isAdmin);
        Task<TaskItemDto?> GetTaskByIdAsync(Guid id, Guid userId, bool isAdmin);
        Task<TaskItemDto> CreateTaskAsync(CreateTaskDto createTaskDto, Guid userId);
        Task<bool> UpdateTaskAsync(Guid id, UpdateTaskDto updateTaskDto, Guid userId, bool isAdmin);
        Task<bool> DeleteTaskAsync(Guid id, Guid userId, bool isAdmin);
    }
}