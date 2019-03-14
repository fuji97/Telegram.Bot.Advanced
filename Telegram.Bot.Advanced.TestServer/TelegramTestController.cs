using System;
using System.Threading.Tasks;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DispatcherFilters;

namespace Telegram.Bot.Advanced.TestServer {
    public class TelegramTestController : TelegramController<TestTelegramContext> {

        [CommandFilter("help")]
        public async Task Help() {
            Console.WriteLine("Hello World");
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World");
        }
    }
}