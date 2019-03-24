using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    public class DispatcherBuilder<TContext, TController> : IDispatcherBuilder
        where TContext : TelegramContext, new()
        where TController : class, ITelegramController<TContext>, new() {

        private ITelegramBotData _botData;
        private ILogger<Dispatcher<TContext, TController>> _logger;

        public IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData) {
            _botData = botData;
            return this;
        }

        public IDispatcherBuilder SetLogger(ILogger<Dispatcher<TContext, TController>> logger) {
            _logger = logger;
            return this;
        }

        public IDispatcher Build() {
            return new Dispatcher<TContext,TController>(_botData, _logger);
        }
    }
}