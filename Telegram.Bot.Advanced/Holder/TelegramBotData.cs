using Telegram.Bot.Advanced.Dispatcher;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotData : ITelegramBotData {
        public string Endpoint { get; }
        public ITelegramBotClient Bot { get; }
        public IDispatcher Dispatcher { get; }
        public string BasePath { get; }


        public TelegramBotData(ITelegramBotClient bot, IDispatcher dispatcher, string endpoint, string basePath = "/telegram") {
            Endpoint = endpoint;
            Bot = bot;
            Dispatcher = dispatcher;
            BasePath = basePath;
        }

        public TelegramBotData(ITelegramBotClient bot, IDispatcherBuilder dispatcherBuilder, string endpoint, string basePath = "/telegram") {
            Endpoint = endpoint;
            Bot = bot;
            Dispatcher = dispatcherBuilder.SetTelegramBotData(this).Build();
            BasePath = basePath;
        }
    }
}