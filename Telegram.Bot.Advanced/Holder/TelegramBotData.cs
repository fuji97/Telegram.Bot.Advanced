using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotData {
        public string Endpoint { get; }
        public ITelegramBotClient Bot { get; }
        public IDispatcher Dispatcher { get; }
        public string BasePath { get; }


        public TelegramBotData(string endpoint, ITelegramBotClient bot, IDispatcher dispatcher, string basePath) {
            Endpoint = endpoint;
            Bot = bot;
            Dispatcher = dispatcher;
            BasePath = basePath;
        }
    }
}