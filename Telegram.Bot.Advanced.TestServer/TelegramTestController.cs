using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Types;

namespace TestServer {
    public class TelegramTestController : TelegramController<TestTelegramContext> {
        public ITelegramBotClient Bot { get; set; }


        [CommandFilter("help")]
        public async Task Help() {
            await Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World");
        }
    }
}