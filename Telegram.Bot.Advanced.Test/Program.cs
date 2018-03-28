using System;
using Telegram.Bot.Args;

namespace Telegram.Bot.Advanced.Test
{
    class Program
    {
        private static Dispatcher Dispatcher { get; } = new Dispatcher(typeof(Controller), typeof(MasterContext));
        public static ITelegramBotClient RealBot { get; } = new TelegramBotClient("437894347:AAEmadhGMPjSF1BtRLad5NXTkRuiW-aIelc");

        static void Main(string[] args)
        {
            // Disable WebHook
            RealBot.DeleteWebhookAsync();

            RealBot.OnUpdate += SendToDispatcher;

            RealBot.StartReceiving();
            Console.WriteLine($"Start listening.");
            Console.ReadLine();
            RealBot.StopReceiving();
        }

        public static void SendToDispatcher(object sender, UpdateEventArgs args) {
            Dispatcher.DispatchUpdate(args.Update);
        }
    }
}
