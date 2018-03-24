using System;
using Telegram.Bot.Args;

namespace Telegram.Bot.Advanced.Test
{
    class Program
    {
        private static Dispatcher Dispatcher { get; } = new Dispatcher(typeof(Controller));

        static void Main(string[] args)
        {
            var bot = new TelegramBotClient("437894347:AAEmadhGMPjSF1BtRLad5NXTkRuiW-aIelc");

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
