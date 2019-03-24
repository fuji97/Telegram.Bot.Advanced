using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class ChatStateFilter : DispatcherFilterAttribute {
        private readonly int _state;

        public ChatStateFilter(int state) {
            _state = state;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return user.State == _state;
        }
    }
}
