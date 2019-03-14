using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class ChatTypeFilter : DispatcherFilterAttribute {
        private readonly ChatType _type;

        public ChatTypeFilter(ChatType type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return update.Message != null && update.Message.Chat.Type == _type;
        }
    }
}
