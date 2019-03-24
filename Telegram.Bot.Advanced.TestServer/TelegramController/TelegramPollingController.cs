using System.Threading.Tasks;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Dispatcher.Filters;

namespace Telegram.Bot.Advanced.TestServer.TelegramController {
    public class TelegramPollingController : TelegramController<TestTelegramContext> {
        [CommandFilter("help")]
        public void Help() {
            BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World!\nSiamo in polling mode.");
        }
        
        [CommandFilter("async")]
        public async Task AsyncMethod() {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World!\nSiamo in polling mode.");
        }
    }
}