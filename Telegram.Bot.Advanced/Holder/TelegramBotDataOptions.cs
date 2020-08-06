using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotDataOptions {
        public string Endpoint { get; set; }
        public ITelegramBotClient Bot { get; set; }
        public IDispatcher Dispatcher { get; set; }
        public IDispatcherBuilder DispatcherBuilder { get; set; }
        public string BasePath { get; set; } = "/telegram";
        public UserUpdate UserUpdate { get; set; } = UserUpdate.PrivateMessage;

        public void CreateTelegramBotClient(string key) {
            Bot = new TelegramBotClient(key);
            Endpoint ??= key;
        }
    }
}