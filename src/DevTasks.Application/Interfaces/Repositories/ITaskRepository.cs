using DevTasks.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace DevTasks.Application.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<IEnumerable<TaskItem>> GetAllAsync();
        Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId);
        Task<TaskItem?> GetByIdAsync(Guid id);
        Task<TaskItem> AddAsync(TaskItem task);
        Task<bool> UpdateAsync(TaskItem task);
        Task<bool> DeleteAsync(Guid id);
    }
}