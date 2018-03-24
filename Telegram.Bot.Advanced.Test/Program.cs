using System;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Test
{
    class Program
    {
        private static Dispatcher Dispatcher { get; } = new Dispatcher(typeof(Controller));

        static void Main(string[] args)
        {
            var bot = new TelegramBotClient("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");

            // Disable WebHook
            bot.DeleteWebhookAsync();

            bot.OnUpdate += SendToDispatcher;

            bot.StartReceiving();
            Console.WriteLine($"Start listening.");
            Console.ReadLine();
            bot.StopReceiving();
        }

        public static void SendToDispatcher(object sender, UpdateEventArgs args) {
            Dispatcher.DispatchUpdate(args.Update);
        }
    }
}
