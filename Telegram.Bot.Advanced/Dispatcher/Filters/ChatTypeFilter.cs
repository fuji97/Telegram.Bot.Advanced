using System.Linq;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Dispatcher.Filters
{
    public class ChatTypeFilter : DispatcherFilterAttribute {
        private readonly ChatType[] _type;

        public ChatTypeFilter(params ChatType[] type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return update.Message != null && _type.Contains(update.Message.Chat.Type);
        }
    }
}
