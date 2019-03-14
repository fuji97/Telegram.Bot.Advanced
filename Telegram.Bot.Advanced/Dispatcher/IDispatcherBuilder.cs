using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    public interface IDispatcherBuilder {
        IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData);
        IDispatcher Build();
    }
}
