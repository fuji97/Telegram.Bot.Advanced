using System.Linq;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher.Filters {
    public class EndpointFilter : DispatcherFilterAttribute {
        private readonly string[] _endpoint;

        public EndpointFilter(params string[] endpoint) {
            _endpoint = endpoint;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return _endpoint.Contains(botData?.Endpoint);
        }
    }
}