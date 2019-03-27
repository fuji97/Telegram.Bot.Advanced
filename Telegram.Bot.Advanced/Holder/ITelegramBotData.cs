using Telegram.Bot.Advanced.Dispatcher;

namespace Telegram.Bot.Advanced.Holder {
    public interface ITelegramBotData {
        string Endpoint { get; }
        ITelegramBotClient Bot { get; }
        IDispatcher Dispatcher { get; }
        string BasePath { get; }
        string Username { get; set; }
    }
}