using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotData<TContext, TController>
        where TContext : TelegramContext, new()
        where TController : class, ITelegramController<TContext>, new() {
        public string Endpoint { get; }
        public ITelegramBotClient Bot { get; }
        public Dispatcher<TContext, TController> Dispatcher { get; }
        public string BasePath { get; }


        public TelegramBotData(string endpoint, ITelegramBotClient bot, Dispatcher<TContext, TController> dispatcher, string basePath) {
            Endpoint = endpoint;
            Bot = bot;
            Dispatcher = dispatcher;
            BasePath = basePath;
        }
    }
}