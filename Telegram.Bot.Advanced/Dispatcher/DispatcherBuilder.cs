using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    public class DispatcherBuilder<TContext, TController> : IDispatcherBuilder
        where TContext : TelegramContext, new()
        where TController : class, ITelegramController<TContext> {

        private ITelegramBotData _botData;


        public IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData) {
            _botData = botData;
            return this;
        }

        public IDispatcher Build() {
            return new Dispatcher<TContext,TController>(_botData);
        }
    }
}