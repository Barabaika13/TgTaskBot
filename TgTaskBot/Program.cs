using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgTaskBot
{
    public class Program
    {
        static async Task Main(string[] args)
        {            
            var botClient = new TelegramBotClient("6605290851:AAHBZb--5TxRwmquaJePPXGjMLINKobyWHI");

            using CancellationTokenSource cts = new();

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };
            var service = new BotService(botClient);

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