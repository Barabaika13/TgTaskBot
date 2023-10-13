using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgTaskBot
{
    public interface ITodoRepository
    {
        Task AddTaskAsync(Todo task, long chatId);
        Task<IEnumerable<Todo>> GetTasksAsync(long chatId);
        Task<Todo> GetTaskByIdAsync(string taskId);
        Task<bool> DeleteTaskAsync(string taskId);
        Task<bool> CompleteTaskAsync(string taskId);
    }
}
