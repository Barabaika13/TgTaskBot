using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TgTaskBot
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
            var botClient = new TelegramBotClient(botToken);
            ITodoRepository dapperTodoRepository = new DapperTodoRepository(Config.SqlConnectionString);
            using CancellationTokenSource cts = new();
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            var service = new BotService(botClient, dapperTodoRepository);

            botClient.StartReceiving(
                updateHandler: service.HandleUpdateAsync,
                pollingErrorHandler: service.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            cts.Cancel();
        }
    }
}