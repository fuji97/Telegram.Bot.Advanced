using System;
using Telegram.Bot.Args;

namespace Telegram.Bot.Advanced.Test
{
    class Program
    {
        private static Dispatcher<MasterContext> Dispatcher { get; } = new Dispatcher<MasterContext>(typeof(Controller));
        public static ITelegramBotClient Bot { get; } = new TelegramBotClient("437894347:AAEmadhGMPjSF1BtRLad5NXTkRuiW-aIelc");

        static void Main(string[] args)
        {
            // Disable WebHook
            Bot.DeleteWebhookAsync();

            Bot.OnUpdate += SendToDispatcher;

            Bot.StartReceiving();
            Console.WriteLine($"Start listening.");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        public static void SendToDispatcher(object sender, UpdateEventArgs args) {
            Dispatcher.DispatchUpdate(args.Update);
        }
    }
}
