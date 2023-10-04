using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Npgsql;
using Dapper;

namespace TgTaskBot
{
    public class BotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly UserStateService _userStateService;
        //private Dictionary<string, Todo> taskList = new Dictionary<string, Todo>();

        public BotService(ITelegramBotClient botClient)
        {
            _botClient = botClient;
            _userStateService = new();
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                if (update.Message is null)
                    return;
                var message = update.Message;
                //var fromUser = message.From;
                if (message.Text is null)
                    return;

                var messageText = message.Text;
                var chatId = message.Chat.Id;
                Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
                if (messageText.StartsWith("/"))
                {
                    await HandleCommands(chatId, messageText, cancellationToken);
                }
                else
                {
                    await HandleMessageByState(messageText, chatId, cancellationToken);
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery is null)
                    return;

                var callbackQuery = update.CallbackQuery;
                //var fromUser = callbackQuery.From;
                if (callbackQuery.Data == null)
                    return;

                var callbackData = callbackQuery.Data;
                //var chatId = callbackQuery.Message.Chat.Id;
                if (callbackData.StartsWith("delete_"))
                {
                    var taskId = callbackData.Replace("delete_", "");
                    var taskName = await GetTaskNameByIdAsync(taskId, cancellationToken);
                    if (!string.IsNullOrEmpty(taskName))
                    {
                        await DeleteTaskByIdAsync(taskId, cancellationToken);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Task '{taskName}' deleted.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Task already deleted.", cancellationToken: cancellationToken);
                    }
                }

                else if (callbackData.StartsWith("complete_"))
                {
                    var taskId = callbackData.Replace("complete_", "");
                    string? taskName = null;
                    using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
                    {
                        string sql = "SELECT name, isdone FROM tasks WHERE id = @taskId";
                        var task = await conn.QueryFirstOrDefaultAsync<Todo>(sql, new { taskId });
                        if (task != null && !task.IsDone)
                        {
                            string completeTaskSql = "UPDATE tasks SET isdone = true WHERE id = @taskId";
                            await conn.ExecuteAsync(completeTaskSql, new { taskId });
                            taskName = task.Name;
                        }
                    }
                    if (!string.IsNullOrEmpty(taskName))
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Task '{taskName}' marked as completed.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Task already marked as completed.", cancellationToken: cancellationToken);
                    }
                    //string taskName = taskList[taskId].Name;
                    //taskList[taskId].IsDone = true;
                    //await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Task '{taskName}' marked as completed.", cancellationToken: cancellationToken);

                }


                else if (callbackData.StartsWith("show_"))
                {
                    var taskId = callbackData.Replace("show_", "");
                    var taskName = await GetTaskNameByIdAsync(taskId, cancellationToken);
                    //string taskName = taskList[taskId].Name;
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"Task '{taskName}' is in your list", cancellationToken: cancellationToken);

                }
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        async Task HandleMessageByState(string messageText, long chatId, CancellationToken cancellationToken)
        {
            UserState userState = _userStateService.GetState(chatId);
            switch (userState)
            {
                case UserState.CreatingTask:
                    await AddTask(chatId, messageText, cancellationToken);
                    break;

                case UserState.TaskList:
                    break;

                case UserState.CompletingTask:
                    break;

                case UserState.DeletingTask:
                    break;

                case UserState.NoState:
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Sorry, I don't understand you. Please try again",
                        cancellationToken: cancellationToken
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(userState));
            }
        }

        async Task HandleCommands(long chatId, string command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case "/start":
                case "/start@bright_tasks_bot":
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Welcome! Please check the menu to see what I can do!",
                        cancellationToken: cancellationToken
                        );
                    break;


                case "/help":
                case "/help@bright_tasks_bot":
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Available commands:\n" +
                              "/start - start interacting with the bot\n" +
                              "/help - see the available commands list\n" +
                              "/create - create a new task\n" +
                              "/list - see your task list\n" +
                              "/complete - mark a task as completed\n" +
                              "/delete - delete a task from your task list\n",
                        cancellationToken: cancellationToken
                    );
                    break;


                case "/create":
                case "/create@bright_tasks_bot":
                    _userStateService.SetState(chatId, UserState.CreatingTask);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Enter your task",
                        cancellationToken: cancellationToken
                    );
                    break;

                case "/list":
                case "/list@bright_tasks_bot":
                    _userStateService.SetState(chatId, UserState.TaskList);
                    await ShowTaskList(chatId,cancellationToken);
                    break;

                case "/complete":
                case "/complete@bright_tasks_bot":
                    _userStateService.SetState(chatId, UserState.CompletingTask);
                    await CompleteTask(chatId, cancellationToken);
                    break;

                case "/delete":
                case "/delete@bright_tasks_bot":
                    _userStateService.SetState(chatId, UserState.DeletingTask);
                    await DeleteTask(chatId, cancellationToken);
                    break;

                default:
                    _userStateService.SetState(chatId, UserState.NoState);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "This command is unknown to me. Please check my command list and try again",
                        cancellationToken: cancellationToken
                    );
                    break;
            }
        }

        async Task AddTask(long chatId, string messageText, CancellationToken cancellationToken)
        {
            var todo = new Todo(messageText);
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = $"INSERT INTO tasks(id, name, isdone, chatid) VALUES (@id, @name, @isdone, @chatid)";
                await conn.ExecuteAsync(sql, new { id = todo.Id, name = todo.Name, isdone = todo.IsDone, chatid = chatId });
            }

            //taskList.Add(todo.Id, todo);
            Console.WriteLine($"ID: {todo.Id}, task: {todo.Name}");
            await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Task <b>{todo.Name}</b> created!",
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                    );
        }

        async Task ShowTaskList(long chatId, CancellationToken cancellationToken)
        {
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "SELECT id, name, isdone, chatid FROM tasks WHERE chatid = @chatId";
                //IEnumerable<Todo> todoListCount = conn.Query<Todo>(sql, new { chatId });
                var todoList = await conn.QueryAsync<Todo>(sql, new { chatId });
                foreach (Todo todo in todoList)
                {
                    Console.WriteLine(todo.Name);
                }

                if (todoList.Any())
                {
                    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(GetKeyboardButtonsAndShow(todoList));
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Your task list \U0001F447",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken
                        );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                       chatId: chatId,
                       text: "You don't have any tasks in your list. To start adding tasks, use the /create command",
                       cancellationToken: cancellationToken
                       );
                }
            }         
        }

        private InlineKeyboardButton[][] GetKeyboardButtonsAndShow(IEnumerable<Todo> tasks)
        {
            //int count = taskList.Count;
            //var buttons = new InlineKeyboardButton[count][];
            //var values = taskList.Values.ToArray();
            //var names = values.Select(todo => (todo.IsDone ? "\u2705" : "\u25FD") + todo.Name).ToArray();

            //for (int i = 0; i < count; i++)
            //{
            //    buttons[i] = new[] { new InlineKeyboardButton(names[i]) { CallbackData = $"show_{values[i].Id}" } };
            //}
            //return buttons;

            int count = tasks.Count();
            var buttons = new InlineKeyboardButton[count][];

            for (int i = 0; i < count; i++)
            {
                var todo = tasks.ElementAt(i);
                var buttonText = (todo.IsDone ? "\u2705" : "\u25FD") + todo.Name;
                var callbackData = $"show_{todo.Id}";
                buttons[i] = new[] { new InlineKeyboardButton(buttonText) { CallbackData = callbackData } };                
            }
            return buttons;
        }

        async Task DeleteTask(long chatId, CancellationToken cancellationToken)
        {
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "SELECT id, name, isdone, chatid FROM tasks WHERE chatid = @chatId";
                var todoList = await conn.QueryAsync<Todo>(sql, new { chatId });

                if (todoList.Any())
                {
                    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(GetKeyboardButtonsAndDelete(todoList));
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Select a task you want to delete",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                       chatId: chatId,
                       text: "You don't have any tasks in your list. To start adding tasks, use the /create command",
                       cancellationToken: cancellationToken
                       );
                }
            }
        }

        private InlineKeyboardButton[][] GetKeyboardButtonsAndDelete(IEnumerable<Todo> tasks)
        {
            //int count = taskList.Count;
            //var buttons = new InlineKeyboardButton[count][];
            //var values = taskList.Values.ToArray();
            //var keys = taskList.Keys.ToArray();

            //for (int i = 0; i < count; i++)
            //{
            //    buttons[i] = new[]
            //    {
            //        new InlineKeyboardButton("\u274C" + " " + values[i].Name) { CallbackData = $"delete_{keys[i]}"},
            //    };
            //}
            //return buttons;

            int count = tasks.Count();
            var buttons = new InlineKeyboardButton[count][];
            for (int i = 0; i < count; i++)
            {
                var todo = tasks.ElementAt(i);               
                buttons[i] = new[] { new InlineKeyboardButton("\u274C" + " " + todo.Name) { CallbackData = $"delete_{todo.Id}" } };
            }
            return buttons;
        }

        async Task CompleteTask(long chatId, CancellationToken cancellationToken)
        {
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "SELECT id, name FROM tasks WHERE chatid = @chatId AND isdone = false";
                var incompleteTasks = await conn.QueryAsync<Todo>(sql, new { chatId });
                if (incompleteTasks.Any())
                {
                    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(GetKeyboardButtonsAndComplete(incompleteTasks));
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Select a task to mark as completed:",
                        replyMarkup: inlineKeyboard,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    string allCompletedSql = "SELECT COUNT(*) FROM tasks WHERE chatid = @chatId";
                    var totalTasksCount = await conn.ExecuteScalarAsync<int>(allCompletedSql, new { chatId });
                    if (totalTasksCount > 0)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "You have all tasks marked as completed in your list",
                            cancellationToken: cancellationToken
                            );
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "You don't have any tasks in your list. To start adding tasks, use the /create command",
                            cancellationToken: cancellationToken
                            );
                    }
                }
            }

            //var values = taskList.Values.ToArray();
            //var incomplete = values.Where(todo => todo.IsDone == false).ToArray();

            //if (values.Length > 0 && incomplete.Length != 0)
            //{
            //    InlineKeyboardMarkup inlineKeyboard = new InlineKeyboardMarkup(GetKeyboardButtonsAndComplete());
            //    await _botClient.SendTextMessageAsync(
            //        chatId: chatId,
            //        text: "Select a task to mark as completed:",
            //        replyMarkup: inlineKeyboard,
            //        cancellationToken: cancellationToken
            //    );
            //}

            //else if (values.Length > 0 && incomplete.Length == 0)
            //{
            //    await _botClient.SendTextMessageAsync(
            //       chatId: chatId,
            //       text: "You have all tasks in your list marked as completed",
            //       cancellationToken: cancellationToken
            //       );
            //}

            //else
            //{
            //    await _botClient.SendTextMessageAsync(
            //       chatId: chatId,
            //       text: "You don't have any tasks in your list. To start adding tasks, use the /create command",
            //       cancellationToken: cancellationToken
            //       );
            //}
        }

        private InlineKeyboardButton[][] GetKeyboardButtonsAndComplete(IEnumerable<Todo> incompleteTasks)
        {
            //var allTodos = taskList.Values.ToArray();
            //var incompleteTodos = allTodos.Where(todo => todo.IsDone == false).ToArray();
            //int count = incompleteTodos.Length;
            //var buttons = new InlineKeyboardButton[count][];

            //var incompleteTaskKeys = allTodos.Where(x => x.IsDone == false).ToArray();

            //for (int i = 0; i < count; i++)
            //{
            //    buttons[i] = new[]
            //       {
            //            new InlineKeyboardButton("\u25FD" + " " + incompleteTodos[i].Name) { CallbackData = $"complete_{incompleteTaskKeys[i].Id}" },
            //       };

            //}
            //return buttons;
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var task in incompleteTasks)
            {
                buttons.Add(new[] { new InlineKeyboardButton("\u25FD" + " " + task.Name) { CallbackData = $"complete_{task.Id}" } });

            }
            return buttons.ToArray();
        }

        async Task<string> GetTaskNameByIdAsync(string taskId, CancellationToken cancellationToken)
        {
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "SELECT name FROM tasks WHERE id = @taskId";
                return await conn.ExecuteScalarAsync<string>(sql, new { taskId });
            }
        }

        async Task<bool> DeleteTaskByIdAsync(string taskId, CancellationToken cancellationToken)
        {
            using (var conn = new NpgsqlConnection(Config.SqlConnectionString))
            {
                string sql = "DELETE FROM tasks WHERE id = @taskId";
                var affectedRows = await conn.ExecuteAsync(sql, new { taskId });

                // If affectedRows > 0, a task was deleted; otherwise, the task didn't exist
                return affectedRows > 0;
            }
        }   

    }
}
