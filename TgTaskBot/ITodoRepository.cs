namespace TgTaskBot
{
    public interface ITodoRepository
    {
        Task AddTaskAsync(Todo task, long chatId);
        Task<IEnumerable<Todo>> GetTasksAsync(long chatId);
        Task<Todo> GetTaskByIdAsync(string taskId);
        Task<bool> DeleteTaskAsync(string taskId);
        Task<bool> CompleteTaskAsync(string taskId);
        Task<IEnumerable<Todo>> GetIncompleteTasksAsync(long chatId);
        Task<int> GetTotalTaskCountAsync(long chatId);
    }
}
