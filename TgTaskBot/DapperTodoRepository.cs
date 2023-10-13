using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Telegram.Bot.Types;

namespace TgTaskBot
{
    public class DapperTodoRepository : ITodoRepository
    {
        private readonly string _connectionString;

        public DapperTodoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task AddTaskAsync(Todo task, long chatId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string sql = $"INSERT INTO tasks(id, name, isdone, chatid) VALUES (@id, @name, @isdone, @chatid)";
                await conn.ExecuteAsync(sql, new { id = task.Id, name = task.Name, isdone = task.IsDone, chatid = chatId });
            }
        }

        public async Task<IEnumerable<Todo>> GetTasksAsync(long chatId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string sql = "SELECT id, name, isdone, chatid FROM tasks WHERE chatid = @chatId";
                var todoList = await conn.QueryAsync<Todo>(sql, new {chatId});
                return todoList;                
            }
        }

        public async Task<Todo> GetTaskByIdAsync(string taskId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string sql = "SELECT id, name, isdone, chatid FROM tasks WHERE id = @taskId";
                var task = await conn.QueryFirstOrDefaultAsync<Todo>(sql, new {taskId});
                return task;
            }
        }

        public async Task<bool> DeleteTaskAsync(string taskId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string sql = "DELETE FROM tasks WHERE id = @taskId";
                int affectedRows = await conn.ExecuteAsync(sql, new { taskId });
                if (affectedRows > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> CompleteTaskAsync(string taskId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                string sql = "UPDATE tasks SET isdone = true WHERE id = @taskId";
                int affectedRows = await conn.ExecuteAsync(sql, new {taskId});
                if (affectedRows > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
