namespace DevTasks.Application.Interfaces
{
    public interface IOverdueTaskChecker
    {
        Task CheckOverdueTasksAsync();
    }
}